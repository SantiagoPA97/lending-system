namespace Lending.Domain;

public sealed record ScheduleTerms(
    decimal Principal,
    decimal AnnualInterestRate,
    int TermMonths,
    DateOnly StartDate,
    RepaymentType RepaymentType);

public sealed record SchedulePeriod(
    int Period,
    DateOnly DueDate,
    decimal PrincipalDue,
    decimal InterestDue,
    decimal RemainingBalance);

public interface IScheduleCalculator
{
    IReadOnlyList<SchedulePeriod> Generate(ScheduleTerms terms);
}

public sealed class ScheduleCalculator : IScheduleCalculator
{
    IReadOnlyList<SchedulePeriod> IScheduleCalculator.Generate(ScheduleTerms terms) => Calculate(terms);

    // Convention: monthly rate = annual/12 (30/360), AnnualInterestRate expressed in percent (7.35 == 7.35%).
    public static IReadOnlyList<SchedulePeriod> Calculate(ScheduleTerms terms)
    {
        Validate(terms);
        var monthlyRate = terms.AnnualInterestRate / 1200m;

        return terms.RepaymentType switch
        {
            RepaymentType.Bullet => Bullet(terms, monthlyRate),
            RepaymentType.InterestOnly => InterestOnly(terms, monthlyRate),
            RepaymentType.Amortizing => Amortizing(terms, monthlyRate),
            _ => throw new DomainException(DomainErrors.Schedule.InvalidTerms, $"Unknown repayment type {terms.RepaymentType}.")
        };
    }

    private static void Validate(ScheduleTerms terms)
    {
        if (terms.Principal <= 0m)
            throw new DomainException(DomainErrors.Schedule.InvalidTerms, "Principal must be positive.");
        if (MoneyMath.Round(terms.Principal) != terms.Principal)
            throw new DomainException(DomainErrors.Schedule.InvalidTerms, "Principal cannot have more than two decimal places.");
        if (terms.AnnualInterestRate < 0m)
            throw new DomainException(DomainErrors.Schedule.InvalidTerms, "Interest rate cannot be negative.");
        if (terms.TermMonths < 1)
            throw new DomainException(DomainErrors.Schedule.InvalidTerms, "Term must be at least one month.");
    }

    private static List<SchedulePeriod> Bullet(ScheduleTerms terms, decimal monthlyRate)
    {
        var interest = MoneyMath.Round(terms.Principal * monthlyRate * terms.TermMonths);
        return
        [
            new SchedulePeriod(
                terms.TermMonths,
                terms.StartDate.AddMonths(terms.TermMonths),
                terms.Principal,
                interest,
                0m)
        ];
    }

    private static List<SchedulePeriod> InterestOnly(ScheduleTerms terms, decimal monthlyRate)
    {
        var periodInterest = MoneyMath.Round(terms.Principal * monthlyRate);
        var schedule = new List<SchedulePeriod>(terms.TermMonths);
        for (var period = 1; period <= terms.TermMonths; period++)
        {
            var isLast = period == terms.TermMonths;
            schedule.Add(new SchedulePeriod(
                period,
                terms.StartDate.AddMonths(period),
                isLast ? terms.Principal : 0m,
                periodInterest,
                isLast ? 0m : terms.Principal));
        }

        return schedule;
    }

    private static List<SchedulePeriod> Amortizing(ScheduleTerms terms, decimal monthlyRate)
    {
        var payment = AnnuityPayment(terms.Principal, monthlyRate, terms.TermMonths);
        var schedule = new List<SchedulePeriod>(terms.TermMonths);
        var balance = terms.Principal;
        for (var period = 1; period <= terms.TermMonths; period++)
        {
            var interest = MoneyMath.Round(balance * monthlyRate);
            // Final period absorbs cumulative rounding so principal sums exactly to the commitment.
            var principal = period == terms.TermMonths
                ? balance
                : Math.Min(payment - interest, balance);
            balance -= principal;
            schedule.Add(new SchedulePeriod(
                period,
                terms.StartDate.AddMonths(period),
                principal,
                interest,
                balance));
        }

        return schedule;
    }

    private static decimal AnnuityPayment(decimal principal, decimal monthlyRate, int termMonths)
    {
        if (monthlyRate == 0m)
            return MoneyMath.Round(principal / termMonths);

        var factor = 1m;
        for (var i = 0; i < termMonths; i++)
            factor *= 1m + monthlyRate;

        return MoneyMath.Round(principal * monthlyRate * factor / (factor - 1m));
    }
}
