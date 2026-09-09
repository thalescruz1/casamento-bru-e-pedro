using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Application.Gifts.Notifications;
using Casamento.Domain.Common;
using Casamento.Domain.Gifts;
using Casamento.Domain.Payments;
using Casamento.Domain.Rsvps.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Casamento.Application.Gifts.Commands.ProcessAsaasWebhook;

public sealed record ProcessAsaasWebhookCommand(
    string EventId,
    string EventType,
    string PaymentId,
    string? CustomerId,
    string? ExternalReference,
    string RawPayload) : IRequest<Result>;

public sealed class ProcessAsaasWebhookHandler(
    IGiftRepository giftRepository,
    IPaymentEventRepository eventRepository,
    IPaymentGateway gateway,
    IGiftPaidNotifier notifier,
    IClock clock,
    ILogger<ProcessAsaasWebhookHandler> logger)
    : IRequestHandler<ProcessAsaasWebhookCommand, Result>
{
    private static readonly HashSet<string> PaidEvents =
        new(StringComparer.OrdinalIgnoreCase) { "PAYMENT_CONFIRMED", "PAYMENT_RECEIVED" };

    public async Task<Result> Handle(ProcessAsaasWebhookCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EventId))
        {
            return Error.Validation("eventId ausente.");
        }

        if (string.IsNullOrWhiteSpace(request.PaymentId))
        {
            return Error.Validation("paymentId ausente.");
        }

        var now = clock.UtcNow;

        Guid? parsedGiftId = null;
        if (Guid.TryParse(request.ExternalReference, out var refId))
        {
            parsedGiftId = refId;
        }

        var evt = new PaymentEvent(request.EventId, request.EventType, request.PaymentId, parsedGiftId, now, request.RawPayload);

        var firstTime = await eventRepository.TryRecordAsync(evt, cancellationToken).ConfigureAwait(false);
        if (!firstTime)
        {
            logger.LogInformation("Webhook Asaas {EventId} já processado, ignorando", request.EventId);
            return Result.Success();
        }

        if (!PaidEvents.Contains(request.EventType))
        {
            logger.LogDebug("Webhook Asaas {EventType} ignorado (não é confirmação de pagamento)", request.EventType);
            return Result.Success();
        }

        var gift = parsedGiftId is { } id
            ? await giftRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            : await giftRepository.FindByAsaasPaymentIdAsync(request.PaymentId, cancellationToken).ConfigureAwait(false);

        if (gift is null)
        {
            logger.LogWarning("Webhook {EventType} para payment {PaymentId} sem presente correspondente (externalReference={Ref})",
                request.EventType, request.PaymentId, request.ExternalReference);
            return Result.Success();
        }

        if (string.IsNullOrWhiteSpace(request.CustomerId))
        {
            logger.LogWarning("Webhook {EventType} sem customerId; não consigo identificar comprador do gift {GiftId}",
                request.EventType, gift.Id);
            return Result.Success();
        }

        PaymentCustomer? customer;
        try
        {
            customer = await gateway.GetCustomerAsync(request.CustomerId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao consultar customer {CustomerId} na Asaas", request.CustomerId);
            return Error.External("Falha ao consultar dados do cliente.");
        }

        if (customer is null || string.IsNullOrWhiteSpace(customer.Email))
        {
            logger.LogWarning("Customer {CustomerId} não encontrado ou sem e-mail", request.CustomerId);
            return Result.Success();
        }

        var wasAvailable = gift.Status == GiftStatus.Available;

        try
        {
            gift.MarkPaid(customer.Name, Email.Create(customer.Email), request.PaymentId, now);
        }
        catch (DomainException ex)
        {
            logger.LogError(ex, "Erro ao marcar gift {GiftId} como pago", gift.Id);
            return Error.Validation(ex.Message);
        }

        await giftRepository.UpdateAsync(gift, cancellationToken).ConfigureAwait(false);

        if (wasAvailable)
        {
            logger.LogInformation("Gift {GiftId} pago por {BuyerEmail} via {EventType}", gift.Id, customer.Email, request.EventType);
            try
            {
                await notifier.NotifyAsync(gift, customer, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao enviar e-mails de confirmação para gift {GiftId}", gift.Id);
            }
        }
        else
        {
            logger.LogWarning(
                "Pagamento duplicado detectado para gift {GiftId} — payment {PaymentId} chegou após {WinningPaymentId}. Estorno manual necessário.",
                gift.Id, request.PaymentId, gift.Purchase?.AsaasPaymentId);
        }

        return Result.Success();
    }
}
