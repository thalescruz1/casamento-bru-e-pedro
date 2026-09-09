using Casamento.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure.Pix;

internal sealed class OwnPixProvider(IOptions<OwnPixOptions> options) : IOwnPixProvider
{
    public OwnPixInfo Get()
    {
        var o = options.Value;
        return new OwnPixInfo(o.Key, o.KeyType, o.Beneficiary, o.Bank, o.City);
    }
}
