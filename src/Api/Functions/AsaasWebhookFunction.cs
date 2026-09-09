using System.Text.Json;
using Casamento.Application.Abstractions;
using Casamento.Application.Gifts.Commands.ProcessAsaasWebhook;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Casamento.Api.Functions;

public sealed class AsaasWebhookFunction(
    ISender sender,
    IPaymentGateway gateway,
    ILogger<AsaasWebhookFunction> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Function("AsaasWebhook")]
    public async Task<IActionResult> Handle(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhooks/asaas")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var providedToken = request.Headers["asaas-access-token"].ToString();
        if (!gateway.VerifyWebhookSignature(providedToken))
        {
            logger.LogWarning("Webhook Asaas recusado — token inválido");
            return new UnauthorizedResult();
        }

        string rawBody;
        using (var reader = new StreamReader(request.Body))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return new BadRequestObjectResult(new { error = "validation", message = "Corpo vazio." });
        }

        AsaasWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<AsaasWebhookPayload>(rawBody, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Webhook Asaas com JSON inválido");
            return new BadRequestObjectResult(new { error = "validation", message = "JSON inválido." });
        }

        if (payload?.Payment is null || string.IsNullOrWhiteSpace(payload.Id))
        {
            return new BadRequestObjectResult(new { error = "validation", message = "Payload incompleto." });
        }

        var command = new ProcessAsaasWebhookCommand(
            EventId: payload.Id,
            EventType: payload.Event ?? "UNKNOWN",
            PaymentId: payload.Payment.Id ?? string.Empty,
            CustomerId: payload.Payment.Customer,
            ExternalReference: payload.Payment.ExternalReference,
            RawPayload: rawBody);

        var result = await sender.Send(command, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return new ObjectResult(new { error = result.Error.Code, message = result.Error.Message })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }

        return new OkResult();
    }

    private sealed record AsaasWebhookPayload(string? Id, string? Event, AsaasWebhookPayment? Payment);
    private sealed record AsaasWebhookPayment(string? Id, string? Customer, string? ExternalReference, string? Status);
}
