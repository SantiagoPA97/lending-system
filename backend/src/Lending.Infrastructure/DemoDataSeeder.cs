using Lending.Domain;
using Lending.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lending.Infrastructure;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(LendingDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Companies.AnyAsync(cancellationToken))
            return;

        var calculator = new ScheduleCalculator();

        var andes = new Company("Andes Coffee Exports", "Andes Coffee Exports S.A.S.", "CO-900123456", "CO", "Agriculture", "finance@andescoffee.co");
        var nordwind = new Company("Nordwind Logistics", "Nordwind Logistics GmbH", "DE-HRB98214", "DE", "Logistics", "treasury@nordwind.de");
        var thames = new Company("Thames Analytics", "Thames Analytics Ltd", "GB-11482930", "GB", "Software", "cfo@thamesanalytics.co.uk");
        var pacific = new Company("Pacific Crest Manufacturing", "Pacific Crest Manufacturing Inc.", "US-EIN-84-1029384", "US", "Manufacturing", "ap@pacificcrest.com");
        var iberia = new Company("Iberia Solar Partners", "Iberia Solar Partners S.L.", "ES-B87654321", "ES", "Renewable Energy", "finanzas@iberiasolar.es");
        var medellin = new Company("Medellin Textiles", "Medellin Textiles S.A.S.", "CO-901567890", "CO", "Textiles", "tesoreria@medellintextiles.co");
        var greatLakes = new Company("Great Lakes Foods", "Great Lakes Foods Inc.", "US-EIN-38-5647382", "US", "Food & Beverage", "finance@greatlakesfoods.com");
        var alpine = new Company("Alpine Precision", "Alpine Precision AG", "CH-CHE-114.618.907", "CH", "Industrial Equipment", "finance@alpineprecision.ch");
        var celtic = new Company("Celtic Maritime", "Celtic Maritime Ltd", "IE-654321", "IE", "Shipping", "accounts@celticmaritime.ie");
        var bayside = new Company("Bayside Robotics", "Bayside Robotics Corp.", "US-EIN-77-2938475", "US", "Robotics", "cfo@baysiderobotics.com");

        var companies = new[] { andes, nordwind, thames, pacific, iberia, medellin, greatLakes, alpine, celtic, bayside };

        var facilities = new List<Facility>();

        Facility Add(Company company, decimal amount, Currency currency, decimal rate, int termMonths, DateOnly start, RepaymentType type)
        {
            var facility = Facility.Create(company, new Money(amount, currency), rate, termMonths, start, type);
            facilities.Add(facility);
            return facility;
        }

        Facility AddActive(Company company, decimal amount, Currency currency, decimal rate, int termMonths, DateOnly start, RepaymentType type, int installmentsPaid = 0)
        {
            var facility = Add(company, amount, currency, rate, termMonths, start, type);
            facility.Activate(calculator);
            PayInstallments(facility, installmentsPaid);
            return facility;
        }

        AddActive(andes, 500_000m, Currency.USD, 8.5m, 24, new DateOnly(2025, 3, 15), RepaymentType.Amortizing, installmentsPaid: 10);
        AddActive(andes, 850_000_000m, Currency.COP, 16.2m, 36, new DateOnly(2025, 6, 1), RepaymentType.Amortizing, installmentsPaid: 8);
        AddActive(andes, 150_000m, Currency.USD, 7.0m, 12, new DateOnly(2026, 7, 1), RepaymentType.Bullet);

        AddActive(nordwind, 1_200_000m, Currency.EUR, 5.75m, 48, new DateOnly(2025, 2, 10), RepaymentType.Amortizing, installmentsPaid: 17);
        var completed = AddActive(nordwind, 300_000m, Currency.EUR, 6.1m, 12, new DateOnly(2025, 4, 20), RepaymentType.InterestOnly, installmentsPaid: 12);
        Add(nordwind, 2_000_000m, Currency.EUR, 4.9m, 60, new DateOnly(2026, 5, 15), RepaymentType.Amortizing);

        AddActive(thames, 750_000m, Currency.GBP, 7.25m, 36, new DateOnly(2025, 5, 1), RepaymentType.Amortizing, installmentsPaid: 12);
        AddActive(thames, 250_000m, Currency.GBP, 8.0m, 18, new DateOnly(2025, 9, 1), RepaymentType.InterestOnly, installmentsPaid: 9);
        Add(thames, 400_000m, Currency.GBP, 6.5m, 24, new DateOnly(2026, 6, 10), RepaymentType.Bullet);

        AddActive(pacific, 3_500_000m, Currency.USD, 6.8m, 60, new DateOnly(2025, 1, 20), RepaymentType.Amortizing, installmentsPaid: 18);
        var defaulted = AddActive(pacific, 800_000m, Currency.USD, 9.5m, 24, new DateOnly(2025, 3, 5), RepaymentType.InterestOnly, installmentsPaid: 6);
        defaulted.MarkDefaulted();
        var withReversal = AddActive(pacific, 1_000_000m, Currency.USD, 7.75m, 36, new DateOnly(2026, 4, 1), RepaymentType.Amortizing, installmentsPaid: 3);
        withReversal.ReverseRepayment(withReversal.Repayments[^1].Id, new DateOnly(2026, 7, 5));

        AddActive(iberia, 2_500_000m, Currency.EUR, 5.4m, 48, new DateOnly(2025, 8, 15), RepaymentType.Amortizing, installmentsPaid: 11);
        AddActive(iberia, 600_000m, Currency.EUR, 6.9m, 24, new DateOnly(2026, 2, 1), RepaymentType.InterestOnly, installmentsPaid: 5);
        AddActive(iberia, 450_000m, Currency.EUR, 5.9m, 12, new DateOnly(2025, 10, 1), RepaymentType.Bullet);

        AddActive(medellin, 400_000_000m, Currency.COP, 18.5m, 24, new DateOnly(2025, 7, 15), RepaymentType.Amortizing, installmentsPaid: 12);
        AddActive(medellin, 250_000_000m, Currency.COP, 17.0m, 12, new DateOnly(2026, 3, 1), RepaymentType.Bullet);
        Add(medellin, 600_000_000m, Currency.COP, 16.75m, 36, new DateOnly(2026, 7, 20), RepaymentType.Amortizing);

        AddActive(greatLakes, 1_750_000m, Currency.USD, 6.25m, 36, new DateOnly(2025, 4, 10), RepaymentType.Amortizing, installmentsPaid: 15);
        var cancelledActive = AddActive(greatLakes, 500_000m, Currency.USD, 7.5m, 18, new DateOnly(2025, 11, 1), RepaymentType.InterestOnly);
        cancelledActive.Cancel();
        Add(greatLakes, 900_000m, Currency.USD, 6.75m, 24, new DateOnly(2026, 6, 15), RepaymentType.Amortizing);

        AddActive(alpine, 1_100_000m, Currency.EUR, 5.25m, 36, new DateOnly(2025, 3, 1), RepaymentType.Amortizing, installmentsPaid: 16);
        AddActive(celtic, 2_200_000m, Currency.GBP, 7.9m, 48, new DateOnly(2025, 5, 20), RepaymentType.Amortizing, installmentsPaid: 13);

        var cancelledDraft = Add(bayside, 350_000m, Currency.USD, 8.25m, 12, new DateOnly(2026, 1, 15), RepaymentType.Bullet);
        cancelledDraft.Cancel();
        AddActive(bayside, 1_500_000m, Currency.USD, 7.1m, 36, new DateOnly(2026, 5, 1), RepaymentType.Amortizing, installmentsPaid: 2);

        if (completed.Status != FacilityStatus.Completed)
            throw new InvalidOperationException("Demo seed expected a completed facility.");

        alpine.Deactivate();
        celtic.Deactivate();

        db.Companies.AddRange(companies);
        db.Facilities.AddRange(facilities);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void PayInstallments(Facility facility, int count)
    {
        foreach (var item in facility.Schedule.OrderBy(i => i.Period).Take(count))
            facility.RecordRepayment(new Money(item.InterestDue + item.PrincipalDue, facility.Currency), item.DueDate);
    }
}
