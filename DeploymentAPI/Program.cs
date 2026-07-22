using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DeploymentAPI.Repositories;
using DeploymentAPI.Services;

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

        // Register GitHub authentication provider based on configuration
        var githubAuthType = context.Configuration["GitHub:AuthType"] ?? "PAT";

        if (githubAuthType.Equals("GitHubApp", StringComparison.OrdinalIgnoreCase))
        {
            // Register private key provider based on configuration
            var privateKeySource = context.Configuration["GitHub:PrivateKeySource"] ?? "File";

            if (privateKeySource.Equals("KeyVault", StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton<IGitHubPrivateKeyProvider, KeyVaultPrivateKeyProvider>();
                Console.WriteLine("🔑 Using Azure Key Vault for GitHub App private key");
            }
            else if (privateKeySource.Equals("Configuration", StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton<IGitHubPrivateKeyProvider, ConfigurationPrivateKeyProvider>();
                Console.WriteLine("🔑 Using configuration for GitHub App private key");
            }
            else
            {
                services.AddSingleton<IGitHubPrivateKeyProvider, FileSystemPrivateKeyProvider>();
                Console.WriteLine("🔑 Using file system for GitHub App private key");
            }

            services.AddSingleton<IGitHubAuthProvider, OctokitGitHubAppAuthProvider>();
            Console.WriteLine("✅ Using GitHub App authentication with Octokit");
        }
        else
        {
            services.AddSingleton<IGitHubAuthProvider, GitHubPersonalTokenAuthProvider>();
            Console.WriteLine("✅ Using Personal Access Token authentication");
        }

        // Register GitHub service  
        services.AddSingleton<IGitHubService, OctokitGitHubService>();
        Console.WriteLine("✅ GitHub integration configured with Octokit");

        // Register workflow sync service
        services.AddScoped<IWorkflowSyncService, WorkflowSyncService>();
        Console.WriteLine("✅ Workflow sync service registered");

        // Register database initialization service
        services.AddSingleton<IDatabaseInitializationService, DatabaseInitializationService>();
        Console.WriteLine("✅ Database initialization service registered");
    })
    .Build();

// Run database initialization on startup
var initializationTask = Task.Run(async () =>
{
    await Task.Delay(3000); // Wait for host to fully start

    using var scope = host.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var initService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();

    try
    {
        logger.LogInformation("🚀 Starting automatic database initialization check...");
        var result = await initService.InitializeIfEmptyAsync();

        if (result.Success)
        {
            if (result.WasInitialized)
            {
                logger.LogInformation("✅ Database initialized: {Message}", result.Message);
                logger.LogInformation("   📊 Customers: {Customers}, Applications: {Apps}, Deployments: {Deployments}",
                    result.CustomersCreated, result.ApplicationsCreated, result.DeploymentsProcessed);
            }
            else
            {
                logger.LogInformation("ℹ️  {Message}", result.Message);
            }
        }
        else
        {
            logger.LogWarning("⚠️  Database initialization failed: {Error}", result.Error ?? result.Message);
            logger.LogInformation("💡 You can manually initialize using: POST http://localhost:7071/api/admin/initialize");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error during startup initialization");
    }
});

host.Run();


