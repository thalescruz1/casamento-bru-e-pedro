using Casamento.Api.Infrastructure;
using Casamento.Application.Contributions.Commands.CheckoutContributionCard;
using Casamento.Application.Contributions.Commands.CheckoutContributionPix;
using Casamento.Application.Contributions.Commands.ConfirmManualContributionPix;
using Casamento.Application.Contributions.Queries.ListContributions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Casamento.Api.Functions;

public sealed class ContributionFunctions(
    ISender sender,
    IClientIpHasher ipHasher,
    IAdminAuthorization authorization)
{
    [Function("CheckoutContributionPix")]
    public async Task<IActionResult> CheckoutPix(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "contributions/checkout/pix")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!decimal.TryParse(request.Query["amount"], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var amount))
        {
            return new BadRequestObjectResult(new { error = "validation", message = "Parâmetro 'amount' inválido." });
        }

        var result = await sender.Send(new CheckoutContributionPixCommand(amount), cancellationToken).ConfigureAwait(false);
        return result.ToActionResult();
    }

    [Function("ConfirmManualContributionPix")]
    public async Task<IActionResult> ConfirmManualPix(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contributions/confirm-pix")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var body = await request.ReadFromJsonAsync<ConfirmPixBody>(cancellationToken).ConfigureAwait(false);
        if (body is null)
        {
            return new BadRequestObjectResult(new { error = "validation", message = "Corpo inválido." });
        }

        var command = new ConfirmManualContributionPixCommand(
            body.Amount,
            body.Name ?? string.Empty,
            body.Email ?? string.Empty,
            body.Message,
            ipHasher.Hash(request));

        var result = await sender.Send(command, cancellationToken).ConfigureAwait(false);
        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [Function("CheckoutContributionCard")]
    public async Task<IActionResult> CheckoutCard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contributions/checkout/card")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var body = await request.ReadFromJsonAsync<CheckoutCardBody>(cancellationToken).ConfigureAwait(false);
        if (body is null || body.Card is null)
        {
            return new BadRequestObjectResult(new { error = "validation", message = "Corpo inválido." });
        }

        var command = new CheckoutContributionCardCommand(
            body.Amount,
            body.Name ?? string.Empty,
            body.Email ?? string.Empty,
            body.Document ?? string.Empty,
            body.Phone ?? string.Empty,
            body.PostalCode ?? string.Empty,
            body.AddressNumber ?? string.Empty,
            body.Card.HolderName ?? string.Empty,
            body.Card.Number ?? string.Empty,
            body.Card.ExpiryMonth ?? string.Empty,
            body.Card.ExpiryYear ?? string.Empty,
            body.Card.Ccv ?? string.Empty,
            body.Message,
            ipHasher.Hash(request));

        var result = await sender.Send(command, cancellationToken).ConfigureAwait(false);
        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [Function("ListContributionsAdmin")]
    public async Task<IActionResult> ListAdmin(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "contributions/admin")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!authorization.TryAuthorize(request, out _))
        {
            return new UnauthorizedResult();
        }

        var result = await sender.Send(new ListContributionsQuery(), cancellationToken).ConfigureAwait(false);
        return result.ToActionResult();
    }

    private sealed record ConfirmPixBody(decimal Amount, string? Name, string? Email, string? Message);

    private sealed record CheckoutCardBody(
        decimal Amount,
        string? Name,
        string? Email,
        string? Document,
        string? Phone,
        string? PostalCode,
        string? AddressNumber,
        CardPayload? Card,
        string? Message);

    private sealed record CardPayload(
        string? HolderName,
        string? Number,
        string? ExpiryMonth,
        string? ExpiryYear,
        string? Ccv);
}
