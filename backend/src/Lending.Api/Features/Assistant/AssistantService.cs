using Anthropic;
using Anthropic.Models.Messages;
using Lending.Infrastructure;
using Microsoft.Extensions.Caching.Hybrid;

namespace Lending.Api.Features.Assistant;

// Thrown when the Anthropic API fails or times out; the endpoint maps it to a 502.
public sealed class AssistantUpstreamException(string message, Exception? inner = null)
    : Exception(message, inner);

public static class AssistantService
{
    public const string DefaultModel = "claude-sonnet-5";
    private const int MaxToolRounds = 8;
    private static readonly TimeSpan UpstreamTimeout = TimeSpan.FromSeconds(60);

    public static async Task<AssistantQueryResponse> QueryAsync(
        AssistantQueryRequest request,
        LendingDbContext db,
        HybridCache cache,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
            return new AssistantQueryResponse(false, null, []);

        var model = configuration["Assistant:Model"];
        if (string.IsNullOrWhiteSpace(model))
            model = DefaultModel;

        AnthropicClient client = new() { ApiKey = apiKey };

        var messages = BuildMessages(request);
        var parameters = new MessageCreateParams
        {
            Model = model,
            MaxTokens = 2048,
            System = BuildSystemPrompt(),
            Tools = AssistantTools.Definitions(),
            Messages = messages
        };

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(UpstreamTimeout);

        var toolCalls = new List<AssistantToolCallResponse>();
        Message? response = null;

        try
        {
            for (var round = 1; round <= MaxToolRounds; round++)
            {
                response = await client.Messages.Create(parameters, cancellationToken: timeoutCts.Token);

                var toolUses = response.Content
                    .Select(b => b.Value)
                    .OfType<ToolUseBlock>()
                    .ToList();

                logger.LogInformation(
                    "Assistant round {Round}/{MaxRounds}: stop={StopReason} toolCalls={ToolNames}",
                    round, MaxToolRounds, response.StopReason,
                    string.Join(",", toolUses.Select(t => t.Name)));

                if (toolUses.Count == 0)
                    break;

                messages.Add(new MessageParam { Role = Role.Assistant, Content = ToParamContent(response) });

                var toolResults = new List<ContentBlockParam>();
                foreach (var toolUse in toolUses)
                {
                    var (summary, resultJson) = await AssistantTools.ExecuteAsync(
                        toolUse.Name, toolUse.Input, db, cache, cancellationToken);
                    toolCalls.Add(new AssistantToolCallResponse(toolUse.Name, summary));
                    toolResults.Add(new ToolResultBlockParam { ToolUseID = toolUse.ID, Content = resultJson });
                }

                messages.Add(new MessageParam { Role = Role.User, Content = toolResults });
            }
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AssistantUpstreamException(
                "The AI assistant timed out while contacting the Anthropic API. Please try again.", ex);
        }
        catch (Exception ex) when (ex is Anthropic.Exceptions.AnthropicException or HttpRequestException)
        {
            logger.LogWarning(ex, "Assistant call to the Anthropic API failed");
            throw new AssistantUpstreamException(
                "The AI assistant could not reach the Anthropic API. Please try again shortly.", ex);
        }

        var answer = response is null
            ? null
            : string.Join("\n\n", response.Content
                .Select(b => b.Value)
                .OfType<TextBlock>()
                .Select(t => t.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t)));

        if (string.IsNullOrWhiteSpace(answer))
            answer = "I wasn't able to produce an answer for that question. Try rephrasing it.";

        return new AssistantQueryResponse(true, answer, toolCalls);
    }

    private static List<MessageParam> BuildMessages(AssistantQueryRequest request)
    {
        var messages = new List<MessageParam>();
        foreach (var entry in (request.History ?? []).TakeLast(10))
        {
            messages.Add(new MessageParam
            {
                Role = entry.Role == "assistant" ? Role.Assistant : Role.User,
                Content = entry.Content
            });
        }

        messages.Add(new MessageParam { Role = Role.User, Content = request.Question });
        return messages;
    }

    // Echo the assistant turn back preserving thinking signatures and tool_use
    // blocks — required by the API before sending tool results.
    private static List<ContentBlockParam> ToParamContent(Message response)
    {
        var blocks = new List<ContentBlockParam>();
        foreach (var block in response.Content)
        {
            switch (block.Value)
            {
                case TextBlock text:
                    blocks.Add(new TextBlockParam { Text = text.Text });
                    break;
                case ThinkingBlock thinking:
                    blocks.Add(new ThinkingBlockParam
                    {
                        Thinking = thinking.Thinking,
                        Signature = thinking.Signature
                    });
                    break;
                case RedactedThinkingBlock redacted:
                    blocks.Add(new RedactedThinkingBlockParam { Data = redacted.Data });
                    break;
                case ToolUseBlock toolUse:
                    blocks.Add(new ToolUseBlockParam
                    {
                        ID = toolUse.ID,
                        Name = toolUse.Name,
                        Input = toolUse.Input
                    });
                    break;
            }
        }

        return blocks;
    }

    private static string BuildSystemPrompt()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return $"""
            You are the Ledgerline assistant, a lending operations analyst for the Ledgerline loan
            management system. Today's date is {today:yyyy-MM-dd}.

            You answer questions about this lending portfolio only: companies, credit facilities,
            repayment schedules, repayments received, and exposure. Use your tools to fetch data and
            cite figures exclusively from tool results — never estimate, extrapolate, or invent numbers.
            When a question needs data, call a tool before answering.

            Rules:
            - Never sum or net amounts across different currencies. Report per currency (e.g. "USD 1.2M
              committed and EUR 300K committed"). If asked for a single combined total, explain that
              amounts are kept per currency.
            - Format money as CODE amount, e.g. "USD 1,250,000.00".
            - Be concise: a short paragraph or a compact bullet list. No preamble.
            - If the question is outside this portfolio's data (general knowledge, market commentary,
              other systems), decline briefly and point back to portfolio topics.
            - If the tools return no matching data, say so plainly instead of guessing.
            """;
    }
}
