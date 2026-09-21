using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Application.Gifts.Dtos;
using Casamento.Application.Gifts.Mapping;
using Casamento.Domain.Common;
using Casamento.Domain.Gifts;
using Casamento.Domain.Gifts.ValueObjects;
using FluentValidation;
using MediatR;

namespace Casamento.Application.Gifts.Commands.UpdateGift;

public sealed record UpdateGiftCommand(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    string? ImageBlobName,
    int? MaxPurchases) : IRequest<Result<GiftAdminDto>>;

public sealed class UpdateGiftValidator : AbstractValidator<UpdateGiftCommand>
{
    public UpdateGiftValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(120);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Price).GreaterThan(0).LessThan(1_000_000m);
        RuleFor(x => x.MaxPurchases).InclusiveBetween(1, Gift.MaxPurchasesLimit).When(x => x.MaxPurchases.HasValue);
    }
}

public sealed class UpdateGiftHandler(
    IGiftRepository repository,
    IGiftImageStorage storage,
    IClock clock) : IRequestHandler<UpdateGiftCommand, Result<GiftAdminDto>>
{
    public async Task<Result<GiftAdminDto>> Handle(UpdateGiftCommand request, CancellationToken cancellationToken)
    {
        var gift = await repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (gift is null)
        {
            return Error.NotFound("Presente não encontrado.");
        }

        try
        {
            gift.UpdateDetails(
                request.Title,
                request.Description,
                Money.FromBrl(request.Price),
                request.ImageBlobName,
                request.MaxPurchases,
                clock.UtcNow);
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

        return GiftMapping.ToAdminDto(gift, storage);
    }
}
