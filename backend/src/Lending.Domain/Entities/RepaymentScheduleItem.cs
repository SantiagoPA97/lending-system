namespace Lending.Domain.Entities;

public class RepaymentScheduleItem
{
    private RepaymentScheduleItem()
    {
    }

    internal RepaymentScheduleItem(Guid facilityId, SchedulePeriod period)
    {
        Id = Guid.NewGuid();
        FacilityId = facilityId;
        Period = period.Period;
        DueDate = period.DueDate;
        PrincipalDue = period.PrincipalDue;
        InterestDue = period.InterestDue;
        RemainingBalance = period.RemainingBalance;
    }

    public Guid Id { get; private set; }
    public Guid FacilityId { get; private set; }
    public int Period { get; private set; }
    public DateOnly DueDate { get; private set; }
    public decimal PrincipalDue { get; private set; }
    public decimal InterestDue { get; private set; }
    public decimal RemainingBalance { get; private set; }
    public decimal PrincipalPaid { get; private set; }
    public decimal InterestPaid { get; private set; }
    public decimal InterestWaived { get; private set; }

    public decimal PrincipalOutstanding => PrincipalDue - PrincipalPaid;
    public decimal InterestOutstanding => InterestDue - InterestPaid - InterestWaived;
    public bool IsSettled => PrincipalOutstanding == 0m && InterestOutstanding == 0m;

    internal void ApplyInterest(decimal amount) => InterestPaid += amount;

    internal void ApplyPrincipal(decimal amount) => PrincipalPaid += amount;

    internal void WaiveRemainingInterest() => InterestWaived = InterestDue - InterestPaid;

    internal void ClearInterestWaiver() => InterestWaived = 0m;

    internal void Revert(decimal interest, decimal principal)
    {
        InterestPaid -= interest;
        PrincipalPaid -= principal;
    }
}
