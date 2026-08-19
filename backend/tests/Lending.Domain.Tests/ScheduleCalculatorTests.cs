using Lending.Domain;

namespace Lending.Domain.Tests;

public class ScheduleCalculatorTests
{
    private static readonly DateOnly Start = new(2026, 1, 15);

    public static TheoryData<decimal, decimal, int> TermCases => new()
    {
        { 100_000m, 7.35m, 13 },
        { 50_000m, 5.5m, 7 },
        { 1_000_000m, 12.99m, 36 },
        { 12_345.67m, 9.87m, 11 },
        { 999.99m, 0m, 5 },
        { 250_000m, 21.5m, 60 },
        { 77_777.77m, 3.33m, 24 }
    };

    public static TheoryData<decimal, decimal, int, RepaymentType> AllTypeCases
    {
        get
        {
            var data = new TheoryData<decimal, decimal, int, RepaymentType>();
            foreach (var row in TermCases)
            {
                foreach (var type in Enum.GetValues<RepaymentType>())
                    data.Add((decimal)row[0], (decimal)row[1], (int)row[2], type);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(AllTypeCases))]
    public void PrincipalSumsExactlyToCommitment_ForAllTypes(
        decimal principal, decimal rate, int term, RepaymentType type)
    {
        var schedule = ScheduleCalculator.Calculate(new ScheduleTerms(principal, rate, term, Start, type));

        Assert.Equal(principal, schedule.Sum(p => p.PrincipalDue));
        Assert.Equal(0m, schedule[^1].RemainingBalance);
        Assert.All(schedule, p =>
        {
            Assert.Equal(MoneyMath.Round(p.PrincipalDue), p.PrincipalDue);
            Assert.Equal(MoneyMath.Round(p.InterestDue), p.InterestDue);
            Assert.Equal(MoneyMath.Round(p.RemainingBalance), p.RemainingBalance);
            Assert.True(p.PrincipalDue >= 0m);
            Assert.True(p.InterestDue >= 0m);
        });
    }

    [Theory]
    [MemberData(nameof(TermCases))]
    public void Amortizing_HasOnePeriodPerMonth_WithDecreasingBalance(
        decimal principal, decimal rate, int term)
    {
        var schedule = ScheduleCalculator.Calculate(
            new ScheduleTerms(principal, rate, term, Start, RepaymentType.Amortizing));

        Assert.Equal(term, schedule.Count);
        var balance = principal;
        foreach (var period in schedule)
        {
            Assert.True(period.RemainingBalance < balance);
            Assert.Equal(balance - period.PrincipalDue, period.RemainingBalance);
            balance = period.RemainingBalance;
        }

        Assert.Equal(
            Enumerable.Range(1, term).Select(Start.AddMonths),
            schedule.Select(p => p.DueDate));
    }

    [Fact]
    public void Amortizing_KnownCase_100k_12pct_12m()
    {
        var schedule = ScheduleCalculator.Calculate(
            new ScheduleTerms(100_000m, 12m, 12, Start, RepaymentType.Amortizing));

        // Annuity payment for 100,000 at 1%/month over 12 months is 8,884.88.
        Assert.Equal(1_000.00m, schedule[0].InterestDue);
        Assert.Equal(7_884.88m, schedule[0].PrincipalDue);
        Assert.Equal(92_115.12m, schedule[0].RemainingBalance);
        Assert.Equal(100_000m, schedule.Sum(p => p.PrincipalDue));
    }

    [Fact]
    public void Amortizing_KnownCase_10k_12pct_3m()
    {
        var schedule = ScheduleCalculator.Calculate(
            new ScheduleTerms(10_000m, 12m, 3, Start, RepaymentType.Amortizing));

        Assert.Equal(3, schedule.Count);
        Assert.Equal((100.00m, 3_300.22m, 6_699.78m),
            (schedule[0].InterestDue, schedule[0].PrincipalDue, schedule[0].RemainingBalance));
        Assert.Equal((67.00m, 3_333.22m, 3_366.56m),
            (schedule[1].InterestDue, schedule[1].PrincipalDue, schedule[1].RemainingBalance));
        Assert.Equal((33.67m, 3_366.56m, 0m),
            (schedule[2].InterestDue, schedule[2].PrincipalDue, schedule[2].RemainingBalance));
    }

    [Fact]
    public void Amortizing_ZeroRate_SplitsPrincipalEvenly_NoInterest()
    {
        var schedule = ScheduleCalculator.Calculate(
            new ScheduleTerms(999.99m, 0m, 5, Start, RepaymentType.Amortizing));

        Assert.All(schedule, p => Assert.Equal(0m, p.InterestDue));
        Assert.Equal(999.99m, schedule.Sum(p => p.PrincipalDue));
        Assert.All(schedule.Take(4), p => Assert.Equal(200.00m, p.PrincipalDue));
        Assert.Equal(199.99m, schedule[^1].PrincipalDue);
    }

    [Fact]
    public void Bullet_SinglePaymentOfPrincipalAndAllInterestAtMaturity()
    {
        var schedule = ScheduleCalculator.Calculate(
            new ScheduleTerms(100_000m, 7.35m, 13, Start, RepaymentType.Bullet));

        var item = Assert.Single(schedule);
        Assert.Equal(13, item.Period);
        Assert.Equal(Start.AddMonths(13), item.DueDate);
        Assert.Equal(100_000m, item.PrincipalDue);
        Assert.Equal(7_962.50m, item.InterestDue); // 100,000 * 7.35%/12 * 13
        Assert.Equal(0m, item.RemainingBalance);
    }

    [Fact]
    public void InterestOnly_MonthlyInterestWithBalloonPrincipalAtMaturity()
    {
        var schedule = ScheduleCalculator.Calculate(
            new ScheduleTerms(100_000m, 7.35m, 13, Start, RepaymentType.InterestOnly));

        Assert.Equal(13, schedule.Count);
        Assert.All(schedule, p => Assert.Equal(612.50m, p.InterestDue)); // 100,000 * 7.35%/12
        Assert.All(schedule.Take(12), p =>
        {
            Assert.Equal(0m, p.PrincipalDue);
            Assert.Equal(100_000m, p.RemainingBalance);
        });
        Assert.Equal(100_000m, schedule[^1].PrincipalDue);
        Assert.Equal(0m, schedule[^1].RemainingBalance);
    }

    [Theory]
    [MemberData(nameof(TermCases))]
    public void InterestOnly_TotalInterest_EqualsBullet_ForIdenticalTerms(
        decimal principal, decimal rate, int term)
    {
        var bullet = ScheduleCalculator.Calculate(
            new ScheduleTerms(principal, rate, term, Start, RepaymentType.Bullet));
        var interestOnly = ScheduleCalculator.Calculate(
            new ScheduleTerms(principal, rate, term, Start, RepaymentType.InterestOnly));

        // The final InterestOnly period reconciles per-period rounding, so both
        // types charge exactly the full-term simple interest.
        Assert.Equal(bullet.Sum(p => p.InterestDue), interestOnly.Sum(p => p.InterestDue));
    }

    [Fact]
    public void InterestOnly_FinalPeriodAbsorbsInterestRounding()
    {
        var schedule = ScheduleCalculator.Calculate(
            new ScheduleTerms(12_345.67m, 9.87m, 11, Start, RepaymentType.InterestOnly));

        Assert.All(schedule.Take(10), p => Assert.Equal(101.54m, p.InterestDue));
        Assert.Equal(101.57m, schedule[^1].InterestDue);
        Assert.Equal(1_116.97m, schedule.Sum(p => p.InterestDue)); // round(12,345.67 * 9.87%/12 * 11)
    }

    [Theory]
    [InlineData(0, 5, 12)]
    [InlineData(-100, 5, 12)]
    [InlineData(100.005, 5, 12)]
    [InlineData(1000, -1, 12)]
    [InlineData(1000, 5, 0)]
    public void InvalidTerms_Throw(decimal principal, decimal rate, int term)
    {
        foreach (var type in Enum.GetValues<RepaymentType>())
        {
            var ex = Assert.Throws<DomainException>(() => ScheduleCalculator.Calculate(
                new ScheduleTerms(principal, rate, term, Start, type)));
            Assert.Equal(DomainErrors.Schedule.InvalidTerms, ex.ErrorCode);
        }
    }
}
