# GitHub Integration for Deployment API

This integration allows the Deployment API to fetch GitHub Actions workflow runs and details from the private repository `AdaptiveBS/CIApp`.

## Setup

### 1. Create a GitHub Personal Access Token

To access the private repository, you need to create a GitHub Personal Access Token (PAT):

1. Go to GitHub Settings ? Developer settings ? Personal access tokens ? Tokens (classic)
2. Click "Generate new token (classic)"
3. Give it a descriptive name (e.g., "DeploymentAPI Access")
4. Set expiration as needed
5. Select the following scopes:
   - `repo` (Full control of private repositories)
   - `workflow` (Update GitHub Action workflows)
6. Click "Generate token"
7. **Copy the token immediately** (you won't be able to see it again)

### 2. Configure the Application

Update the GitHub token in your `local.settings.json` or `local.settings.tablestorage.json`:

```json
{
  "Values": {
    "GitHub:Token": "ghp_your_actual_token_here",
    "GitHub:Owner": "AdaptiveBS",
    "GitHub:Repository": "CIApp"
  }
}
```

For production (Azure), set these as Application Settings:
- `GitHub:Token`
- `GitHub:Owner` (default: AdaptiveBS)
- `GitHub:Repository` (default: CIApp)

## API Endpoints

### Get GitHub Actions Workflow Runs
```
GET /api/github/actions
GET /api/github/actions?client=ClientName
```

Fetches all workflow runs from the repository. Optionally filter by client name.

**Response:**
```json
[
  {
    "id": 123456789,
    "name": "Deploy to Client ABC",
    "status": "completed",
    "conclusion": "success",
    "headBranch": "main",
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-01-15T10:35:00Z",
    "htmlUrl": "https://github.com/AdaptiveBS/CIApp/actions/runs/123456789"
  }
]
```

### Get GitHub Workflows
```
GET /api/github/workflows
```

Fetches all configured workflows in the repository.

**Response:**
```json
[
  {
    "id": 12345,
    "name": "Deploy to Production",
    "path": ".github/workflows/deploy-prod.yml",
    "state": "active",
    "createdAt": "2024-01-01T00:00:00Z"
  }
]
```

### Get Repository Information
```
GET /api/github/repository
```

Fetches repository metadata.

**Response:**
```json
{
  "id": 123456,
  "name": "CIApp",
  "fullName": "AdaptiveBS/CIApp",
  "private": true,
  "defaultBranch": "main",
  "createdAt": "2023-01-01T00:00:00Z"
}
```

## Usage Examples

### Fetch all workflow runs
```bash
curl http://localhost:7071/api/github/actions
```

### Fetch workflow runs for a specific client
```bash
curl "http://localhost:7071/api/github/actions?client=ABC"
```

### Get all workflows
```bash
curl http://localhost:7071/api/github/workflows
```

## Architecture

### Components

1. **IGitHubService** - Interface for GitHub operations
2. **GitHubService** - Implementation using GitHub REST API v3
3. **Models** - Data models for GitHub entities:
   - `GitHubWorkflowRun` - Represents a workflow run
   - `GitHubWorkflow` - Represents a workflow definition
   - `GitHubRepository` - Repository information
   - `GitHubUser` - User/actor information

### Functions

- **GetGitHubActionsFunction** - HTTP endpoint for workflow runs
- **GetGitHubWorkflowsFunction** - HTTP endpoint for workflows
- **GetGitHubRepositoryFunction** - HTTP endpoint for repository info

## Security Considerations

1. **Never commit the GitHub token** to source control
2. Store tokens in Azure Key Vault for production
3. Use managed identities where possible
4. Rotate tokens regularly
5. Use fine-grained permissions (minimum required access)

## Troubleshooting

### 401 Unauthorized
- Check that your GitHub token is valid
- Verify the token has `repo` scope
- Ensure the token hasn't expired

### 404 Not Found
- Verify the repository owner and name are correct
- Check that your token has access to the private repository
- Confirm you're a member of the AdaptiveBS organization

### Rate Limiting
- GitHub API has rate limits (5000 requests/hour for authenticated requests)
- The service automatically includes required headers for rate limit tracking
- Consider implementing caching if needed

## Future Enhancements

- [ ] Add caching layer for workflow runs
- [ ] Implement webhook handlers for real-time updates
- [ ] Add job details and logs retrieval
- [ ] Support for triggering workflows
- [ ] Integration with deployment registration
