using System.Net;
using DeploymentAPI.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DeploymentAPI.Functions;

public class InitializeDatabaseFunction
{
    private readonly IDatabaseInitializationService _initService;
    private readonly ILogger<InitializeDatabaseFunction> _logger;

    public InitializeDatabaseFunction(
        IDatabaseInitializationService initService,
        ILogger<InitializeDatabaseFunction> logger)
    {
        _initService = initService;
        _logger = logger;
    }

    [Function("InitializeDatabase")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/initialize")] HttpRequestData req)
    {
        _logger.LogInformation("Initialize database endpoint called");

        try
        {
            // Check for force parameter
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var force = query["force"]?.ToLower() == "true";

            InitializationResult result;
            if (force)
            {
                _logger.LogInformation("Force initialization requested");
                result = await _initService.ForceInitializeAsync();
            }
            else
            {
                result = await _initService.InitializeIfEmptyAsync();
            }

            var response = req.CreateResponse(result.Success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new
            {
                success = result.Success,
                wasInitialized = result.WasInitialized,
                message = result.Message,
                data = new
                {
                    customersCreated = result.CustomersCreated,
                    applicationsCreated = result.ApplicationsCreated,
                    deploymentsProcessed = result.DeploymentsProcessed
                },
                error = result.Error
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in InitializeDatabase endpoint");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                success = false,
                message = "Failed to initialize database",
                error = ex.Message
            });

            return errorResponse;
        }
    }

    [Function("GetInitializationStatus")]
    public async Task<HttpResponseData> GetStatus(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "admin/initialize/status")] HttpRequestData req)
    {
        _logger.LogInformation("Get initialization status endpoint called");

        try
        {
            // For now, just check if database has data
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                message = "Use POST /api/admin/initialize to initialize the database",
                endpoints = new
                {
                    initialize = "/api/admin/initialize",
                    forceInitialize = "/api/admin/initialize?force=true",
                    status = "/api/admin/initialize/status"
                }
            });

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetInitializationStatus endpoint");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
            {
                error = ex.Message
            });

            return errorResponse;
        }
    }
}
