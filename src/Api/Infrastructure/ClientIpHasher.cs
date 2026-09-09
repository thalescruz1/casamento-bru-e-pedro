using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Casamento.Api.Infrastructure;

public interface IClientIpHasher
{
    string Hash(HttpRequest request);
}

internal sealed class ClientIpHasher : IClientIpHasher
{
    public string Hash(HttpRequest request)
    {
        var ip = ExtractIp(request);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ip));
        return Convert.ToHexString(bytes);
    }

    private static string ExtractIp(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Forwarded-For", out var forwarded) && forwarded.Count > 0)
        {
            var first = forwarded[0]?.Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first;
            }
        }

        return request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
