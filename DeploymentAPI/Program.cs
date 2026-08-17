using Azure.Data.Tables;
using Azure.Identity;
using DeploymentStatus.Api.Security;
using DeploymentStatus.Api.Storage;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<CallerContextFactory>();
        if (context.Configuration.GetValue("Storage:UseInMemory", false))
        {
            services.AddSingleton<IDeploymentStore, InMemoryDeploymentStore>();
            return;
        }
        services.AddSingleton(_ =>
        {
            var serviceUri = context.Configuration["Storage:ServiceUri"];
            if (!string.IsNullOrWhiteSpace(serviceUri))
                return new TableServiceClient(new Uri(serviceUri), new DefaultAzureCredential());
            var connectionString = context.Configuration["AzureWebJobsStorage"];
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Storage:ServiceUri or AzureWebJobsStorage must be configured.");
            return new TableServiceClient(connectionString);
        });
        services.AddSingleton<IDeploymentStore, TableDeploymentStore>();
    })
    .Build();

host.Run();
