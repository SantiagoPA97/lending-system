using Lending.Domain;
using Lending.Domain.Entities;

namespace Lending.Domain.Tests;

public class FacilityLifecycleTests
{
    private static Facility InStatus(FacilityStatus status)
    {
        var facility = TestData.DraftFacility();
        switch (status)
        {
            case FacilityStatus.Draft:
                break;
            case FacilityStatus.Active:
                facility.Activate(TestData.Calculator);
                break;
            case FacilityStatus.Completed:
                facility.Activate(TestData.Calculator);
                facility.RecordRepayment(new Money(10_000m, Currency.USD), TestData.Start.AddDays(1));
                break;
            case FacilityStatus.Cancelled:
                facility.Activate(TestData.Calculator);
                facility.Cancel();
                break;
            case FacilityStatus.Defaulted:
                facility.Activate(TestData.Calculator);
                facility.MarkDefaulted();
                break;
        }

        Assert.Equal(status, facility.Status);
        return facility;
    }

    [Fact]
    public void Create_StartsAsDraft_WithZeroOutstandingAndNoSchedule()
    {
        var facility = TestData.DraftFacility();
        Assert.Equal(FacilityStatus.Draft, facility.Status);
        Assert.Equal(0m, facility.OutstandingPrincipal);
        Assert.Empty(facility.Schedule);
        Assert.Empty(facility.Repayments);
    }

    [Theory]
    [InlineData(0, 5, 12)]
    [InlineData(-500, 5, 12)]
    [InlineData(100.005, 5, 12)]
    [InlineData(1000, -0.5, 12)]
    [InlineData(1000, 5, 0)]
    public void Create_WithInvalidTerms_Throws(decimal commitment, decimal rate, int term)
    {
        var ex = Assert.Throws<DomainException>(() => Facility.Create(
            TestData.Company(), new Money(commitment, Currency.USD), rate, term,
            TestData.Start, RepaymentType.Amortizing));
        Assert.Equal("facility.invalid_terms", ex.ErrorCode);
    }

    [Fact]
    public void Activate_GeneratesScheduleAndSetsOutstanding()
    {
        var facility = TestData.DraftFacility(commitment: 10_000m, termMonths: 3);
        facility.Activate(TestData.Calculator);

        Assert.Equal(FacilityStatus.Active, facility.Status);
        Assert.Equal(10_000m, facility.OutstandingPrincipal);
        Assert.Equal(3, facility.Schedule.Count);
        Assert.Equal(10_000m, facility.Schedule.Sum(i => i.PrincipalDue));
        Assert.All(facility.Schedule, i => Assert.Equal(facility.Id, i.FacilityId));
    }

    [Theory]
    [InlineData(FacilityStatus.Active)]
    [InlineData(FacilityStatus.Completed)]
    [InlineData(FacilityStatus.Cancelled)]
    [InlineData(FacilityStatus.Defaulted)]
    public void Activate_FromNonDraft_Throws(FacilityStatus status)
    {
        var ex = Assert.Throws<DomainException>(() => InStatus(status).Activate(TestData.Calculator));
        Assert.Equal("facility.invalid_transition", ex.ErrorCode);
    }

    [Theory]
    [InlineData(FacilityStatus.Draft)]
    [InlineData(FacilityStatus.Active)]
    public void Cancel_FromDraftOrActive_Succeeds(FacilityStatus status)
    {
        var facility = InStatus(status);
        facility.Cancel();
        Assert.Equal(FacilityStatus.Cancelled, facility.Status);
    }

    [Theory]
    [InlineData(FacilityStatus.Completed)]
    [InlineData(FacilityStatus.Cancelled)]
    [InlineData(FacilityStatus.Defaulted)]
    public void Cancel_FromTerminalStatus_Throws(FacilityStatus status)
    {
        var ex = Assert.Throws<DomainException>(InStatus(status).Cancel);
        Assert.Equal("facility.invalid_transition", ex.ErrorCode);
    }

    [Fact]
    public void MarkDefaulted_FromActive_Succeeds()
    {
        var facility = InStatus(FacilityStatus.Active);
        facility.MarkDefaulted();
        Assert.Equal(FacilityStatus.Defaulted, facility.Status);
    }

    [Theory]
    [InlineData(FacilityStatus.Draft)]
    [InlineData(FacilityStatus.Completed)]
    [InlineData(FacilityStatus.Cancelled)]
    [InlineData(FacilityStatus.Defaulted)]
    public void MarkDefaulted_FromNonActive_Throws(FacilityStatus status)
    {
        var ex = Assert.Throws<DomainException>(InStatus(status).MarkDefaulted);
        Assert.Equal("facility.invalid_transition", ex.ErrorCode);
    }

    [Fact]
    public void UpdateTerms_InDraft_ReplacesTerms()
    {
        var facility = TestData.DraftFacility();
        facility.UpdateTerms(new Money(25_000m, Currency.EUR), 8.5m, 24, TestData.Start.AddMonths(1), RepaymentType.InterestOnly);

        Assert.Equal(25_000m, facility.CommitmentAmount);
        Assert.Equal(Currency.EUR, facility.Currency);
        Assert.Equal(8.5m, facility.AnnualInterestRate);
        Assert.Equal(24, facility.TermMonths);
        Assert.Equal(RepaymentType.InterestOnly, facility.RepaymentType);
    }

    [Fact]
    public void UpdateTerms_AfterActivation_Throws()
    {
        var facility = InStatus(FacilityStatus.Active);
        var ex = Assert.Throws<DomainException>(() => facility.UpdateTerms(
            new Money(25_000m, Currency.USD), 8.5m, 24, TestData.Start, RepaymentType.Bullet));
        Assert.Equal("facility.not_editable", ex.ErrorCode);
    }
}
