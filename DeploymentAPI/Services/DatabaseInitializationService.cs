using DeploymentAPI.Models;
using DeploymentAPI.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeploymentAPI.Services;

/// <summary>
/// Service responsible for initializing the database with data from GitHub Actions
/// when the database is empty.
/// </summary>
public interface IDatabaseInitializationService
{
    /// <summary>
    /// Checks if database is empty and initializes it from GitHub Actions if needed.
    /// </summary>
    Task<InitializationResult> InitializeIfEmptyAsync();

    /// <summary>
    /// Forces database initialization regardless of current state.
    /// </summary>
    Task<InitializationResult> ForceInitializeAsync();
}

public class InitializationResult
{
    public bool WasInitialized { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CustomersCreated { get; set; }
    public int ApplicationsCreated { get; set; }
    public int DeploymentsProcessed { get; set; }
    public string? Error { get; set; }
}

public class DatabaseInitializationService : IDatabaseInitializationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializationService> _logger;
    private bool _hasInitialized = false;

    public DatabaseInitializationService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<InitializationResult> InitializeIfEmptyAsync()
    {
        if (_hasInitialized)
        {
            _logger.LogInformation("Database initialization already completed in this session");
            return new InitializationResult
            {
                WasInitialized = false,
                Success = true,
                Message = "Database already initialized in this session"
            };
        }

        try
        {
            _logger.LogInformation("Checking if database initialization is needed...");

            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();

            // Check if database already has data
            var customers = await repository.GetAllCustomersAsync();
            var applications = await repository.GetAllApplicationsAsync();

            if (customers.Any() || applications.Any())
            {
                _logger.LogInformation("Database already contains data (Customers: {CustomerCount}, Applications: {AppCount}). Skipping initialization.",
                    customers.Count(), applications.Count());

                _hasInitialized = true;
                return new InitializationResult
                {
                    WasInitialized = false,
                    Success = true,
                    Message = $"Database already populated with {customers.Count()} customers and {applications.Count()} applications"
                };
            }

            _logger.LogInformation("Database is empty. Starting initialization from GitHub Actions...");
            return await PerformInitializationAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database initialization check");
            return new InitializationResult
            {
                WasInitialized = false,
                Success = false,
                Message = "Initialization check failed",
                Error = ex.Message
            };
        }
    }

    public async Task<InitializationResult> ForceInitializeAsync()
    {
        _logger.LogWarning("Forcing database initialization regardless of current state");
        _hasInitialized = false;
        return await PerformInitializationAsync();
    }

    private async Task<InitializationResult> PerformInitializationAsync()
    {
        try
        {
            _logger.LogInformation("Fetching latest workflow data from GitHub Actions...");

            using var scope = _serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IWorkflowSyncService>();
            var repository = scope.ServiceProvider.GetRequiredService<IDeploymentRepository>();

            // Sync latest workflow run to populate database
            var syncResult = await syncService.SyncLatestWorkflowRunAsync();

            if (syncResult.Success)
            {
                _logger.LogInformation("Successfully initialized database from GitHub Actions");
                _logger.LogInformation("Processed: {CustomerCount} customers, {DeploymentCount} deployments",
                    syncResult.CustomersProcessed, syncResult.DeploymentsRecorded);

                _hasInitialized = true;

                // Ensure CI/CD version is set
                await EnsureCiCdVersionAsync(repository);

                return new InitializationResult
                {
                    WasInitialized = true,
                    Success = true,
                    Message = $"Database initialized successfully from GitHub Actions workflow run #{syncResult.WorkflowRunId}",
                    CustomersCreated = syncResult.CustomersCreated,
                    ApplicationsCreated = 0, // Applications are counted separately if needed
                    DeploymentsProcessed = syncResult.DeploymentsRecorded
                };
            }
            else
            {
                var errorMessage = syncResult.Errors.Any() ? string.Join("; ", syncResult.Errors) : "Unknown error";
                _logger.LogError("Failed to initialize database from GitHub Actions: {Error}", errorMessage);
                return new InitializationResult
                {
                    WasInitialized = false,
                    Success = false,
                    Message = "Failed to sync workflow data from GitHub",
                    Error = errorMessage
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database initialization");
            return new InitializationResult
            {
                WasInitialized = false,
                Success = false,
                Message = "Database initialization failed",
                Error = ex.Message
            };
        }
    }

    private async Task EnsureCiCdVersionAsync(IDeploymentRepository repository)
    {
        try
        {
            var currentVersion = await repository.GetCurrentCiCdVersionAsync();
            if (currentVersion == null)
            {
                _logger.LogInformation("No CI/CD version found. Setting default version...");

                var ciCdVersion = new CiCdVersion
                {
                    Version = "1.0.0",
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "System",
                    Notes = "Initial version set during database initialization"
                };

                await repository.UpdateCiCdVersionAsync(ciCdVersion);
                _logger.LogInformation("Default CI/CD version set to 1.0.0");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to ensure CI/CD version during initialization. This is non-critical.");
        }
    }
}
