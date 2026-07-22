# GitHub Integration Architecture

## ✅ Correct Pattern: Internal Service

GitHub authentication and API access should be **internal services** that support your deployment tracking features, not exposed as public API endpoints.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Public API                            │
├─────────────────────────────────────────────────────────────┤
│  /api/deployments              - Register deployment         │
│  /api/clients/{id}/status      - Get client status           │
│  /api/clients/status           - Get all clients status      │
│  /api/cicd/version             - Get/Update CI/CD version    │
│                                                               │
│  /api/clients/{id}/status-with-github  ✓ Enriched with      │
│                                          GitHub data          │
└─────────────────────────────────────────────────────────────┘
						   ↓
┌─────────────────────────────────────────────────────────────┐
│                    Internal Services                         │
├─────────────────────────────────────────────────────────────┤
│  IGitHubService      - Fetch workflow runs, repository info  │
│  IGitHubAuthProvider - Handle GitHub App authentication      │
│  IDeploymentRepository - Store deployment data               │
└─────────────────────────────────────────────────────────────┘
						   ↓
┌─────────────────────────────────────────────────────────────┐
│                   External Services                          │
├─────────────────────────────────────────────────────────────┤
│  GitHub API          - Workflow runs, commits, releases      │
│  Azure Table Storage - Persistent deployment storage         │
└─────────────────────────────────────────────────────────────┘
```

## ✅ Good: Using GitHub Internally

### Example 1: Enrich Deployment Status with GitHub Data
```csharp
[Function("GetClientDeploymentStatusWithGitHub")]
public async Task<HttpResponseData> Run(
	[HttpTrigger(AuthorizationLevel.Function, "get", 
	 Route = "clients/{clientId}/status-with-github")] HttpRequestData req,
	string clientId)
{
	// Get local deployment data
	var clientStatus = await _repository.GetClientStatusAsync(clientId);

	// Enrich with GitHub workflow data
	var workflowRuns = await _gitHubService.GetWorkflowRunsAsync(clientId);

	return new {
		deployment = clientStatus,
		recentBuilds = workflowRuns
	};
}
```

### Example 2: Auto-populate Deployment from GitHub Release
```csharp
[Function("RegisterDeploymentFromGitHub")]
public async Task<HttpResponseData> Run(
	[HttpTrigger(AuthorizationLevel.Function, "post", 
	 Route = "deployments/from-github")] HttpRequestData req)
{
	var request = await req.ReadFromJsonAsync<GitHubReleaseRequest>();

	// Use GitHub service to fetch release info
	var release = await _gitHubService.GetReleaseByTagAsync(request.Tag);

	// Create deployment record with GitHub data
	var deployment = new DeploymentRecord {
		ClientId = request.ClientId,
		Version = release.TagName,
		DeployedAt = DateTime.UtcNow,
		DeployedBy = release.Author.Login,
		ReleaseUrl = release.HtmlUrl,
		Notes = release.Body
	};

	await _repository.AddDeploymentAsync(deployment);
	return deployment;
}
```

### Example 3: Validate Deployment Against CI/CD Status
```csharp
[Function("RegisterDeployment")]
public async Task<HttpResponseData> Run(
	[HttpTrigger(AuthorizationLevel.Function, "post", 
	 Route = "deployments")] HttpRequestData req)
{
	var request = await req.ReadFromJsonAsync<RegisterDeploymentRequest>();

	// Optional: Verify the version was built successfully
	if (request.ValidateWithGitHub)
	{
		var runs = await _gitHubService.GetWorkflowRunsAsync();
		var buildRun = runs.FirstOrDefault(r => 
			r.HeadBranch.Contains(request.Version) && 
			r.Conclusion == "success");

		if (buildRun == null)
		{
			return BadRequest("No successful build found for this version");
		}

		request.BuildUrl = buildRun.HtmlUrl;
	}

	await _repository.AddDeploymentAsync(request);
	return Ok();
}
```

## ❌ Removed: Direct GitHub API Proxies

These endpoints were removed because they expose GitHub data without adding deployment-tracking value:

- ~~`/api/github/actions`~~ - Direct proxy to GitHub Actions API
- ~~`/api/github/workflows`~~ - Direct proxy to GitHub Workflows API  
- ~~`/api/github/repository`~~ - Direct proxy to GitHub Repository API

**Why removed:**
1. **Security** - Exposes your GitHub App credentials indirectly
2. **Unnecessary** - Clients can call GitHub API directly if needed
3. **No value-add** - Doesn't combine with your deployment data
4. **Maintenance** - You're maintaining a GitHub API wrapper

## GitHub Service Usage

### IGitHubService Methods

```csharp
public interface IGitHubService
{
	// Get workflow runs (optionally filtered by client/branch)
	Task<IEnumerable<GitHubWorkflowRun>> GetWorkflowRunsAsync(string? clientName = null);

	// Get a specific workflow run by ID
	Task<GitHubWorkflowRun?> GetWorkflowRunByIdAsync(long runId);

	// Get all workflows
	Task<IEnumerable<GitHubWorkflow>> GetWorkflowsAsync();

	// Get repository information
	Task<GitHubRepository> GetRepositoryInfoAsync();
}
```

### When to Use GitHubService

✅ **Do use for:**
- Enriching deployment records with build information
- Validating that a version was built successfully
- Auto-populating deployment data from releases
- Correlating deployments with commits/PRs
- Showing recent CI/CD activity alongside deployments

❌ **Don't use for:**
- Generic GitHub API queries unrelated to deployments
- Proxying requests from frontend to GitHub
- Features that don't involve deployment tracking

## Configuration

GitHub App authentication is configured in `local.settings.json`:

```json
{
  "Values": {
	"GitHub:AuthType": "GitHubApp",
	"GitHub:PrivateKeySource": "File",
	"GitHub:PrivateKeyPath": "path/to/private-key.pem",
	"GitHub:AppId": "your-app-id",
	"GitHub:InstallationId": "your-installation-id",
	"GitHub:Owner": "organization-or-user",
	"GitHub:Repository": "repository-name"
  }
}
```

## Current Endpoints

### Deployment Management
- `POST /api/deployments` - Register a new deployment
- `GET /api/clients/{clientId}/status` - Get deployment status
- `GET /api/clients/status` - Get all clients status
- `GET /api/clients/{clientId}/history` - Get deployment history

### CI/CD Version
- `GET /api/cicd/version` - Get current CI/CD version
- `POST|PUT /api/cicd/version` - Update CI/CD version

### ✓ GitHub-Enhanced Endpoints
- `GET /api/clients/{clientId}/status-with-github` - Deployment status enriched with GitHub workflow data

## Best Practices

1. **Keep GitHub internal** - Don't expose raw GitHub API data
2. **Add value** - Combine GitHub data with your deployment data
3. **Validate** - Use GitHub to verify deployments are valid
4. **Enrich** - Use GitHub to add context (build URLs, commits, etc.)
5. **Cache** - Cache GitHub data to avoid rate limits
6. **Handle errors** - Don't fail deployments if GitHub is unavailable

## Testing

Test the GitHub integration with a client-specific endpoint:

```powershell
# Test enriched client status (includes GitHub data)
Invoke-RestMethod "http://localhost:7071/api/clients/test-client/status-with-github"
```

This approach keeps your API focused on deployment tracking while leveraging GitHub as a supporting data source.
