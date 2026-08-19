using Lending.Domain;
using Lending.Domain.Entities;

namespace Lending.Domain.Tests;

internal static class TestData
{
    public static readonly DateOnly Start = new(2026, 1, 15);
    public static readonly IScheduleCalculator Calculator = new ScheduleCalculator();

    public static Company Company() => new(
        "Acme Corp", "Acme Corporation S.A.", "REG-001", "US", "Manufacturing", "finance@acme.test");

    public static Facility DraftFacility(
        decimal commitment = 10_000m,
        decimal rate = 12m,
        int termMonths = 3,
        RepaymentType type = RepaymentType.Amortizing,
        Currency currency = Currency.USD)
        => Facility.Create(Company(), new Money(commitment, currency), rate, termMonths, Start, type);

    public static Facility ActiveFacility(
        decimal commitment = 10_000m,
        decimal rate = 12m,
        int termMonths = 3,
        RepaymentType type = RepaymentType.Amortizing,
        Currency currency = Currency.USD)
    {
        var facility = DraftFacility(commitment, rate, termMonths, type, currency);
        facility.Activate(Calculator);
        return facility;
    }
}
