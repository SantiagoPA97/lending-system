namespace Lending.Domain;

public readonly record struct Money(decimal Amount, Currency Currency)
{
    public static Money Zero(Currency currency) => new(0m, currency);

    public bool IsZero => Amount == 0m;

    public bool IsNegative => Amount < 0m;

    public Money Round() => this with { Amount = MoneyMath.Round(Amount) };

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left with { Amount = left.Amount + right.Amount };
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left with { Amount = left.Amount - right.Amount };
    }

    public static Money operator -(Money value) => value with { Amount = -value.Amount };

    public static Money operator *(Money value, decimal factor) => value with { Amount = value.Amount * factor };

    public static Money operator *(decimal factor, Money value) => value * factor;

    public static bool operator >(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount >= right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount <= right.Amount;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new DomainException(
                DomainErrors.Money.CurrencyMismatch,
                $"Cannot operate on amounts in {left.Currency} and {right.Currency}.");
        }
    }
}

public static class MoneyMath
{
    public static decimal Round(decimal amount) => decimal.Round(amount, 2, MidpointRounding.ToEven);
}
