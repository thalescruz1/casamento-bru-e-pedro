using System.Reflection;
using Casamento.Application.Common;
using Casamento.Application.Contributions.Notifications;
using Casamento.Application.Gifts.Notifications;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Casamento.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddSingleton<IClock, SystemClock>();

        services.AddOptions<WeddingEmailOptions>()
            .Bind(configuration.GetSection(WeddingEmailOptions.SectionName));
        services.AddScoped<IGiftPaidNotifier, GiftPaidNotifier>();
        services.AddScoped<IContributionPaidNotifier, ContributionPaidNotifier>();

        return services;
    }
}
