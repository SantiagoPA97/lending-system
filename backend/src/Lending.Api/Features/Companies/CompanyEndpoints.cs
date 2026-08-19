using FluentValidation;
using FluentValidation.Results;
using Lending.Api.Common;
using Lending.Api.Features.Auth;
using Lending.Domain;
using Lending.Domain.Entities;
using Lending.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Lending.Api.Features.Companies;

public static class CompanyEndpoints
{
    public static RouteGroupBuilder MapCompanyEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetList);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create).WithValidation<CompanyRequest>()
            .RequireAuthorization(AuthPolicies.Operator);
        group.MapPut("/{id:guid}", Update).WithValidation<CompanyRequest>()
            .RequireAuthorization(AuthPolicies.Operator);
        group.MapPost("/{id:guid}/deactivate", Deactivate)
            .RequireAuthorization(AuthPolicies.Admin);
        group.MapPost("/{id:guid}/activate", Activate)
            .RequireAuthorization(AuthPolicies.Operator);
        return group;
    }

    private static async Task<IResult> GetList(
        LendingDbContext db,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        string? sort = null,
        string? status = null,
        string? q = null)
    {
        if (page < 1)
            throw Invalid("page", "page must be at least 1.");
        if (pageSize is < 1 or > 100)
            throw Invalid("pageSize", "pageSize must be between 1 and 100.");

        var query = db.Companies.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<CompanyStatus>(status, ignoreCase: true, out var statusFilter))
                throw Invalid("status", $"status must be one of: {string.Join(", ", Enum.GetNames<CompanyStatus>())}.");
            query = query.Where(c => c.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q.Trim()}%";
            query = query.Where(c => EF.Functions.ILike(c.Name, pattern));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await ApplySort(query, sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Results.Ok(new PagedResult<CompanyResponse>(
            items.Select(c => c.ToResponse()).ToList(),
            total,
            page,
            pageSize));
    }

    private static async Task<IResult> GetById(Guid id, LendingDbContext db, CancellationToken cancellationToken)
    {
        var company = await db.Companies.AsNoTracking()
                          .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                      ?? throw new EntityNotFoundException(nameof(Company), id);

        var facilities = await db.Facilities.AsNoTracking()
            .Where(f => f.CompanyId == id)
            .OrderBy(f => f.Reference)
            .ToListAsync(cancellationToken);

        return Results.Ok(company.ToDetailResponse(facilities));
    }

    private static async Task<IResult> Create(
        CompanyRequest request,
        LendingDbContext db,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        await EnsureNameAvailable(db, name, excludeId: null, cancellationToken);

        var company = new Company(
            name,
            request.LegalName.Trim(),
            request.RegistrationNumber.Trim(),
            request.Country.Trim().ToUpperInvariant(),
            request.Industry.Trim(),
            request.ContactEmail.Trim());

        db.Companies.Add(company);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/companies/{company.Id}", company.ToResponse());
    }

    private static async Task<IResult> Update(
        Guid id,
        CompanyRequest request,
        LendingDbContext db,
        CancellationToken cancellationToken)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                      ?? throw new EntityNotFoundException(nameof(Company), id);

        var name = request.Name.Trim();
        await EnsureNameAvailable(db, name, excludeId: id, cancellationToken);

        company.UpdateDetails(
            name,
            request.LegalName.Trim(),
            request.RegistrationNumber.Trim(),
            request.Country.Trim().ToUpperInvariant(),
            request.Industry.Trim(),
            request.ContactEmail.Trim());

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(company.ToResponse());
    }

    private static async Task<IResult> Deactivate(Guid id, LendingDbContext db, CancellationToken cancellationToken)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                      ?? throw new EntityNotFoundException(nameof(Company), id);
        company.Deactivate();
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(company.ToResponse());
    }

    private static async Task<IResult> Activate(Guid id, LendingDbContext db, CancellationToken cancellationToken)
    {
        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                      ?? throw new EntityNotFoundException(nameof(Company), id);
        company.Activate();
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(company.ToResponse());
    }

    private static async Task EnsureNameAvailable(
        LendingDbContext db,
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var taken = await db.Companies.AnyAsync(
            c => c.Name == name && (excludeId == null || c.Id != excludeId),
            cancellationToken);
        if (taken)
            throw new DomainException("company.duplicate_name", $"A company named '{name}' already exists.");
    }

    private static IQueryable<Company> ApplySort(IQueryable<Company> query, string? sort)
    {
        var descending = sort?.StartsWith('-') == true;
        var key = (descending ? sort![1..] : sort)?.Trim().ToLowerInvariant();
        return key switch
        {
            null or "" or "name" => descending ? query.OrderByDescending(c => c.Name) : query.OrderBy(c => c.Name),
            "country" => descending ? query.OrderByDescending(c => c.Country) : query.OrderBy(c => c.Country),
            "industry" => descending ? query.OrderByDescending(c => c.Industry) : query.OrderBy(c => c.Industry),
            "status" => descending ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            "createdat" => descending ? query.OrderByDescending(c => c.CreatedAtUtc) : query.OrderBy(c => c.CreatedAtUtc),
            "updatedat" => descending ? query.OrderByDescending(c => c.UpdatedAtUtc) : query.OrderBy(c => c.UpdatedAtUtc),
            _ => throw Invalid("sort",
                "sort must be one of: name, country, industry, status, createdAt, updatedAt (prefix with '-' for descending).")
        };
    }

    private static ValidationException Invalid(string property, string message) =>
        new([new ValidationFailure(property, message)]);
}
