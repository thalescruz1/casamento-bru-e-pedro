using Casamento.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Casamento.Api.Infrastructure;

internal static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, int successStatus = StatusCodes.Status200OK) =>
        result.IsSuccess
            ? new ObjectResult(result.Value) { StatusCode = successStatus }
            : ErrorResult(result.Error);

    public static IActionResult ToActionResult(this Result result, int successStatus = StatusCodes.Status204NoContent) =>
        result.IsSuccess
            ? new StatusCodeResult(successStatus)
            : ErrorResult(result.Error);

    private static ObjectResult ErrorResult(Error error)
    {
        var status = error.Code switch
        {
            "validation" => StatusCodes.Status400BadRequest,
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            "rate_limited" => StatusCodes.Status429TooManyRequests,
            "external" => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };

        return new ObjectResult(new { error = error.Code, message = error.Message })
        {
            StatusCode = status
        };
    }
}
