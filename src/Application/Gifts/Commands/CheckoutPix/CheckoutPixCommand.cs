using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Application.Gifts.Dtos;
using Casamento.Domain.Gifts;
using FluentValidation;
using MediatR;

namespace Casamento.Application.Gifts.Commands.CheckoutPix;

public sealed record CheckoutPixCommand(Guid GiftId) : IRequest<Result<CheckoutPixResultDto>>;

public sealed class CheckoutPixValidator : AbstractValidator<CheckoutPixCommand>
{
    public CheckoutPixValidator()
    {
        RuleFor(x => x.GiftId).NotEmpty();
    }
}

public sealed class CheckoutPixHandler(
    IGiftRepository repository,
    IOwnPixProvider pixProvider,
    IPixQrCodeBuilder qrBuilder) : IRequestHandler<CheckoutPixCommand, Result<CheckoutPixResultDto>>
{
    public async Task<Result<CheckoutPixResultDto>> Handle(CheckoutPixCommand request, CancellationToken cancellationToken)
    {
        var gift = await repository.GetByIdAsync(request.GiftId, cancellationToken).ConfigureAwait(false);
        if (gift is null)
        {
            return Error.NotFound("Presente não encontrado.");
        }

        if (gift.Status != GiftStatus.Available)
        {
            return Error.Conflict("Este presente já não está disponível.");
        }

        var pix = pixProvider.Get();
        var reference = $"HT{gift.Id.ToString("N")[..10]}";
        var qr = qrBuilder.Build(pix, gift.Price.Amount, reference);

        return new CheckoutPixResultDto(
            gift.Id,
            pix.Key,
            pix.KeyType,
            pix.Beneficiary,
            pix.Bank,
            gift.Price.Amount,
            qr.Payload,
            qr.ImageBase64);
    }
}
