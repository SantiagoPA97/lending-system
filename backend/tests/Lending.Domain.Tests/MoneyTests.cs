using Lending.Domain;

namespace Lending.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Add_SameCurrency_SumsAmounts()
    {
        var result = new Money(10.50m, Currency.USD) + new Money(4.25m, Currency.USD);
        Assert.Equal(new Money(14.75m, Currency.USD), result);
    }

    [Fact]
    public void Subtract_SameCurrency_SubtractsAmounts()
    {
        var result = new Money(10.50m, Currency.EUR) - new Money(4.25m, Currency.EUR);
        Assert.Equal(new Money(6.25m, Currency.EUR), result);
    }

    [Fact]
    public void Add_DifferentCurrency_Throws()
    {
        var ex = Assert.Throws<DomainException>(
            () => new Money(1m, Currency.USD) + new Money(1m, Currency.EUR));
        Assert.Equal(DomainErrors.Money.CurrencyMismatch, ex.ErrorCode);
    }

    [Fact]
    public void Subtract_DifferentCurrency_Throws()
    {
        var ex = Assert.Throws<DomainException>(
            () => new Money(1m, Currency.GBP) - new Money(1m, Currency.COP));
        Assert.Equal(DomainErrors.Money.CurrencyMismatch, ex.ErrorCode);
    }

    [Fact]
    public void Compare_DifferentCurrency_Throws()
    {
        var ex = Assert.Throws<DomainException>(
            () => new Money(1m, Currency.USD) > new Money(1m, Currency.EUR));
        Assert.Equal(DomainErrors.Money.CurrencyMismatch, ex.ErrorCode);
    }

    [Fact]
    public void Comparison_SameCurrency_Works()
    {
        Assert.True(new Money(2m, Currency.USD) > new Money(1m, Currency.USD));
        Assert.True(new Money(1m, Currency.USD) < new Money(2m, Currency.USD));
        Assert.True(new Money(2m, Currency.USD) >= new Money(2m, Currency.USD));
        Assert.True(new Money(2m, Currency.USD) <= new Money(2m, Currency.USD));
    }

    [Fact]
    public void Negate_FlipsSign()
    {
        Assert.Equal(new Money(-5m, Currency.USD), -new Money(5m, Currency.USD));
    }

    [Fact]
    public void Multiply_ScalesAmount()
    {
        Assert.Equal(new Money(25m, Currency.USD), new Money(10m, Currency.USD) * 2.5m);
        Assert.Equal(new Money(25m, Currency.USD), 2.5m * new Money(10m, Currency.USD));
    }

    [Theory]
    [InlineData("2.005", "2.00")]
    [InlineData("2.015", "2.02")]
    [InlineData("2.025", "2.02")]
    [InlineData("2.675", "2.68")]
    [InlineData("10.985", "10.98")]
    [InlineData("-2.005", "-2.00")]
    [InlineData("3.14159", "3.14")]
    public void Round_UsesBankersRoundingAtTwoDecimals(string input, string expected)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var rounded = new Money(decimal.Parse(input, culture), Currency.USD).Round();
        Assert.Equal(decimal.Parse(expected, culture), rounded.Amount);
    }

    [Fact]
    public void Zero_HasZeroAmount()
    {
        var zero = Money.Zero(Currency.GBP);
        Assert.True(zero.IsZero);
        Assert.Equal(Currency.GBP, zero.Currency);
    }
}
