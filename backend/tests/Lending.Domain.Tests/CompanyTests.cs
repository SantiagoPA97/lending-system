using Lending.Domain;
using Lending.Domain.Entities;

namespace Lending.Domain.Tests;

public class CompanyTests
{
    [Fact]
    public void NewCompany_IsActive()
    {
        Assert.Equal(CompanyStatus.Active, TestData.Company().Status);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var company = TestData.Company();
        company.Deactivate();
        Assert.Equal(CompanyStatus.Inactive, company.Status);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_Throws()
    {
        var company = TestData.Company();
        company.Deactivate();
        var ex = Assert.Throws<DomainException>(company.Deactivate);
        Assert.Equal("company.invalid_transition", ex.ErrorCode);
    }

    [Fact]
    public void Activate_ReactivatesInactiveCompany()
    {
        var company = TestData.Company();
        company.Deactivate();
        company.Activate();
        Assert.Equal(CompanyStatus.Active, company.Status);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_Throws()
    {
        var ex = Assert.Throws<DomainException>(TestData.Company().Activate);
        Assert.Equal("company.invalid_transition", ex.ErrorCode);
    }

    [Fact]
    public void InactiveCompany_CannotReceiveNewFacility()
    {
        var company = TestData.Company();
        company.Deactivate();
        var ex = Assert.Throws<DomainException>(() => Facility.Create(
            company, new Money(1_000m, Currency.USD), 5m, 12, TestData.Start, RepaymentType.Bullet));
        Assert.Equal("company.inactive", ex.ErrorCode);
    }
}
