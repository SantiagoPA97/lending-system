using Lending.Domain;
using Lending.Domain.Entities;

namespace Lending.Domain.Tests;

// Baseline facility: 10,000 USD, 12% annual, 3 months amortizing from 2026-01-15.
// Schedule: P1 (100.00 int / 3,300.22 prin), P2 (67.00 / 3,333.22), P3 (33.67 / 3,366.56).
public class RepaymentTests
{
    private static Money Usd(decimal amount) => new(amount, Currency.USD);

    [Fact]
    public void RecordRepayment_OnDraftFacility_Throws()
    {
        var facility = TestData.DraftFacility();
        var ex = Assert.Throws<DomainException>(
            () => facility.RecordRepayment(Usd(100m), TestData.Start));
        Assert.Equal(DomainErrors.Facility.NotActive, ex.ErrorCode);
    }

    [Fact]
    public void RecordRepayment_WithWrongCurrency_Throws()
    {
        var facility = TestData.ActiveFacility(currency: Currency.USD);
        var ex = Assert.Throws<DomainException>(
            () => facility.RecordRepayment(new Money(100m, Currency.EUR), TestData.Start.AddMonths(1)));
        Assert.Equal(DomainErrors.Repayment.CurrencyMismatch, ex.ErrorCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void RecordRepayment_NonPositiveAmount_Throws(decimal amount)
    {
        var facility = TestData.ActiveFacility();
        var ex = Assert.Throws<DomainException>(
            () => facility.RecordRepayment(Usd(amount), TestData.Start.AddMonths(1)));
        Assert.Equal(DomainErrors.Repayment.InvalidAmount, ex.ErrorCode);
    }

    [Fact]
    public void PartialPayment_AllocatesToOldestInterestFirst()
    {
        var facility = TestData.ActiveFacility();
        var repayment = facility.RecordRepayment(Usd(50m), TestData.Start.AddMonths(1));

        Assert.Equal(50m, repayment.InterestApplied);
        Assert.Equal(0m, repayment.PrincipalApplied);
        Assert.Equal(50m, facility.Schedule[0].InterestPaid);
        Assert.Equal(0m, facility.Schedule[0].PrincipalPaid);
        Assert.Equal(10_000m, facility.OutstandingPrincipal);
        Assert.Equal(FacilityStatus.Active, facility.Status);
    }

    [Fact]
    public void Payment_SpansMultipleInstallments_InterestFirstThenPrincipalOldestFirst()
    {
        var facility = TestData.ActiveFacility();
        // At P2 due date: unpaid interest due is 100 (P1) + 67 (P2); remainder goes to P1 principal.
        var repayment = facility.RecordRepayment(Usd(667m), TestData.Start.AddMonths(2));

        Assert.Equal(167m, repayment.InterestApplied);
        Assert.Equal(500m, repayment.PrincipalApplied);
        Assert.Equal(100m, facility.Schedule[0].InterestPaid);
        Assert.Equal(500m, facility.Schedule[0].PrincipalPaid);
        Assert.Equal(67m, facility.Schedule[1].InterestPaid);
        Assert.Equal(0m, facility.Schedule[1].PrincipalPaid);
        Assert.Equal(9_500m, facility.OutstandingPrincipal);

        Assert.Equal(
            new[]
            {
                new RepaymentAllocation(1, 100m, 500m),
                new RepaymentAllocation(2, 67m, 0m)
            },
            repayment.Allocations);
    }

    [Fact]
    public void Payment_SettlesPrincipalAcrossInstallments_OldestFirst()
    {
        var facility = TestData.ActiveFacility();
        // 100 interest (P1) + 4,000 principal: settles P1 principal (3,300.22) then part of P2.
        facility.RecordRepayment(Usd(4_100m), TestData.Start.AddMonths(1));

        Assert.True(facility.Schedule[0].IsSettled);
        Assert.Equal(3_300.22m, facility.Schedule[0].PrincipalPaid);
        Assert.Equal(699.78m, facility.Schedule[1].PrincipalPaid);
        Assert.Equal(6_000m, facility.OutstandingPrincipal);
    }

    [Fact]
    public void Overpayment_BeyondOutstandingPlusDueInterest_Throws()
    {
        var facility = TestData.ActiveFacility();
        // At P1 due date the maximum payable is 10,000 + 100 interest.
        var ex = Assert.Throws<DomainException>(
            () => facility.RecordRepayment(Usd(10_100.01m), TestData.Start.AddMonths(1)));
        Assert.Equal(DomainErrors.Repayment.ExceedsOutstanding, ex.ErrorCode);
        Assert.Equal(10_000m, facility.OutstandingPrincipal);
        Assert.All(facility.Schedule, i => Assert.Equal(0m, i.InterestPaid + i.PrincipalPaid));
    }

    [Fact]
    public void PayingExactlyOutstandingPlusDueInterest_CompletesFacility()
    {
        var facility = TestData.ActiveFacility();
        facility.RecordRepayment(Usd(10_100m), TestData.Start.AddMonths(1));

        Assert.Equal(0m, facility.OutstandingPrincipal);
        Assert.Equal(FacilityStatus.Completed, facility.Status);
    }

    [Fact]
    public void EarlySettlement_BeforeAnyDueDate_PaysPrincipalWithoutFutureInterest()
    {
        var facility = TestData.ActiveFacility();
        var repayment = facility.RecordRepayment(Usd(10_000m), TestData.Start.AddDays(3));

        Assert.Equal(0m, repayment.InterestApplied);
        Assert.Equal(10_000m, repayment.PrincipalApplied);
        Assert.Equal(0m, facility.OutstandingPrincipal);
        Assert.Equal(FacilityStatus.Completed, facility.Status);
        Assert.Equal(3_300.22m, facility.Schedule[0].PrincipalPaid);
        Assert.Equal(3_333.22m, facility.Schedule[1].PrincipalPaid);
        Assert.Equal(3_366.56m, facility.Schedule[2].PrincipalPaid);
        // Early settlement waives all remaining interest — nothing left "due".
        Assert.All(facility.Schedule, i => Assert.True(i.IsSettled));
        Assert.Equal(0m, facility.Schedule.Sum(i => i.InterestPaid));
        Assert.Equal(200.67m, facility.Schedule.Sum(i => i.InterestWaived));
    }

    [Fact]
    public void EarlyPayoff_Amortizing_WaivesFutureInterest_AndSettlesAllInstallments()
    {
        var facility = TestData.ActiveFacility();
        facility.RecordRepayment(Usd(3_400.22m), TestData.Start.AddMonths(1)); // P1 in full
        facility.RecordRepayment(Usd(6_699.78m), TestData.Start.AddMonths(1).AddDays(5));

        Assert.Equal(0m, facility.OutstandingPrincipal);
        Assert.Equal(FacilityStatus.Completed, facility.Status);
        Assert.All(facility.Schedule, i => Assert.True(i.IsSettled));
        // Only P1 interest was actually received; P2/P3 interest is waived, not paid.
        Assert.Equal(100m, facility.Schedule.Sum(i => i.InterestPaid));
        Assert.Equal(0m, facility.Schedule[0].InterestWaived);
        Assert.Equal(67m, facility.Schedule[1].InterestWaived);
        Assert.Equal(33.67m, facility.Schedule[2].InterestWaived);
    }

    [Fact]
    public void EarlyPayoff_InterestOnly_WaivesFutureInterest_AndSettlesAllInstallments()
    {
        var facility = TestData.ActiveFacility(
            commitment: 100_000m, rate: 7.35m, termMonths: 13, type: RepaymentType.InterestOnly);

        facility.RecordRepayment(Usd(612.50m), TestData.Start.AddMonths(1));
        facility.RecordRepayment(Usd(100_612.50m), TestData.Start.AddMonths(2));

        Assert.Equal(0m, facility.OutstandingPrincipal);
        Assert.Equal(FacilityStatus.Completed, facility.Status);
        Assert.All(facility.Schedule, i => Assert.True(i.IsSettled));
        // Two months of interest received; the remaining 11 periods are waived.
        Assert.Equal(1_225m, facility.Schedule.Sum(i => i.InterestPaid));
        Assert.Equal(11 * 612.50m, facility.Schedule.Sum(i => i.InterestWaived));
    }

    [Fact]
    public void EarlySettlement_CannotExceedOutstandingPrincipal()
    {
        var facility = TestData.ActiveFacility();
        var ex = Assert.Throws<DomainException>(
            () => facility.RecordRepayment(Usd(10_000.01m), TestData.Start.AddDays(3)));
        Assert.Equal(DomainErrors.Repayment.ExceedsOutstanding, ex.ErrorCode);
    }

    [Fact]
    public void PayingEachInstallmentOnDueDate_CompletesFacility()
    {
        var facility = TestData.ActiveFacility();
        facility.RecordRepayment(Usd(3_400.22m), TestData.Start.AddMonths(1));
        facility.RecordRepayment(Usd(3_400.22m), TestData.Start.AddMonths(2));
        Assert.Equal(FacilityStatus.Active, facility.Status);

        facility.RecordRepayment(Usd(3_400.23m), TestData.Start.AddMonths(3));

        Assert.Equal(0m, facility.OutstandingPrincipal);
        Assert.Equal(FacilityStatus.Completed, facility.Status);
        Assert.All(facility.Schedule, i => Assert.True(i.IsSettled));
    }

    [Fact]
    public void InterestOnlyFacility_CollectsMonthlyInterestThenBalloon()
    {
        var facility = TestData.ActiveFacility(
            commitment: 100_000m, rate: 7.35m, termMonths: 13, type: RepaymentType.InterestOnly);

        var first = facility.RecordRepayment(Usd(612.50m), TestData.Start.AddMonths(1));
        Assert.Equal(612.50m, first.InterestApplied);
        Assert.Equal(0m, first.PrincipalApplied);
        Assert.Equal(100_000m, facility.OutstandingPrincipal);
        Assert.True(facility.Schedule[0].IsSettled);
    }

    [Fact]
    public void RecordRepayment_BeforeStartDate_Throws()
    {
        var facility = TestData.ActiveFacility();
        var ex = Assert.Throws<DomainException>(
            () => facility.RecordRepayment(Usd(100m), TestData.Start.AddDays(-1)));
        Assert.Equal(DomainErrors.Repayment.BeforeStart, ex.ErrorCode);
        Assert.Equal(10_000m, facility.OutstandingPrincipal);
    }

    [Fact]
    public void RecordRepayment_OnStartDate_Succeeds()
    {
        var facility = TestData.ActiveFacility();
        var repayment = facility.RecordRepayment(Usd(100m), TestData.Start);
        Assert.Equal(100m, repayment.PrincipalApplied);
    }

    [Fact]
    public void RecordRepayment_AfterCompletion_Throws()
    {
        var facility = TestData.ActiveFacility();
        facility.RecordRepayment(Usd(10_000m), TestData.Start.AddDays(1));

        var ex = Assert.Throws<DomainException>(
            () => facility.RecordRepayment(Usd(1m), TestData.Start.AddMonths(1)));
        Assert.Equal(DomainErrors.Facility.NotActive, ex.ErrorCode);
    }
}
