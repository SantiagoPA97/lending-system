namespace Lending.Domain.Entities;

public sealed record RepaymentAllocation(int Period, decimal Interest, decimal Principal);

public class Repayment
{
    private readonly List<RepaymentAllocation> _allocations = [];

    private Repayment()
    {
    }

    internal Repayment(
        Guid facilityId,
        decimal amount,
        Currency currency,
        DateOnly paymentDate,
        decimal principalApplied,
        decimal interestApplied,
        bool isReversal,
        Guid? reversesRepaymentId,
        IEnumerable<RepaymentAllocation> allocations,
        string? note = null)
    {
        Id = Guid.NewGuid();
        FacilityId = facilityId;
        Amount = amount;
        Currency = currency;
        PaymentDate = paymentDate;
        PrincipalApplied = principalApplied;
        InterestApplied = interestApplied;
        IsReversal = isReversal;
        ReversesRepaymentId = reversesRepaymentId;
        Note = note;
        CreatedAtUtc = DateTime.UtcNow;
        _allocations.AddRange(allocations);
    }

    public Guid Id { get; private set; }
    public Guid FacilityId { get; private set; }
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public decimal PrincipalApplied { get; private set; }
    public decimal InterestApplied { get; private set; }
    public bool IsReversal { get; private set; }
    public Guid? ReversesRepaymentId { get; private set; }
    public Guid? ReversedByRepaymentId { get; internal set; }
    public string? Note { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyList<RepaymentAllocation> Allocations => _allocations;
}
