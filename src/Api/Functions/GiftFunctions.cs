using Casamento.Api.Infrastructure;
using Casamento.Application.Gifts.Commands.CheckoutCard;
using Casamento.Application.Gifts.Commands.CheckoutPix;
using Casamento.Application.Gifts.Commands.ConfirmManualPix;
using Casamento.Application.Gifts.Commands.CreateGift;
using Casamento.Application.Gifts.Commands.DeleteGift;
using Casamento.Application.Gifts.Commands.UpdateGift;
using Casamento.Application.Gifts.Commands.UploadGiftImage;
using Casamento.Application.Gifts.Queries.ListAllGifts;
using Casamento.Application.Gifts.Queries.ListAvailableGifts;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Casamento.Api.Functions;

public sealed class GiftFunctions(
    ISender sender,
    IClientIpHasher ipHasher,
    IAdminAuthorization authorization)
{
    [Function("ListAvailableGifts")]
    public async Task<IActionResult> ListAvailable(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "gifts")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListAvailableGiftsQuery(), cancellationToken).ConfigureAwait(false);
        return result.ToActionResult();
    }

    [Function("ListAllGifts")]
    public async Task<IActionResult> ListAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "gifts/admin")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!authorization.TryAuthorize(request, out _))
        {
            return new UnauthorizedResult();
        }

        var result = await sender.Send(new ListAllGiftsQuery(), cancellationToken).ConfigureAwait(false);
        return result.ToActionResult();
    }

    [Function("CreateGift")]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "gifts")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!authorization.TryAuthorize(request, out _))
        {
            return new UnauthorizedResult();
        }

        var body = await request.ReadFromJsonAsync<CreateGiftBody>(cancellationToken).ConfigureAwait(false);
        if (body is null)
        {
            return new BadRequestObjectResult(new { error = "validation", message = "Corpo inválido." });
        }

        var result = await sender.Send(
            new CreateGiftCommand(body.Title ?? string.Empty, body.Description ?? string.Empty, body.Price, body.ImageBlobName),
            cancellationToken).ConfigureAwait(false);
        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [Function("UpdateGift")]
    public async Task<IActionResult> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "gifts/{id:guid}")] HttpRequest request,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!authorization.TryAuthorize(request, out _))
        {
            return new UnauthorizedResult();
        }

        var body = await request.ReadFromJsonAsync<CreateGiftBody>(cancellationToken).ConfigureAwait(false);
        if (body is null)
        {
            return new BadRequestObjectResult(new { error = "validation", message = "Corpo inválido." });
        }

        var result = await sender.Send(
            new UpdateGiftCommand(id, body.Title ?? string.Empty, body.Description ?? string.Empty, body.Price, body.ImageBlobName),
            cancellationToken).ConfigureAwait(false);
        return result.ToActionResult();
    }

    [Function("DeleteGift")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "gifts/{id:guid}")] HttpRequest request,
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!authorization.TryAuthorize(request, out _))
        {
            return new UnauthorizedResult();
        }

        var result = await sender.Send(new DeleteGiftCommand(id), cancellationToken).ConfigureAwait(false);
        return result.ToActionResult();
    }

    [Function("CheckoutPix")]
    public async Task<IActionResult> CheckoutPix(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "gifts/{id:guid}/checkout/pix")] HttpRequest request,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CheckoutPixCommand(id), cancellationToken).ConfigureAwait(false);
        return result.ToActionResult();
    }

    [Function("ConfirmManualPix")]
    public async Task<IActionResult> ConfirmManualPix(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "gifts/{id:guid}/confirm-pix")] HttpRequest request,
        Guid id,
        CancellationToken cancellationToken)
    {
        var body = await request.ReadFromJsonAsync<ConfirmPixBody>(cancellationToken).ConfigureAwait(false);
        if (body is null)
        {
            return new BadRequestObjectResult(new { error = "validation", message = "Corpo inválido." });
        }

        var command = new ConfirmManualPixCommand(
            id,
            body.Name ?? string.Empty,
            body.Email ?? string.Empty,
            body.Message,
            ipHasher.Hash(request));

        var result = await sender.Send(command, cancellationToken).ConfigureAwait(false);
        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [Function("CheckoutCard")]
    public async Task<IActionResult> CheckoutCard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "gifts/{id:guid}/checkout/card")] HttpRequest request,
        Guid id,
        CancellationToken cancellationToken)
    {
        var body = await request.ReadFromJsonAsync<CheckoutCardBody>(cancellationToken).ConfigureAwait(false);
        if (body is null || body.Card is null)
        {
            return new BadRequestObjectResult(new { error = "validation", message = "Corpo inválido." });
        }

        var command = new CheckoutCardCommand(
            id,
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

    [Function("UploadGiftImage")]
    public async Task<IActionResult> UploadImage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "gifts/upload-image")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!authorization.TryAuthorize(request, out _))
        {
            return new UnauthorizedResult();
        }

        if (!request.HasFormContentType)
        {
            return new BadRequestObjectResult(new { error = "validation", message = "Envie como multipart/form-data." });
        }

        var form = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
        var file = form.Files.Count == 0 ? null : form.Files[0];
        if (file is null || file.Length == 0)
        {
            return new BadRequestObjectResult(new { error = "validation", message = "Arquivo ausente." });
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return new BadRequestObjectResult(new { error = "validation", message = "Arquivo maior que 5MB." });
        }

        await using var stream = file.OpenReadStream();
        var result = await sender.Send(
            new UploadGiftImageCommand(file.FileName, file.ContentType, stream),
            cancellationToken).ConfigureAwait(false);
        return result.ToActionResult(StatusCodes.Status201Created);
    }

    private sealed record CreateGiftBody(string? Title, string? Description, decimal Price, string? ImageBlobName);

    private sealed record ConfirmPixBody(string? Name, string? Email, string? Message);

    private sealed record CheckoutCardBody(
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
