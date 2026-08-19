using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Lending.Api.Features.Companies;
using Lending.Api.Features.Facilities;
using Lending.Api.Features.Repayments;
using Lending.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Lending.Api.Tests;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "api";
}

// One Postgres container and one app host for the whole run; test classes in the
// "api" collection run sequentially against the shared database.
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        // Program reads these flags straight from the process environment.
        Environment.SetEnvironmentVariable("DATABASE_URL", null);
        Environment.SetEnvironmentVariable("MIGRATE_ON_STARTUP", "true");
        Environment.SetEnvironmentVariable("SEED_DEMO_DATA", "false");
        Factory = CreateFactory();
        _ = Factory.Server; // boot the host so migrations apply before the first test
    }

    // A separate factory shares the database but gets a fresh in-process HybridCache.
    public WebApplicationFactory<Program> CreateFactory() =>
        new LendingApiFactory(_container.GetConnectionString());

    public async Task<HttpClient> CreateClientAsync(
        string? role = null,
        bool csrfHeader = true,
        WebApplicationFactory<Program>? factory = null)
    {
        var client = (factory ?? Factory).CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (csrfHeader)
            client.DefaultRequestHeaders.Add("X-Requested-With", "fetch");
        if (role is not null)
        {
            var login = await client.GetAsync($"/auth/dev-login?role={role}");
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        }

        return client;
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }
}

internal sealed class LendingApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Auth:Mode", "Bypass");
    }
}

public static class Api
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(body, Json)
            ?? throw new InvalidOperationException($"Could not deserialize response body: {body}");
    }

    public static async Task<JsonNode> ReadProblemAsync(
        this HttpResponseMessage response,
        int expectedStatus,
        string? expectedErrorCode = null)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(expectedStatus == (int)response.StatusCode,
            $"Expected {expectedStatus} but got {(int)response.StatusCode}: {body}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var node = JsonNode.Parse(body)!;
        Assert.Equal(expectedStatus, node["status"]!.GetValue<int>());
        if (expectedErrorCode is not null)
            Assert.Equal(expectedErrorCode, node["errorCode"]!.GetValue<string>());
        return node;
    }

    public static async Task<CompanyResponse> CreateCompanyAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/companies", new CompanyRequest(
            name,
            $"{name} S.A.S.",
            $"REG-{Guid.NewGuid().ToString("N")[..10]}",
            "CO",
            "Manufacturing",
            "finance@example.com"), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsAsync<CompanyResponse>();
    }

    public static async Task<FacilityResponse> CreateFacilityAsync(
        HttpClient client,
        Guid companyId,
        decimal amount,
        Currency currency,
        decimal annualRate,
        int termMonths,
        DateOnly startDate,
        RepaymentType repaymentType)
    {
        var response = await client.PostAsJsonAsync("/api/facilities", new CreateFacilityRequest(
            companyId, amount, currency, annualRate, termMonths, startDate, repaymentType), Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsAsync<FacilityResponse>();
    }

    public static async Task<FacilityResponse> ActivateFacilityAsync(HttpClient client, Guid facilityId)
    {
        var response = await client.PostAsync($"/api/facilities/{facilityId}/activate", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadAsAsync<FacilityResponse>();
    }

    public static Task<HttpResponseMessage> RepayAsync(
        HttpClient client,
        Guid facilityId,
        decimal amount,
        Currency currency,
        DateOnly paymentDate) =>
        client.PostAsJsonAsync(
            $"/api/facilities/{facilityId}/repayments",
            new RecordRepaymentRequest(amount, currency, paymentDate, null),
            Json);
}
