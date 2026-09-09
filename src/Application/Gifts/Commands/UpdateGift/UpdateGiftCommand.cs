using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Application.Gifts.Dtos;
using Casamento.Application.Gifts.Mapping;
using Casamento.Domain.Common;
using Casamento.Domain.Gifts.ValueObjects;
using FluentValidation;
using MediatR;

namespace Casamento.Application.Gifts.Commands.UpdateGift;

public sealed record UpdateGiftCommand(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    string? ImageBlobName) : IRequest<Result<GiftAdminDto>>;

public sealed class UpdateGiftValidator : AbstractValidator<UpdateGiftCommand>
{
    public UpdateGiftValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(120);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Price).GreaterThan(0).LessThan(1_000_000m);
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
                clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return Error.Conflict(ex.Message);
        }

        await repository.UpdateAsync(gift, cancellationToken).ConfigureAwait(false);
        return GiftMapping.ToAdminDto(gift, storage);
    }
}
