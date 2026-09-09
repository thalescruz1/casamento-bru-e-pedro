using System.IO;
using Casamento.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Casamento.Api.Functions;

/// <summary>
/// Endpoint chamado pela Asaas pra autorizar cada saque/transferência.
/// Como o painel Asaas tem 2FA e só os noivos têm acesso, aprovamos tudo após validar o token.
/// </summary>
public sealed class AsaasWithdrawalValidationFunction(
    IPaymentGateway gateway,
    ILogger<AsaasWithdrawalValidationFunction> logger)
{
    [Function("AsaasWithdrawalValidation")]
    public async Task<IActionResult> Validate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhooks/asaas-withdrawal")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var providedToken = request.Headers["asaas-access-token"].ToString();
        if (!gateway.VerifyWithdrawalWebhookToken(providedToken))
        {
            logger.LogWarning("Webhook Asaas (saque) recusado — token inválido");
            return new UnauthorizedResult();
        }

        string rawBody;
        using (var reader = new StreamReader(request.Body))
        {
            rawBody = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        // Auditoria — todo saque autorizado é logado pra rastreabilidade.
        logger.LogInformation("Saque autorizado pela API. Payload: {Payload}", rawBody);

        return new OkObjectResult(new { status = "APPROVED" });
    }
}
