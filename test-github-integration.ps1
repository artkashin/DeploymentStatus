# Test GitHub Integration
# This script tests the GitHub integration endpoints

Write-Host "?? Testing GitHub Integration" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:7071/api"

# Check if Functions are running
Write-Host "Checking if Azure Functions are running..." -ForegroundColor Yellow
try {
    $healthCheck = Invoke-WebRequest -Uri "$baseUrl/status" -Method GET -ErrorAction SilentlyContinue
    Write-Host "? Functions are running" -ForegroundColor Green
} catch {
    Write-Host "??  Functions might not be running. Make sure to start them first." -ForegroundColor Red
    Write-Host "   Run: ./start-functions.ps1 or ./start-functions-en.ps1" -ForegroundColor Yellow
}
Write-Host ""

# Test 1: Get Repository Info
Write-Host "Test 1: Get Repository Information" -ForegroundColor Cyan
Write-Host "------------------------------------" -ForegroundColor Gray
try {
    $repoResponse = Invoke-RestMethod -Uri "$baseUrl/github/repository" -Method GET
    Write-Host "? Repository fetched successfully!" -ForegroundColor Green
    Write-Host "   Name: $($repoResponse.name)" -ForegroundColor White
    Write-Host "   Full Name: $($repoResponse.fullName)" -ForegroundColor White
    Write-Host "   Private: $($repoResponse.private)" -ForegroundColor White
    Write-Host "   Default Branch: $($repoResponse.defaultBranch)" -ForegroundColor White
} catch {
    Write-Host "? Failed to fetch repository info" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 2: Get Workflows
Write-Host "Test 2: Get GitHub Workflows" -ForegroundColor Cyan
Write-Host "------------------------------------" -ForegroundColor Gray
try {
    $workflowsResponse = Invoke-RestMethod -Uri "$baseUrl/github/workflows" -Method GET
    Write-Host "? Workflows fetched successfully!" -ForegroundColor Green
    Write-Host "   Total workflows: $($workflowsResponse.Count)" -ForegroundColor White
    
    if ($workflowsResponse.Count -gt 0) {
        Write-Host ""
        Write-Host "   Workflows:" -ForegroundColor White
        foreach ($workflow in $workflowsResponse) {
            Write-Host "   - [$($workflow.id)] $($workflow.name) ($($workflow.state))" -ForegroundColor Gray
            Write-Host "     Path: $($workflow.path)" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "? Failed to fetch workflows" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 3: Get All Workflow Runs
Write-Host "Test 3: Get All Workflow Runs" -ForegroundColor Cyan
Write-Host "------------------------------------" -ForegroundColor Gray
try {
    $runsResponse = Invoke-RestMethod -Uri "$baseUrl/github/actions" -Method GET
    Write-Host "? Workflow runs fetched successfully!" -ForegroundColor Green
    Write-Host "   Total runs: $($runsResponse.Count)" -ForegroundColor White
    
    if ($runsResponse.Count -gt 0) {
        Write-Host ""
        Write-Host "   Recent runs:" -ForegroundColor White
        $recentRuns = $runsResponse | Select-Object -First 5
        foreach ($run in $recentRuns) {
            $statusIcon = if ($run.conclusion -eq "success") { "?" } 
                         elseif ($run.conclusion -eq "failure") { "?" } 
                         elseif ($run.status -eq "in_progress") { "?" }
                         else { "??" }
            
            Write-Host "   $statusIcon [$($run.id)] $($run.name)" -ForegroundColor Gray
            Write-Host "      Status: $($run.status) | Conclusion: $($run.conclusion)" -ForegroundColor Gray
            Write-Host "      Branch: $($run.headBranch)" -ForegroundColor Gray
            Write-Host "      Created: $($run.createdAt)" -ForegroundColor Gray
            Write-Host "      URL: $($run.htmlUrl)" -ForegroundColor Gray
            Write-Host ""
        }
    }
} catch {
    Write-Host "? Failed to fetch workflow runs" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Message -like "*401*") {
        Write-Host ""
        Write-Host "   ?? This looks like an authentication error." -ForegroundColor Yellow
        Write-Host "   Make sure you have set a valid GitHub Personal Access Token in:" -ForegroundColor Yellow
        Write-Host "   - DeploymentAPI/local.settings.json" -ForegroundColor Yellow
        Write-Host "   - Set GitHub:Token with a token that has 'repo' scope" -ForegroundColor Yellow
    }
}
Write-Host ""

# Test 4: Filter Runs by Client
Write-Host "Test 4: Filter Runs by Client Name" -ForegroundColor Cyan
Write-Host "------------------------------------" -ForegroundColor Gray
$clientName = "test"
try {
    $filteredResponse = Invoke-RestMethod -Uri "$baseUrl/github/actions?client=$clientName" -Method GET
    Write-Host "? Filtered workflow runs fetched successfully!" -ForegroundColor Green
    Write-Host "   Filtered by client: '$clientName'" -ForegroundColor White
    Write-Host "   Matching runs: $($filteredResponse.Count)" -ForegroundColor White
    
    if ($filteredResponse.Count -gt 0) {
        Write-Host ""
        Write-Host "   Matching runs:" -ForegroundColor White
        foreach ($run in $filteredResponse) {
            Write-Host "   - [$($run.id)] $($run.name) - $($run.status)" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "? Failed to fetch filtered workflow runs" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

Write-Host "==============================" -ForegroundColor Cyan
Write-Host "?? GitHub Integration Tests Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "?? For more information, see:" -ForegroundColor Yellow
Write-Host "   - DeploymentAPI/GITHUB-INTEGRATION.md" -ForegroundColor Gray
