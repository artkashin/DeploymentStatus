using DeploymentAPI.Models;

namespace DeploymentAPI.Services;

public interface IWorkflowSyncService
{
    /// <summary>
    /// Syncs the latest "Update all customers" workflow run data into the deployment repository.
    /// Creates/updates customer records and deployment history.
    /// </summary>
    /// <returns>A summary of the sync operation</returns>
    Task<WorkflowSyncResult> SyncLatestWorkflowRunAsync();

    /// <summary>
    /// Syncs a specific workflow run by ID into the deployment repository.
    /// </summary>
    /// <param name="runId">GitHub workflow run ID</param>
    /// <returns>A summary of the sync operation</returns>
    Task<WorkflowSyncResult> SyncWorkflowRunByIdAsync(long runId);
}

public class WorkflowSyncResult
{
    public long WorkflowRunId { get; set; }
    public int RunNumber { get; set; }
    public int CustomersProcessed { get; set; }
    public int CustomersCreated { get; set; }
    public int CustomersUpdated { get; set; }
    public int DeploymentsRecorded { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool Success => !Errors.Any();
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}
