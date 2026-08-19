using Lending.Domain;

namespace Lending.Domain.Tests;

public class DomainErrorsTests
{
    // Error codes are wire values (RFC7807 errorCode extension) that the frontend
    // maps to messages. Pin a representative sample so a rename cannot slip through.
    [Fact]
    public void ErrorCodes_MatchWireContract()
    {
        Assert.Equal("facility.invalid_transition", DomainErrors.Facility.InvalidTransition);
        Assert.Equal("repayment.exceeds_outstanding", DomainErrors.Repayment.ExceedsOutstanding);
        Assert.Equal("money.currency_mismatch", DomainErrors.Money.CurrencyMismatch);
        Assert.Equal("company.inactive", DomainErrors.Company.Inactive);
        Assert.Equal("schedule.invalid_terms", DomainErrors.Schedule.InvalidTerms);
        Assert.Equal("repayment.facility_closed", DomainErrors.Repayment.FacilityClosed);
        Assert.Equal("repayment.before_start", DomainErrors.Repayment.BeforeStart);
    }
}
