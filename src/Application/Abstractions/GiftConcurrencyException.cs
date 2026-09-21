namespace Casamento.Application.Abstractions;

/// <summary>
/// O presente foi alterado por outra requisição entre a leitura e a gravação
/// (ex.: duas compras quase simultâneas do mesmo presente).
/// </summary>
public sealed class GiftConcurrencyException(Guid giftId, Exception? inner = null)
    : Exception($"O presente {giftId:N} foi alterado por outra operação.", inner)
{
    public Guid GiftId { get; } = giftId;
}
