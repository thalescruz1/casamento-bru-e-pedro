using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Casamento.Api.Functions;

public static class HealthFunction
{
    [Function("Health")]
    public static IActionResult Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest request) =>
        new OkObjectResult(new
        {
            status = "ok",
            version = typeof(HealthFunction).Assembly.GetName().Version?.ToString() ?? "dev",
            now = DateTimeOffset.UtcNow
        });
}
