namespace Lending.Domain.Entities;

public class Facility
{
    private readonly List<RepaymentScheduleItem> _schedule = [];
    private readonly List<Repayment> _repayments = [];

    private Facility()
    {
    }

    public static Facility Create(
        Company company,
        Money commitment,
        decimal annualInterestRate,
        int termMonths,
        DateOnly startDate,
        RepaymentType repaymentType)
    {
        if (company.Status != CompanyStatus.Active)
            throw new DomainException("company.inactive", "Inactive companies cannot receive new facilities.");

        var facility = new Facility
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Status = FacilityStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow
        };
        facility.UpdatedAtUtc = facility.CreatedAtUtc;
        facility.SetTerms(commitment, annualInterestRate, termMonths, startDate, repaymentType);
        return facility;
    }

    public Guid Id { get; private set; }
    public string Reference { get; set; } = string.Empty;
    public Guid CompanyId { get; private set; }
    public decimal CommitmentAmount { get; private set; }
    public Currency Currency { get; private set; }
    public decimal AnnualInterestRate { get; private set; }
    public int TermMonths { get; private set; }
    public DateOnly StartDate { get; private set; }
    public RepaymentType RepaymentType { get; private set; }
    public FacilityStatus Status { get; private set; }
    public decimal OutstandingPrincipal { get; private set; }
    public uint Xmin { get; set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public IReadOnlyList<RepaymentScheduleItem> Schedule => _schedule;
    public IReadOnlyList<Repayment> Repayments => _repayments;

    public Money Commitment => new(CommitmentAmount, Currency);
    public Money Outstanding => new(OutstandingPrincipal, Currency);

    public void UpdateTerms(
        Money commitment,
        decimal annualInterestRate,
        int termMonths,
        DateOnly startDate,
        RepaymentType repaymentType)
    {
        if (Status != FacilityStatus.Draft)
            throw new DomainException("facility.not_editable", "Only draft facilities can be edited.");
        SetTerms(commitment, annualInterestRate, termMonths, startDate, repaymentType);
        Touch();
    }

    public void Activate(IScheduleCalculator scheduleCalculator)
    {
        if (Status != FacilityStatus.Draft)
            throw new DomainException(
                "facility.invalid_transition",
                $"Cannot activate a facility in status {Status}.");

        var periods = scheduleCalculator.Generate(
            new ScheduleTerms(CommitmentAmount, AnnualInterestRate, TermMonths, StartDate, RepaymentType));
        _schedule.AddRange(periods.Select(p => new RepaymentScheduleItem(Id, p)));
        OutstandingPrincipal = CommitmentAmount;
        Status = FacilityStatus.Active;
        Touch();
    }

    public void Cancel()
    {
        if (Status is not (FacilityStatus.Draft or FacilityStatus.Active))
            throw new DomainException(
                "facility.invalid_transition",
                $"Cannot cancel a facility in status {Status}.");
        Status = FacilityStatus.Cancelled;
        Touch();
    }

    public void MarkDefaulted()
    {
        if (Status != FacilityStatus.Active)
            throw new DomainException(
                "facility.invalid_transition",
                $"Cannot default a facility in status {Status}.");
        Status = FacilityStatus.Defaulted;
        Touch();
    }

    public Repayment RecordRepayment(Money amount, DateOnly paymentDate)
    {
        if (Status != FacilityStatus.Active)
            throw new DomainException("facility.not_active", "Repayments can only be recorded on active facilities.");
        if (amount.Currency != Currency)
            throw new DomainException(
                "repayment.currency_mismatch",
                $"Repayment currency {amount.Currency} does not match facility currency {Currency}.");
        var value = MoneyMath.Round(amount.Amount);
        if (value <= 0m)
            throw new DomainException("repayment.invalid_amount", "Repayment amount must be positive.");

        var unpaidInterestDue = _schedule
            .Where(i => i.DueDate <= paymentDate)
            .Sum(i => i.InterestOutstanding);
        var maxPayable = OutstandingPrincipal + unpaidInterestDue;
        if (value > maxPayable)
            throw new DomainException(
                "repayment.exceeds_outstanding",
                $"Repayment of {value} exceeds the payable amount of {maxPayable} " +
                "(outstanding principal plus unpaid interest due).");

        var remaining = value;
        var allocations = new Dictionary<int, (decimal Interest, decimal Principal)>();
        decimal interestApplied = 0m;

        foreach (var item in _schedule
                     .Where(i => i.DueDate <= paymentDate && i.InterestOutstanding > 0m)
                     .OrderBy(i => i.Period))
        {
            if (remaining == 0m)
                break;
            var portion = Math.Min(remaining, item.InterestOutstanding);
            item.ApplyInterest(portion);
            allocations[item.Period] = (portion, 0m);
            interestApplied += portion;
            remaining -= portion;
        }

        decimal principalApplied = 0m;
        foreach (var item in _schedule
                     .Where(i => i.PrincipalOutstanding > 0m)
                     .OrderBy(i => i.Period))
        {
            if (remaining == 0m)
                break;
            var portion = Math.Min(remaining, item.PrincipalOutstanding);
            item.ApplyPrincipal(portion);
            var existing = allocations.TryGetValue(item.Period, out var current) ? current : (0m, 0m);
            allocations[item.Period] = (existing.Item1, existing.Item2 + portion);
            principalApplied += portion;
            remaining -= portion;
        }

        OutstandingPrincipal -= principalApplied;

        var repayment = new Repayment(
            Id,
            value,
            Currency,
            paymentDate,
            principalApplied,
            interestApplied,
            isReversal: false,
            reversesRepaymentId: null,
            allocations
                .OrderBy(a => a.Key)
                .Select(a => new RepaymentAllocation(a.Key, a.Value.Interest, a.Value.Principal)));
        _repayments.Add(repayment);

        if (OutstandingPrincipal == 0m)
            Status = FacilityStatus.Completed;
        Touch();
        return repayment;
    }

    public Repayment ReverseRepayment(Guid repaymentId, DateOnly reversalDate)
    {
        var original = _repayments.FirstOrDefault(r => r.Id == repaymentId)
            ?? throw new DomainException("repayment.not_found", $"Repayment {repaymentId} was not found on this facility.");
        if (original.IsReversal)
            throw new DomainException("repayment.cannot_reverse_reversal", "A reversal entry cannot be reversed.");
        if (original.ReversedByRepaymentId is not null)
            throw new DomainException("repayment.already_reversed", "This repayment has already been reversed.");

        foreach (var allocation in original.Allocations)
        {
            _schedule.Single(i => i.Period == allocation.Period)
                .Revert(allocation.Interest, allocation.Principal);
        }

        OutstandingPrincipal += original.PrincipalApplied;

        var reversal = new Repayment(
            Id,
            -original.Amount,
            Currency,
            reversalDate,
            -original.PrincipalApplied,
            -original.InterestApplied,
            isReversal: true,
            reversesRepaymentId: original.Id,
            original.Allocations.Select(a => new RepaymentAllocation(a.Period, -a.Interest, -a.Principal)));
        _repayments.Add(reversal);
        original.ReversedByRepaymentId = reversal.Id;

        if (Status == FacilityStatus.Completed && OutstandingPrincipal > 0m)
            Status = FacilityStatus.Active;
        Touch();
        return reversal;
    }

    private void SetTerms(
        Money commitment,
        decimal annualInterestRate,
        int termMonths,
        DateOnly startDate,
        RepaymentType repaymentType)
    {
        if (commitment.Amount <= 0m || MoneyMath.Round(commitment.Amount) != commitment.Amount)
            throw new DomainException("facility.invalid_terms", "Commitment must be a positive amount with at most two decimal places.");
        if (annualInterestRate < 0m)
            throw new DomainException("facility.invalid_terms", "Interest rate cannot be negative.");
        if (termMonths < 1)
            throw new DomainException("facility.invalid_terms", "Term must be at least one month.");

        CommitmentAmount = commitment.Amount;
        Currency = commitment.Currency;
        AnnualInterestRate = annualInterestRate;
        TermMonths = termMonths;
        StartDate = startDate;
        RepaymentType = repaymentType;
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
