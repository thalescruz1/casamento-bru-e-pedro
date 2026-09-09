using Casamento.Domain.Common;

namespace Casamento.Domain.Gifts.ValueObjects;

public readonly record struct Money
{
    public const string DefaultCurrency = "BRL";

    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money FromBrl(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainException("Valor deve ser maior que zero.");
        }

        if (amount > 1_000_000m)
        {
            throw new DomainException("Valor máximo excedido.");
        }

        return new Money(decimal.Round(amount, 2, MidpointRounding.ToEven), DefaultCurrency);
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
