using Lending.Domain;

namespace Lending.Domain.Tests;

public class RepaymentReversalTests
{
    private static Money Usd(decimal amount) => new(amount, Currency.USD);
    private static readonly DateOnly ReversalDate = TestData.Start.AddMonths(2).AddDays(5);

    [Fact]
    public void Reversal_RestoresInstallmentsAndOutstanding()
    {
        var facility = TestData.ActiveFacility();
        var original = facility.RecordRepayment(Usd(667m), TestData.Start.AddMonths(2));

        facility.ReverseRepayment(original.Id, ReversalDate);

        Assert.Equal(10_000m, facility.OutstandingPrincipal);
        Assert.All(facility.Schedule, i =>
        {
            Assert.Equal(0m, i.InterestPaid);
            Assert.Equal(0m, i.PrincipalPaid);
        });
    }

    [Fact]
    public void Reversal_CreatesCompensatingImmutableEntry()
    {
        var facility = TestData.ActiveFacility();
        var original = facility.RecordRepayment(Usd(667m), TestData.Start.AddMonths(2));

        var reversal = facility.ReverseRepayment(original.Id, ReversalDate);

        Assert.Equal(2, facility.Repayments.Count);
        Assert.True(reversal.IsReversal);
        Assert.Equal(-667m, reversal.Amount);
        Assert.Equal(-167m, reversal.InterestApplied);
        Assert.Equal(-500m, reversal.PrincipalApplied);
        Assert.Equal(original.Id, reversal.ReversesRepaymentId);
        Assert.Equal(reversal.Id, original.ReversedByRepaymentId);
        // Original financial fields untouched.
        Assert.Equal(667m, original.Amount);
        Assert.Equal(167m, original.InterestApplied);
        Assert.Equal(500m, original.PrincipalApplied);
    }

    [Fact]
    public void Reversal_OfFinalPayment_ReopensCompletedFacility()
    {
        var facility = TestData.ActiveFacility();
        var settlement = facility.RecordRepayment(Usd(10_000m), TestData.Start.AddDays(1));
        Assert.Equal(FacilityStatus.Completed, facility.Status);

        facility.ReverseRepayment(settlement.Id, ReversalDate);

        Assert.Equal(FacilityStatus.Active, facility.Status);
        Assert.Equal(10_000m, facility.OutstandingPrincipal);
    }

    [Fact]
    public void Reversal_OfEarlySettlement_RestoresWaivedInterest()
    {
        var facility = TestData.ActiveFacility();
        var settlement = facility.RecordRepayment(Usd(10_000m), TestData.Start.AddDays(1));
        Assert.True(facility.Schedule.Sum(i => i.InterestWaived) > 0m);

        facility.ReverseRepayment(settlement.Id, ReversalDate);

        Assert.Equal(FacilityStatus.Active, facility.Status);
        Assert.All(facility.Schedule, i =>
        {
            Assert.Equal(0m, i.InterestWaived);
            Assert.Equal(0m, i.InterestPaid);
            Assert.Equal(0m, i.PrincipalPaid);
            Assert.False(i.IsSettled);
        });
        Assert.Equal(100m, facility.Schedule[0].InterestOutstanding);
    }

    [Fact]
    public void Reversal_OfInterestPayment_OnSettledFacility_ReWaivesRemainingInterest()
    {
        var facility = TestData.ActiveFacility();
        var interestPayment = facility.RecordRepayment(Usd(100m), TestData.Start.AddMonths(1)); // P1 interest
        facility.RecordRepayment(Usd(10_000m), TestData.Start.AddMonths(1)); // full payoff
        Assert.Equal(FacilityStatus.Completed, facility.Status);

        facility.ReverseRepayment(interestPayment.Id, ReversalDate);

        // Principal is still fully settled, so the facility stays completed and the
        // clawed-back interest joins the waived remainder — nothing left "due".
        Assert.Equal(FacilityStatus.Completed, facility.Status);
        Assert.Equal(0m, facility.OutstandingPrincipal);
        Assert.All(facility.Schedule, i => Assert.True(i.IsSettled));
        Assert.Equal(0m, facility.Schedule[0].InterestPaid);
        Assert.Equal(100m, facility.Schedule[0].InterestWaived);
    }

    [Theory]
    [InlineData(FacilityStatus.Cancelled)]
    [InlineData(FacilityStatus.Defaulted)]
    public void Reversal_OnClosedFacility_Throws(FacilityStatus status)
    {
        var facility = TestData.ActiveFacility();
        var original = facility.RecordRepayment(Usd(500m), TestData.Start.AddMonths(1));
        if (status == FacilityStatus.Cancelled)
            facility.Cancel();
        else
            facility.MarkDefaulted();

        var ex = Assert.Throws<DomainException>(
            () => facility.ReverseRepayment(original.Id, ReversalDate));
        Assert.Equal(DomainErrors.Repayment.FacilityClosed, ex.ErrorCode);
        // Nothing was restored: the ledger and outstanding are untouched.
        Assert.Equal(9_600m, facility.OutstandingPrincipal);
        Assert.Equal(100m, facility.Schedule[0].InterestPaid);
        Assert.Equal(400m, facility.Schedule[0].PrincipalPaid);
        Assert.Null(original.ReversedByRepaymentId);
    }

    [Fact]
    public void Reversal_ThenRepayingAgain_WorksAsIfNeverPaid()
    {
        var facility = TestData.ActiveFacility();
        var original = facility.RecordRepayment(Usd(667m), TestData.Start.AddMonths(2));
        facility.ReverseRepayment(original.Id, ReversalDate);

        var again = facility.RecordRepayment(Usd(667m), TestData.Start.AddMonths(2));

        Assert.Equal(167m, again.InterestApplied);
        Assert.Equal(500m, again.PrincipalApplied);
        Assert.Equal(9_500m, facility.OutstandingPrincipal);
    }

    [Fact]
    public void Reversal_OfMiddlePayment_RestoresOnlyItsAllocations()
    {
        var facility = TestData.ActiveFacility();
        var first = facility.RecordRepayment(Usd(100m), TestData.Start.AddMonths(1));
        var second = facility.RecordRepayment(Usd(1_067m), TestData.Start.AddMonths(2));

        facility.ReverseRepayment(first.Id, ReversalDate);

        // Second payment's allocations survive: 67 interest (P2) + 1,000 principal (P1).
        Assert.Equal(0m, facility.Schedule[0].InterestPaid);
        Assert.Equal(1_000m, facility.Schedule[0].PrincipalPaid);
        Assert.Equal(67m, facility.Schedule[1].InterestPaid);
        Assert.Equal(9_000m, facility.OutstandingPrincipal);
        Assert.NotNull(first.ReversedByRepaymentId);
        Assert.Null(second.ReversedByRepaymentId);
    }

    [Fact]
    public void ReversingTwice_Throws()
    {
        var facility = TestData.ActiveFacility();
        var original = facility.RecordRepayment(Usd(500m), TestData.Start.AddMonths(1));
        facility.ReverseRepayment(original.Id, ReversalDate);

        var ex = Assert.Throws<DomainException>(
            () => facility.ReverseRepayment(original.Id, ReversalDate));
        Assert.Equal(DomainErrors.Repayment.AlreadyReversed, ex.ErrorCode);
    }

    [Fact]
    public void ReversingAReversal_Throws()
    {
        var facility = TestData.ActiveFacility();
        var original = facility.RecordRepayment(Usd(500m), TestData.Start.AddMonths(1));
        var reversal = facility.ReverseRepayment(original.Id, ReversalDate);

        var ex = Assert.Throws<DomainException>(
            () => facility.ReverseRepayment(reversal.Id, ReversalDate));
        Assert.Equal(DomainErrors.Repayment.CannotReverseReversal, ex.ErrorCode);
    }

    [Fact]
    public void ReversingUnknownRepayment_Throws()
    {
        var facility = TestData.ActiveFacility();
        var ex = Assert.Throws<DomainException>(
            () => facility.ReverseRepayment(Guid.NewGuid(), ReversalDate));
        Assert.Equal(DomainErrors.Repayment.NotFound, ex.ErrorCode);
    }
}
