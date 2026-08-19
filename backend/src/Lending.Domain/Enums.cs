namespace Lending.Domain;

public enum CompanyStatus
{
    Active,
    Inactive
}

public enum FacilityStatus
{
    Draft,
    Active,
    Completed,
    Cancelled,
    Defaulted
}

public enum RepaymentType
{
    Bullet,
    Amortizing,
    InterestOnly
}

public enum Currency
{
    USD,
    EUR,
    GBP,
    COP
}
