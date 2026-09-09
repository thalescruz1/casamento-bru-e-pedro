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

namespace Casamento.Application.Gifts.Commands.CheckoutCard;

public sealed record CheckoutCardCommand(
    Guid GiftId,
    string BuyerName,
    string BuyerEmail,
    string BuyerDocument,
    string Phone,
    string PostalCode,
    string AddressNumber,
    string CardHolderName,
    string CardNumber,
    string CardExpiryMonth,
    string CardExpiryYear,
    string CardCcv,
    string? Message,
    string IpHash) : IRequest<Result<CheckoutCardResultDto>>;

public sealed class CheckoutCardValidator : AbstractValidator<CheckoutCardCommand>
{
    public CheckoutCardValidator()
    {
        RuleFor(x => x.GiftId).NotEmpty();
        RuleFor(x => x.BuyerName).NotEmpty().MinimumLength(2).MaximumLength(120);
        RuleFor(x => x.BuyerEmail).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.BuyerDocument).NotEmpty().Must(HasValidDigits).WithMessage("CPF ou CNPJ inválido.");
        RuleFor(x => x.Phone).NotEmpty().Must(x => x.Count(char.IsDigit) is >= 10 and <= 11);
        RuleFor(x => x.PostalCode).NotEmpty().Must(x => x.Count(char.IsDigit) == 8).WithMessage("CEP inválido.");
        RuleFor(x => x.AddressNumber).NotEmpty().MaximumLength(10);
        RuleFor(x => x.CardHolderName).NotEmpty().MinimumLength(2);
        RuleFor(x => x.CardNumber).NotEmpty().Must(x => x.Count(char.IsDigit) is >= 13 and <= 19);
        RuleFor(x => x.CardExpiryMonth).NotEmpty().Matches("^(0?[1-9]|1[0-2])$");
        RuleFor(x => x.CardExpiryYear).NotEmpty().Matches(@"^\d{4}$");
        RuleFor(x => x.CardCcv).NotEmpty().Matches(@"^\d{3,4}$");
        RuleFor(x => x.Message).MaximumLength(Gift.MaxMessageLength);
        RuleFor(x => x.IpHash).NotEmpty();
    }

    private static bool HasValidDigits(string document)
    {
        var digits = document.Count(char.IsDigit);
        return digits is 11 or 14;
    }
}

public sealed class CheckoutCardHandler(
    IGiftRepository repository,
    IPaymentGateway gateway,
    IGiftPaidNotifier notifier,
    IClock clock,
    ILogger<CheckoutCardHandler> logger) : IRequestHandler<CheckoutCardCommand, Result<CheckoutCardResultDto>>
{
    public async Task<Result<CheckoutCardResultDto>> Handle(CheckoutCardCommand request, CancellationToken cancellationToken)
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

        PaymentCardResult card;
        try
        {
            card = await gateway.CreateCardPaymentAsync(
                new CreateCardPaymentRequest(
                    GiftId: gift.Id,
                    AmountBrl: gift.Price.Amount,
                    GuestName: request.BuyerName,
                    GuestEmail: request.BuyerEmail,
                    GuestDocument: request.BuyerDocument,
                    Description: $"Presente de casamento Helo & Thales — {gift.Title}",
                    DueDate: now.AddDays(1),
                    RemoteIp: request.IpHash,
                    Card: new CreditCardData(
                        HolderName: request.CardHolderName,
                        Number: request.CardNumber,
                        ExpiryMonth: request.CardExpiryMonth,
                        ExpiryYear: request.CardExpiryYear,
                        Ccv: request.CardCcv),
                    HolderInfo: new CreditCardHolderData(
                        PostalCode: request.PostalCode,
                        AddressNumber: request.AddressNumber,
                        Phone: request.Phone)),
                cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao cobrar cartão para gift {GiftId}", gift.Id);
            return Error.External("Não foi possível processar o cartão. Verifique os dados e tente de novo.");
        }

        // Asaas confirmou na hora — marcar pago sincronamente (webhook segue como backup idempotente).
        if (card.Status == PaymentCardStatus.Confirmed)
        {
            try
            {
                gift.MarkPaid(request.BuyerName, buyerEmail, card.ProviderPaymentId, now, request.Message);
            }
            catch (DomainException ex)
            {
                logger.LogError(ex, "Erro ao marcar gift {GiftId} como pago após cartão Confirmed", gift.Id);
                return Error.Validation(ex.Message);
            }

            await repository.UpdateAsync(gift, cancellationToken).ConfigureAwait(false);

            try
            {
                await notifier.NotifyAsync(
                    gift,
                    new PaymentCustomer(request.BuyerName, request.BuyerEmail),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao enviar e-mails (cartão) para gift {GiftId}", gift.Id);
            }
        }

        return new CheckoutCardResultDto(gift.Id, card.ProviderPaymentId, card.Status.ToString());
    }
}
