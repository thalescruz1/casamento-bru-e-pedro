using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Application.Gifts.Dtos;
using Casamento.Application.Gifts.Mapping;
using Casamento.Domain.Common;
using Casamento.Domain.Gifts;
using Casamento.Domain.Gifts.ValueObjects;
using FluentValidation;
using MediatR;

namespace Casamento.Application.Gifts.Commands.CreateGift;

public sealed record CreateGiftCommand(
    string Title,
    string Description,
    decimal Price,
    string? ImageBlobName) : IRequest<Result<GiftAdminDto>>;

public sealed class CreateGiftValidator : AbstractValidator<CreateGiftCommand>
{
    public CreateGiftValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(120);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Price).GreaterThan(0).LessThan(1_000_000m);
    }
}

public sealed class CreateGiftHandler(
    IGiftRepository repository,
    IGiftImageStorage storage,
    IClock clock) : IRequestHandler<CreateGiftCommand, Result<GiftAdminDto>>
{
    public async Task<Result<GiftAdminDto>> Handle(CreateGiftCommand request, CancellationToken cancellationToken)
    {
        Gift gift;
        try
        {
            gift = Gift.Create(
                request.Title,
                request.Description,
                Money.FromBrl(request.Price),
                request.ImageBlobName,
                clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Message);
        }

        await repository.AddAsync(gift, cancellationToken).ConfigureAwait(false);
        return GiftMapping.ToAdminDto(gift, storage);
    }
}
