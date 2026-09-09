using Casamento.Api.Infrastructure;
using Casamento.Application.Received.Queries.ListReceived;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Casamento.Api.Functions;

public sealed class ReceivedFunctions(
    ISender sender,
    IAdminAuthorization authorization)
{
    [Function("ListReceivedAdmin")]
    public async Task<IActionResult> ListAdmin(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "received/admin")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!authorization.TryAuthorize(request, out _))
        {
            return new UnauthorizedResult();
        }

        var result = await sender.Send(new ListReceivedQuery(), cancellationToken).ConfigureAwait(false);
        return result.ToActionResult();
    }
}
