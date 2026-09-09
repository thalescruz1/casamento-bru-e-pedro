namespace Casamento.Application.Abstractions;

public sealed record OwnPixInfo(
    string Key,
    string KeyType,
    string Beneficiary,
    string? Bank,
    string City);

public interface IOwnPixProvider
{
    OwnPixInfo Get();
}
