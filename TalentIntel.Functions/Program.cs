using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TalentIntel.Infrastructure.DependencyInjection;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication() // Enables ASP.NET Core integration (middleware, filters, etc.)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddJsonFile(
            $"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
            optional: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
            options.ConnectionString = context.Configuration["ApplicationInsights:ConnectionString"];
        });
        services.ConfigureFunctionsApplicationInsights();

        services.AddInfrastructure(context.Configuration);
    })
    .Build();

await host.RunAsync();