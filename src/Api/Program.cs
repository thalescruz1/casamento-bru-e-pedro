using Casamento.Api.Infrastructure;
using Casamento.Application;
using Casamento.Infrastructure;
using Casamento.Infrastructure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddSerilog((sp, cfg) =>
{
    cfg.ReadFrom.Configuration(builder.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console();
});

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IClientIpHasher, ClientIpHasher>();
builder.Services.AddSingleton<IAdminAuthorization, AdminAuthorization>();
builder.Services.AddSingleton<IHostedService, CosmosBootstrapService>();

var app = builder.Build();
app.Run();
