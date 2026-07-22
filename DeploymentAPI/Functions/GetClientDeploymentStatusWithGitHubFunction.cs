using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using DeploymentAPI.Services;
using DeploymentAPI.Repositories;
using DeploymentAPI.Models;

namespace DeploymentAPI.Functions;

public class GetClientDeploymentStatusWithGitHubFunction
{
    private readonly ILogger<GetClientDeploymentStatusWithGitHubFunction> _logger;
    private readonly IGitHubService _gitHubService;
    private readonly IDeploymentRepository _repository;

    public GetClientDeploymentStatusWithGitHubFunction(
        ILogger<GetClientDeploymentStatusWithGitHubFunction> logger,
        IGitHubService gitHubService,
        IDeploymentRepository repository)
    {
        _logger = logger;
        _gitHubService = gitHubService;
        _repository = repository;
    }

    [Function("GetClientDeploymentStatusWithGitHub")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "clients/{clientId}/status-with-github")] HttpRequestData req,
        string clientId)
    {
        _logger.LogInformation("GetClientDeploymentStatusWithGitHub function triggered for client: {ClientId}", clientId);

        try
        {
            // Get local deployment status
            var clientStatus = await _repository.GetClientStatusAsync(clientId);

            if (clientStatus == null)
            {
                _logger.LogWarning("Client {ClientId} not found", clientId);
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { error = $"Client '{clientId}' not found" });
                return notFoundResponse;
            }

            // Get GitHub workflow runs for this client
            var workflowRuns = await _gitHubService.GetWorkflowRunsAsync(clientId);

            // Combine the data
            var enrichedStatus = new
            {
                client = clientStatus,
                gitHubWorkflows = workflowRuns.Select(run => new
                {
                    run.Id,
                    run.Name,
                    run.Status,
                    run.Conclusion,
                    run.HeadBranch,
                    run.CreatedAt,
                    run.UpdatedAt,
                    run.HtmlUrl,
                    Actor = run.Actor?.Login
                }).ToList()
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(enrichedStatus);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client status with GitHub data for {ClientId}", clientId);

            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new
            {
                error = "Failed to retrieve client status with GitHub data",
                message = ex.Message
            });

            return response;
        }
    }
}
