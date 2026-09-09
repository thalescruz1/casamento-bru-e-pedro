using Azure.Communication.Email;
using Azure.Storage.Blobs;
using Casamento.Application.Abstractions;
using Casamento.Infrastructure.Asaas;
using Casamento.Infrastructure.Auth;
using Casamento.Infrastructure.Cosmos;
using Casamento.Infrastructure.Messaging;
using Casamento.Infrastructure.Pix;
using Casamento.Infrastructure.Storage;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Casamento.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCosmos(configuration);
        services.AddBlobStorage(configuration);
        services.AddAsaas(configuration);
        services.AddAuth(configuration);
        services.AddEmail(configuration);
        services.AddOwnPix(configuration);
        return services;
    }

    private static void AddOwnPix(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OwnPixOptions>()
            .Bind(configuration.GetSection(OwnPixOptions.SectionName));

        services.AddSingleton<IOwnPixProvider, OwnPixProvider>();
        services.AddSingleton<IPixQrCodeBuilder, PixQrCodeBuilder>();
    }

    private static void AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        var acsSection = configuration.GetSection(AcsEmailOptions.SectionName);
        var acsConfigured = !string.IsNullOrWhiteSpace(acsSection["ConnectionString"])
            && !string.IsNullOrWhiteSpace(acsSection["SenderAddress"]);

        if (acsConfigured)
        {
            services.AddOptions<AcsEmailOptions>()
                .Bind(acsSection);

            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<AcsEmailOptions>>().Value;
                return new EmailClient(options.ConnectionString);
            });

            services.AddScoped<IEmailSender, AcsEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, NoOpEmailSender>();
        }
    }

    private static void AddCosmos(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CosmosOptions>()
            .Bind(configuration.GetSection(CosmosOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<CosmosOptions>, CosmosOptionsValidator>();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;
            var clientOptions = new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                    IgnoreNullValues = true
                },
                ConnectionMode = ConnectionMode.Gateway
            };
            return new CosmosClient(options.ConnectionString, clientOptions);
        });

        services.AddSingleton<CosmosBootstrapper>();
        services.AddScoped<IRsvpRepository, CosmosRsvpRepository>();
        services.AddScoped<IGiftRepository, CosmosGiftRepository>();
        services.AddScoped<IPaymentEventRepository, CosmosPaymentEventRepository>();
        services.AddScoped<IAdminUserRepository, CosmosAdminUserRepository>();
        services.AddScoped<IContributionRepository, CosmosContributionRepository>();
    }

    private static void AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AuthOptions>, AuthOptionsValidator>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IAuthTokenService, HmacAuthTokenService>();
        services.AddScoped<AdminSeedService>();
    }

    private static void AddBlobStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BlobOptions>()
            .Bind(configuration.GetSection(BlobOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<BlobOptions>, BlobOptionsValidator>();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BlobOptions>>().Value;
            return new BlobServiceClient(options.ConnectionString);
        });

        services.AddScoped<IGiftImageStorage, BlobGiftImageStorage>();
    }

    private static void AddAsaas(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AsaasOptions>()
            .Bind(configuration.GetSection(AsaasOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AsaasOptions>, AsaasOptionsValidator>();

        services.AddHttpClient<IPaymentGateway, AsaasPaymentGateway>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<AsaasOptions>>().Value;
            client.BaseAddress = options.BaseUrl;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("access_token", options.ApiKey);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("casamento-helo-thales/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AsaasOptions>>().Value;
            var handler = new System.Net.Http.HttpClientHandler();

            if (!string.IsNullOrWhiteSpace(options.ProxyUrl))
            {
                var proxy = new System.Net.WebProxy(options.ProxyUrl, BypassOnLocal: false);

                if (!string.IsNullOrWhiteSpace(options.ProxyUsername))
                {
                    var creds = new System.Net.NetworkCredential(options.ProxyUsername, options.ProxyPassword);
                    proxy.Credentials = creds;
                    // Pra HTTPS (CONNECT tunnel), DefaultProxyCredentials é o que .NET usa.
                    handler.DefaultProxyCredentials = creds;
                    handler.PreAuthenticate = true;
                }

                handler.Proxy = proxy;
                handler.UseProxy = true;
            }

            return handler;
        })
        .AddStandardResilienceHandler();
    }
}
