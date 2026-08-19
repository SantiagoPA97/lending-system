using FluentValidation;
using FluentValidation.Results;
using Lending.Api.Common;
using Lending.Api.Features.Auth;
using Lending.Api.Features.Repayments;
using Lending.Domain;
using Lending.Domain.Entities;
using Lending.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Lending.Api.Features.Facilities;

public static class FacilityEndpoints
{
    public static RouteGroupBuilder MapFacilityEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetList);
        group.MapGet("/{id:guid}", GetById);
        group.MapPost("/", Create).WithValidation<CreateFacilityRequest>()
            .RequireAuthorization(AuthPolicies.Operator);
        group.MapPut("/{id:guid}", Update).WithValidation<UpdateFacilityRequest>()
            .RequireAuthorization(AuthPolicies.Operator);
        group.MapPost("/{id:guid}/activate", Activate)
            .RequireAuthorization(AuthPolicies.Operator);
        group.MapPost("/{id:guid}/cancel", Cancel)
            .RequireAuthorization(AuthPolicies.Admin);
        group.MapPost("/{id:guid}/default", MarkDefaulted)
            .RequireAuthorization(AuthPolicies.Admin);
        group.MapGet("/{id:guid}/schedule", GetSchedule);
        // Schedule preview is a projection (read-only) despite being a POST, so viewer access is fine.
        group.MapPost("/schedule-preview", SchedulePreview).WithValidation<SchedulePreviewRequest>();
        group.MapPost("/{id:guid}/repayments", RecordRepayment).WithValidation<RecordRepaymentRequest>()
            .RequireAuthorization(AuthPolicies.Operator);
        group.MapGet("/{id:guid}/repayments", GetRepayments);
        return group;
    }

    private static async Task<IResult> GetList(
        LendingDbContext db,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        string? sort = null,
        string? status = null,
        string? repaymentType = null,
        string? currency = null,
        Guid? companyId = null)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);

        var query = db.Facilities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusFilter = ParseEnum<FacilityStatus>(status, "status");
            query = query.Where(f => f.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(repaymentType))
        {
            var typeFilter = ParseEnum<RepaymentType>(repaymentType, "repaymentType");
            query = query.Where(f => f.RepaymentType == typeFilter);
        }

        if (!string.IsNullOrWhiteSpace(currency))
        {
            var currencyFilter = ParseEnum<Currency>(currency, "currency");
            query = query.Where(f => f.Currency == currencyFilter);
        }

        if (companyId is not null)
            query = query.Where(f => f.CompanyId == companyId);

        var total = await query.CountAsync(cancellationToken);
        var items = await ApplySort(query, sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var companyNames = await GetCompanyNames(db, items.Select(f => f.CompanyId), cancellationToken);

        return Results.Ok(new PagedResult<FacilityResponse>(
            items.Select(f => f.ToResponse(companyNames[f.CompanyId])).ToList(),
            total,
            page,
            pageSize));
    }

    private static async Task<IResult> GetById(Guid id, LendingDbContext db, CancellationToken cancellationToken)
    {
        var facility = await db.Facilities.AsNoTracking()
                           .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
                       ?? throw new EntityNotFoundException(nameof(Facility), id);

        var company = await db.Companies.AsNoTracking()
            .FirstAsync(c => c.Id == facility.CompanyId, cancellationToken);

        var schedule = await db.ScheduleItems.AsNoTracking()
            .Where(i => i.FacilityId == id)
            .OrderBy(i => i.Period)
            .ToListAsync(cancellationToken);

        return Results.Ok(facility.ToDetailResponse(company, schedule));
    }

    private static async Task<IResult> Create(
        CreateFacilityRequest request,
        LendingDbContext db,
        CancellationToken cancellationToken)
    {
        var company = await db.Companies.AsNoTracking()
                          .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken)
                      ?? throw Invalid("companyId", $"Company '{request.CompanyId}' does not exist.");

        var facility = Facility.Create(
            company,
            new Money(request.CommitmentAmount, request.Currency),
            request.AnnualInterestRate,
            request.TermMonths,
            request.StartDate,
            request.RepaymentType);

        db.Facilities.Add(facility);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/facilities/{facility.Id}", facility.ToResponse(company.Name));
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateFacilityRequest request,
        LendingDbContext db,
        CancellationToken cancellationToken)
    {
        var facility = await db.Facilities.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
                       ?? throw new EntityNotFoundException(nameof(Facility), id);

        facility.UpdateTerms(
            new Money(request.CommitmentAmount, request.Currency),
            request.AnnualInterestRate,
            request.TermMonths,
            request.StartDate,
            request.RepaymentType);

        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(facility.ToResponse(await GetCompanyName(db, facility.CompanyId, cancellationToken)));
    }

    private static async Task<IResult> Activate(
        Guid id,
        IScheduleCalculator scheduleCalculator,
        LendingDbContext db,
        CancellationToken cancellationToken)
    {
        var facility = await db.Facilities
                           .Include(f => f.Schedule)
                           .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
                       ?? throw new EntityNotFoundException(nameof(Facility), id);

        facility.Activate(scheduleCalculator);
        // New children carry client-set ids, so navigation fixup would track them as
        // Modified; explicit Add marks them for INSERT.
        db.ScheduleItems.AddRange(facility.Schedule);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(facility.ToResponse(await GetCompanyName(db, facility.CompanyId, cancellationToken)));
    }

    private static async Task<IResult> Cancel(Guid id, LendingDbContext db, CancellationToken cancellationToken)
    {
        var facility = await db.Facilities.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
                       ?? throw new EntityNotFoundException(nameof(Facility), id);

        facility.Cancel();
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(facility.ToResponse(await GetCompanyName(db, facility.CompanyId, cancellationToken)));
    }

    private static async Task<IResult> MarkDefaulted(Guid id, LendingDbContext db, CancellationToken cancellationToken)
    {
        var facility = await db.Facilities.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
                       ?? throw new EntityNotFoundException(nameof(Facility), id);

        facility.MarkDefaulted();
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(facility.ToResponse(await GetCompanyName(db, facility.CompanyId, cancellationToken)));
    }

    private static async Task<IResult> GetSchedule(Guid id, LendingDbContext db, CancellationToken cancellationToken)
    {
        var facility = await db.Facilities.AsNoTracking()
                           .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
                       ?? throw new EntityNotFoundException(nameof(Facility), id);

        var items = await db.ScheduleItems.AsNoTracking()
            .Where(i => i.FacilityId == id)
            .OrderBy(i => i.Period)
            .ToListAsync(cancellationToken);

        var totalPrincipal = items.Sum(i => i.PrincipalDue);
        var totalInterest = items.Sum(i => i.InterestDue);

        return Results.Ok(new ScheduleResponse(
            facility.Id,
            facility.Reference,
            facility.Currency,
            items.Select(i => i.ToItemResponse()).ToList(),
            totalPrincipal,
            totalInterest,
            totalPrincipal + totalInterest));
    }

    private static IResult SchedulePreview(SchedulePreviewRequest request)
    {
        var periods = ScheduleCalculator.Calculate(new ScheduleTerms(
            request.CommitmentAmount,
            request.AnnualInterestRate,
            request.TermMonths,
            request.StartDate,
            request.RepaymentType));

        var totalPrincipal = periods.Sum(p => p.PrincipalDue);
        var totalInterest = periods.Sum(p => p.InterestDue);

        return Results.Ok(new SchedulePreviewResponse(
            request.Currency,
            periods.Select(p => p.ToPreviewResponse()).ToList(),
            totalPrincipal,
            totalInterest,
            totalPrincipal + totalInterest));
    }

    private static async Task<IResult> RecordRepayment(
        Guid id,
        RecordRepaymentRequest request,
        LendingDbContext db,
        CancellationToken cancellationToken)
    {
        // RecordRepayment only reads the schedule; the repayments collection is
        // needed for reversal lookups, not here.
        var facility = await db.Facilities
                           .Include(f => f.Schedule)
                           .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
                       ?? throw new EntityNotFoundException(nameof(Facility), id);

        var repayment = facility.RecordRepayment(
            new Money(request.Amount, request.Currency),
            request.PaymentDate,
            request.Note);
        db.Repayments.Add(repayment);

        // Single SaveChanges = one DB transaction; xmin concurrency conflicts bubble as 409.
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/facilities/{id}/repayments",
            repayment.ToResultResponse(facility));
    }

    private static async Task<IResult> GetRepayments(Guid id, LendingDbContext db, CancellationToken cancellationToken)
    {
        var exists = await db.Facilities.AsNoTracking().AnyAsync(f => f.Id == id, cancellationToken);
        if (!exists)
            throw new EntityNotFoundException(nameof(Facility), id);

        var repayments = await db.Repayments.AsNoTracking()
            .Where(r => r.FacilityId == id)
            .OrderBy(r => r.PaymentDate)
            .ThenBy(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return Results.Ok(repayments.Select(r => r.ToResponse()).ToList());
    }

    private static async Task<Dictionary<Guid, string>> GetCompanyNames(
        LendingDbContext db,
        IEnumerable<Guid> companyIds,
        CancellationToken cancellationToken)
    {
        var ids = companyIds.Distinct().ToList();
        return await db.Companies.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
    }

    private static async Task<string> GetCompanyName(
        LendingDbContext db,
        Guid companyId,
        CancellationToken cancellationToken) =>
        await db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => c.Name)
            .FirstAsync(cancellationToken);

    private static TEnum ParseEnum<TEnum>(string value, string paramName) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw Invalid(paramName, $"{paramName} must be one of: {string.Join(", ", Enum.GetNames<TEnum>())}.");

    private static IQueryable<Facility> ApplySort(IQueryable<Facility> query, string? sort)
    {
        var descending = sort?.StartsWith('-') == true;
        var key = (descending ? sort![1..] : sort)?.Trim().ToLowerInvariant();
        return key switch
        {
            null or "" or "reference" => descending
                ? query.OrderByDescending(f => f.Reference)
                : query.OrderBy(f => f.Reference),
            "commitmentamount" => descending
                ? query.OrderByDescending(f => f.CommitmentAmount)
                : query.OrderBy(f => f.CommitmentAmount),
            "outstandingprincipal" => descending
                ? query.OrderByDescending(f => f.OutstandingPrincipal)
                : query.OrderBy(f => f.OutstandingPrincipal),
            "annualinterestrate" => descending
                ? query.OrderByDescending(f => f.AnnualInterestRate)
                : query.OrderBy(f => f.AnnualInterestRate),
            "startdate" => descending
                ? query.OrderByDescending(f => f.StartDate)
                : query.OrderBy(f => f.StartDate),
            "status" => descending
                ? query.OrderByDescending(f => f.Status)
                : query.OrderBy(f => f.Status),
            "createdat" => descending
                ? query.OrderByDescending(f => f.CreatedAtUtc)
                : query.OrderBy(f => f.CreatedAtUtc),
            "updatedat" => descending
                ? query.OrderByDescending(f => f.UpdatedAtUtc)
                : query.OrderBy(f => f.UpdatedAtUtc),
            _ => throw Invalid("sort",
                "sort must be one of: reference, commitmentAmount, outstandingPrincipal, annualInterestRate, " +
                "startDate, status, createdAt, updatedAt (prefix with '-' for descending).")
        };
    }

    private static ValidationException Invalid(string property, string message) =>
        new([new ValidationFailure(property, message)]);
}
