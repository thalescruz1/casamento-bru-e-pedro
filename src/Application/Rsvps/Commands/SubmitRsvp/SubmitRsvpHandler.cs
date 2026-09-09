using System.Diagnostics;
using Casamento.Application.Abstractions;
using Casamento.Application.Common;
using Casamento.Application.Rsvps.Dtos;
using Casamento.Domain.Common;
using Casamento.Domain.Rsvps;
using Casamento.Domain.Rsvps.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Casamento.Application.Rsvps.Commands.SubmitRsvp;

public sealed class SubmitRsvpHandler(
    IRsvpRepository repository,
    IClock clock,
    ILogger<SubmitRsvpHandler> logger) : IRequestHandler<SubmitRsvpCommand, Result<RsvpConfirmationDto>>
{
    private const int RateLimitWindowMinutes = 5;
    private const int RateLimitMaxSubmissions = 3;

    public async Task<Result<RsvpConfirmationDto>> Handle(
        SubmitRsvpCommand request,
        CancellationToken cancellationToken)
    {
        using var activity = Activities.Source.StartActivity("rsvp-submit");
        activity?.SetTag("attend", request.Attend);

        var now = clock.UtcNow;

        var windowStart = now.AddMinutes(-RateLimitWindowMinutes);
        var recent = await repository.CountSubmissionsFromAsync(request.IpHash, windowStart, cancellationToken)
            .ConfigureAwait(false);
        if (recent >= RateLimitMaxSubmissions)
        {
            logger.LogWarning("RSVP rate limit atingido para origem {IpHashPrefix}", request.IpHash[..Math.Min(8, request.IpHash.Length)]);
            return Error.RateLimited("Muitas tentativas. Tente novamente em alguns minutos.");
        }

        Email email;
        Rsvp rsvp;
        try
        {
            email = Email.Create(request.Email);
            rsvp = Rsvp.Submit(
                request.Name,
                email,
                request.Phone,
                request.Attend,
                request.Guests,
                request.GuestNames,
                request.Restrictions,
                request.IpHash,
                now);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Message);
        }

        await repository.AddAsync(rsvp, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("RSVP {RsvpId} registrado ({Attend})", rsvp.Id, rsvp.Attend);

        return new RsvpConfirmationDto(rsvp.Id, rsvp.Name, rsvp.Attend, rsvp.SubmittedAt);
    }
}

internal static class Activities
{
    public static readonly ActivitySource Source = new("casamento");
}
