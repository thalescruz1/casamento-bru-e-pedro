using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Domain.Gifts;
using MediatR;

namespace Casamento.Application.Gifts.Commands.DeleteGift;

public sealed record DeleteGiftCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteGiftHandler(
    IGiftRepository repository,
    IGiftImageStorage storage) : IRequestHandler<DeleteGiftCommand, Result>
{
    public async Task<Result> Handle(DeleteGiftCommand request, CancellationToken cancellationToken)
    {
        var gift = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (gift is null)
        {
            return Error.NotFound("Presente não encontrado.");
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
