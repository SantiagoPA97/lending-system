using FluentValidation;

namespace Lending.Api.Features.Assistant;

public sealed record AssistantChatMessage(string Role, string Content);

public sealed record AssistantQueryRequest(string Question, List<AssistantChatMessage>? History);

public sealed record AssistantToolCallResponse(string Name, string Summary);

public sealed record AssistantQueryResponse(
    bool Configured,
    string? Answer,
    IReadOnlyList<AssistantToolCallResponse> ToolCalls);

public sealed class AssistantQueryRequestValidator : AbstractValidator<AssistantQueryRequest>
{
    private static readonly string[] AllowedRoles = ["user", "assistant"];

    public AssistantQueryRequestValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.History)
            .Must(h => h is null || h.Count <= 10)
            .WithMessage("history can contain at most 10 messages.");

        RuleForEach(x => x.History).ChildRules(message =>
        {
            message.RuleFor(m => m.Role)
                .Must(r => AllowedRoles.Contains(r))
                .WithMessage("role must be 'user' or 'assistant'.");
            message.RuleFor(m => m.Content)
                .NotEmpty()
                .MaximumLength(20000);
        });
    }
}
