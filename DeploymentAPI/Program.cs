using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DeploymentAPI.Repositories;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        
        // Register deployment repository based on configuration
        var storageType = context.Configuration["StorageType"] ?? "InMemory";
        var connectionString = context.Configuration["AzureWebJobsStorage"];

        if (storageType.Equals("TableStorage", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(connectionString))
        {
            services.AddSingleton<IDeploymentRepository>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<TableStorageDeploymentRepository>>();
                return new TableStorageDeploymentRepository(connectionString, logger);
            });
            
            Console.WriteLine("? Using Azure Table Storage for data persistence");
        }
        else
        {
            services.AddSingleton<IDeploymentRepository, InMemoryDeploymentRepository>();
            Console.WriteLine("??  Using In-Memory storage (data will be lost on restart)");
        }
    })
    .Build();

host.Run();


