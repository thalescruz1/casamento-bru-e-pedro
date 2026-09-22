using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Domain.Common;
using Casamento.Domain.Gifts;
using MediatR;

namespace Casamento.Application.Gifts.Commands.DeleteGift;

public sealed record DeleteGiftCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteGiftHandler(
    IGiftRepository repository,
    IGiftImageStorage storage,
    IClock clock) : IRequestHandler<DeleteGiftCommand, Result>
{
    public async Task<Result> Handle(DeleteGiftCommand request, CancellationToken cancellationToken)
    {
        var gift = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (gift is null)
        {
            return Error.NotFound("Presente não encontrado.");
        }

        // Presente com compras não pode ser removido de verdade — o histórico (quem comprou,
        // quanto pagou) continua existindo em Recebidos. Exclusão lógica: só some das listas.
        if (gift.PurchaseCount > 0)
        {
            try
            {
                gift.MarkDeleted(clock.UtcNow);
            }
            catch (DomainException ex)
            {
                return Error.Conflict(ex.Message);
            }

            try
            {
                await repository.UpdateAsync(gift, cancellationToken).ConfigureAwait(false);
            }
            catch (GiftConcurrencyException)
            {
                return Error.Conflict("O presente acabou de ser alterado (por exemplo, por uma nova compra). Recarregue a lista e tente de novo.");
            }

            return Result.Success();
        }

        if (gift.Status != GiftStatus.Available)
        {
            return Error.Conflict("Só é possível remover um presente disponível.");
        }

        if (gift.ImageBlobName is not null)
        {
            await storage.DeleteAsync(gift.ImageBlobName, cancellationToken).ConfigureAwait(false);
        }

        await repository.DeleteAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
