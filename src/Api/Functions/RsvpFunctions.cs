using Casamento.Api.Infrastructure;
using Casamento.Application.Rsvps.Commands.SubmitRsvp;
using Casamento.Application.Rsvps.Queries.ListRsvps;
using Casamento.Domain.Rsvps;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text;

namespace Casamento.Api.Functions;

public sealed class RsvpFunctions(
    ISender sender,
    IClientIpHasher ipHasher,
    IAdminAuthorization authorization)
{
    [Function("SubmitRsvp")]
    public async Task<IActionResult> Submit(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "rsvp")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        var body = await request.ReadFromJsonAsync<SubmitRsvpBody>(cancellationToken).ConfigureAwait(false);
        if (body is null)
        {
            return new BadRequestObjectResult(new { error = "validation", message = "Corpo inválido." });
        }

        var command = new SubmitRsvpCommand(
            body.Name ?? string.Empty,
            body.Email ?? string.Empty,
            body.Phone ?? string.Empty,
            body.Attend,
            body.Guests,
            body.GuestNames,
            body.Restrictions,
            ipHasher.Hash(request));

        var result = await sender.Send(command, cancellationToken).ConfigureAwait(false);
        return result.ToActionResult(StatusCodes.Status201Created);
    }

    [Function("ListRsvps")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rsvp")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!authorization.TryAuthorize(request, out _))
        {
            return new UnauthorizedResult();
        }

        _ = int.TryParse(request.Query["skip"], out var skip);
        _ = int.TryParse(request.Query["take"], out var take);

        var result = await sender.Send(new ListRsvpsQuery(skip, take), cancellationToken).ConfigureAwait(false);
        return result.ToActionResult();
    }

    [Function("ExportRsvps")]
    public async Task<IActionResult> Export(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rsvp/export")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!authorization.TryAuthorize(request, out _))
        {
            return new UnauthorizedResult();
        }

        var result = await sender.Send(new ListRsvpsQuery(0, 5000), cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        var sb = new StringBuilder();
        sb.AppendLine("id;nome;email;telefone;comparece;acompanhantes;nomes_acompanhantes;observacao;enviado_em");
        foreach (var item in result.Value!.Items)
        {
            sb.Append(item.Id).Append(';')
              .Append(Escape(item.Name)).Append(';')
              .Append(item.Email).Append(';')
              .Append(Escape(item.Phone ?? string.Empty)).Append(';')
              .Append(item.Attend == Attendance.Sim ? "sim" : "nao").Append(';')
              .Append(item.Guests).Append(';')
              .Append(Escape(item.GuestNames ?? string.Empty)).Append(';')
              .Append(Escape(item.Restrictions ?? string.Empty)).Append(';')
              .Append(item.SubmittedAt.ToString("yyyy-MM-dd HH:mm:ss"))
              .Append('\n');
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return new FileContentResult(bytes, "text/csv; charset=utf-8")
        {
            FileDownloadName = $"rsvps-{DateTimeOffset.UtcNow:yyyyMMddHHmm}.csv"
        };
    }

    private static string Escape(string value) =>
        value.Contains(';', StringComparison.Ordinal) || value.Contains('\n', StringComparison.Ordinal)
            ? "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : value;

    private sealed record SubmitRsvpBody(
        string? Name,
        string? Email,
        string? Phone,
        Attendance Attend,
        int Guests,
        string? GuestNames,
        string? Restrictions);
}
