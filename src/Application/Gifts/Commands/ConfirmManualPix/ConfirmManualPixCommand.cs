using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Application.Gifts.Dtos;
using Casamento.Application.Gifts.Notifications;
using Casamento.Domain.Common;
using Casamento.Domain.Gifts;
using Casamento.Domain.Rsvps.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Casamento.Application.Gifts.Commands.ConfirmManualPix;

public sealed record ConfirmManualPixCommand(
    Guid GiftId,
    string BuyerName,
    string BuyerEmail,
    string? Message,
    string IpHash) : IRequest<Result<CheckoutCardResultDto>>;

public sealed class ConfirmManualPixValidator : AbstractValidator<ConfirmManualPixCommand>
{
    public ConfirmManualPixValidator()
    {
        RuleFor(x => x.GiftId).NotEmpty();
        RuleFor(x => x.BuyerName).NotEmpty().MinimumLength(2).MaximumLength(120);
        RuleFor(x => x.BuyerEmail).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Message).MaximumLength(Gift.MaxMessageLength);
        RuleFor(x => x.IpHash).NotEmpty();
    }
}

public sealed class ConfirmManualPixHandler(
    IGiftRepository repository,
    IGiftPaidNotifier notifier,
    IClock clock,
    ILogger<ConfirmManualPixHandler> logger) : IRequestHandler<ConfirmManualPixCommand, Result<CheckoutCardResultDto>>
{
    public async Task<Result<CheckoutCardResultDto>> Handle(ConfirmManualPixCommand request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        var gift = await repository.GetByIdAsync(request.GiftId, cancellationToken).ConfigureAwait(false);
        if (gift is null)
        {
            return Error.NotFound("Presente não encontrado.");
        }

        if (gift.Status != GiftStatus.Available)
        {
            return Error.Conflict("Este presente já não está disponível.");
        }

        Email buyerEmail;
        try
        {
            buyerEmail = Email.Create(request.BuyerEmail);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Message);
        }

        var manualPaymentId = $"manual-pix-{Guid.NewGuid():N}";

        try
        {
            gift.MarkPaid(request.BuyerName, buyerEmail, manualPaymentId, now, request.Message);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Message);
        }

        await repository.UpdateAsync(gift, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Gift {GiftId} marcado como pago via Pix manual por {Buyer}. ConfirmaÇão pendente de verificação pelos noivos.",
            gift.Id, request.BuyerEmail);

        try
        {
            await notifier.NotifyAsync(gift, new PaymentCustomer(request.BuyerName, request.BuyerEmail), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao enviar e-mails de confirmação (Pix manual) para gift {GiftId}", gift.Id);
        }

        return new CheckoutCardResultDto(gift.Id, manualPaymentId, "Confirmed");
    }
}
