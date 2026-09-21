using Casamento.Application.Abstractions;
using Casamento.Domain.Gifts;

namespace Casamento.Application.Gifts;

/// <summary>
/// Registra uma compra e grava o presente. Como um presente pode ter várias compras, duas
/// gravações concorrentes se sobrescreveriam e uma compra ficaria sem registro: nesse caso o
/// presente é relido e a compra é reaplicada.
/// </summary>
internal static class GiftPurchaseRecorder
{
    private const int MaxAttempts = 5;

    public static async Task<(Gift Gift, bool Recorded)> RecordAsync(
        IGiftRepository repository,
        Gift gift,
        Func<Gift, bool> apply,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var recorded = apply(gift);

            try
            {
                await repository.UpdateAsync(gift, cancellationToken).ConfigureAwait(false);
                return (gift, recorded);
            }
            catch (GiftConcurrencyException) when (attempt < MaxAttempts)
            {
                gift = await repository.GetByIdAsync(gift.Id, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("O presente foi removido durante o registro da compra.");
            }
        }
    }
}
