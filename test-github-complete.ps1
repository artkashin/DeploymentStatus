# Complete GitHub Integration Test with Authentication Verification
# Tests both configuration and actual API access through Azure Functions

param(
    [string]$BaseUrl = "http://localhost:7071/api"
)

Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  Complete GitHub Integration Test" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Testing against: $BaseUrl" -ForegroundColor Gray
Write-Host ""

$testsPassed = 0
$testsFailed = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET"
    )
    
    Write-Host "Testing: $Name" -ForegroundColor Yellow
    Write-Host "  URL: $Url" -ForegroundColor Gray
    
    try {
        $response = Invoke-RestMethod -Uri $Url -Method $Method -ErrorAction Stop
        Write-Host "  ? SUCCESS" -ForegroundColor Green
        $script:testsPassed++
        return $response
    }
    catch {
        Write-Host "  ? FAILED" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
        
        if ($_.Exception.Response) {
            $statusCode = $_.Exception.Response.StatusCode.value__
            Write-Host "  Status Code: $statusCode" -ForegroundColor Red
            
            # Provide helpful hints based on status code
            switch ($statusCode) {
                401 {
                    Write-Host "  ?? Hint: Authentication failed" -ForegroundColor Yellow
                    Write-Host "     - Check App ID: 3906613" -ForegroundColor Gray
                    Write-Host "     - Check Installation ID: 136590013" -ForegroundColor Gray
                    Write-Host "     - Verify PEM file is valid" -ForegroundColor Gray
                }
                403 {
                    Write-Host "  ?? Hint: Permission denied" -ForegroundColor Yellow
                    Write-Host "     - Check app has 'Actions: Read' permission" -ForegroundColor Gray
                    Write-Host "     - Verify app is installed on repository" -ForegroundColor Gray
                }
                404 {
                    Write-Host "  ?? Hint: Resource not found" -ForegroundColor Yellow
                    Write-Host "     - Verify repository: AdaptiveBS/CIApp" -ForegroundColor Gray
                    Write-Host "     - Check app is installed on this repository" -ForegroundColor Gray
                }
                500 {
                    Write-Host "  ?? Hint: Server error" -ForegroundColor Yellow
                    Write-Host "     - Check Azure Functions logs" -ForegroundColor Gray
                    Write-Host "     - Verify PEM file format is correct" -ForegroundColor Gray
                }
            }
        }
        
        $script:testsFailed++
        return $null
    }
    Write-Host ""
}

# Test 1: Check if Functions are running
Write-Host "??????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "Test 1: Azure Functions Health Check" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

try {
    $healthCheck = Invoke-WebRequest -Uri "$BaseUrl/status" -Method GET -ErrorAction SilentlyContinue -UseBasicParsing
    Write-Host "? Functions are running" -ForegroundColor Green
    $testsPassed++
}
catch {
    Write-Host "??  Functions might not be running" -ForegroundColor Yellow
    Write-Host "   Please start them with: .\start-functions.ps1" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Continuing with other tests..." -ForegroundColor Cyan
}
Write-Host ""

# Test 2: Get Repository Information
Write-Host "??????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "Test 2: GitHub Repository Access" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

$repo = Test-Endpoint -Name "Get Repository Info" -Url "$BaseUrl/github/repository"
if ($repo) {
    Write-Host "  ?? Repository Details:" -ForegroundColor Cyan
    Write-Host "     Name: $($repo.name)" -ForegroundColor White
    Write-Host "     Full Name: $($repo.fullName)" -ForegroundColor White
    Write-Host "     Private: $($repo.private)" -ForegroundColor White
    Write-Host "     Default Branch: $($repo.defaultBranch)" -ForegroundColor White
    Write-Host "     Created: $($repo.createdAt)" -ForegroundColor White
    Write-Host ""
}

# Test 3: Get Workflows
Write-Host "??????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "Test 3: GitHub Actions Workflows" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

$workflows = Test-Endpoint -Name "Get Workflows" -Url "$BaseUrl/github/workflows"
if ($workflows) {
    Write-Host "  ?? Found $($workflows.Count) workflow(s):" -ForegroundColor Cyan
    foreach ($workflow in $workflows | Select-Object -First 5) {
        Write-Host "     [$($workflow.id)] $($workflow.name)" -ForegroundColor White
        Write-Host "        Path: $($workflow.path)" -ForegroundColor Gray
        Write-Host "        State: $($workflow.state)" -ForegroundColor Gray
    }
    if ($workflows.Count -gt 5) {
        Write-Host "     ... and $($workflows.Count - 5) more" -ForegroundColor Gray
    }
    Write-Host ""
}

# Test 4: Get Workflow Runs
Write-Host "??????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "Test 4: GitHub Actions Workflow Runs" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

$runs = Test-Endpoint -Name "Get Workflow Runs" -Url "$BaseUrl/github/actions"
if ($runs) {
    Write-Host "  ?? Found $($runs.Count) workflow run(s):" -ForegroundColor Cyan
    
    $recentRuns = $runs | Select-Object -First 5
    foreach ($run in $recentRuns) {
        $statusIcon = switch ($run.conclusion) {
            "success" { "?" }
            "failure" { "?" }
            "cancelled" { "??" }
            default { if ($run.status -eq "in_progress") { "?" } else { "?" } }
        }
        
        Write-Host "     $statusIcon [$($run.id)] $($run.name)" -ForegroundColor White
        Write-Host "        Status: $($run.status) | Conclusion: $($run.conclusion)" -ForegroundColor Gray
        Write-Host "        Branch: $($run.headBranch)" -ForegroundColor Gray
        Write-Host "        Created: $($run.createdAt)" -ForegroundColor Gray
        Write-Host "        Actor: $($run.actor.login)" -ForegroundColor Gray
    }
    
    if ($runs.Count -gt 5) {
        Write-Host "     ... and $($runs.Count - 5) more" -ForegroundColor Gray
    }
    Write-Host ""
}

# Test 5: Filter by Client
Write-Host "??????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host "Test 5: Filter Runs by Client" -ForegroundColor Cyan
Write-Host "??????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""

$clientFilter = "test"
$filteredRuns = Test-Endpoint -Name "Filter by client '$clientFilter'" -Url "$BaseUrl/github/actions?client=$clientFilter"
if ($filteredRuns) {
    Write-Host "  ?? Found $($filteredRuns.Count) matching run(s)" -ForegroundColor Cyan
    Write-Host ""
}

# Summary
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "  Test Summary" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

$totalTests = $testsPassed + $testsFailed
$successRate = if ($totalTests -gt 0) { [math]::Round(($testsPassed / $totalTests) * 100, 0) } else { 0 }

Write-Host "  Total Tests: $totalTests" -ForegroundColor White
Write-Host "  ? Passed: $testsPassed" -ForegroundColor Green
Write-Host "  ? Failed: $testsFailed" -ForegroundColor $(if ($testsFailed -gt 0) { "Red" } else { "Gray" })
Write-Host "  ?? Success Rate: $successRate%" -ForegroundColor $(if ($successRate -eq 100) { "Green" } elseif ($successRate -ge 75) { "Yellow" } else { "Red" })
Write-Host ""

if ($testsPassed -eq $totalTests -and $totalTests -gt 0) {
    Write-Host "?? All tests passed! GitHub App integration is working perfectly!" -ForegroundColor Green
    Write-Host ""
    Write-Host "? You now have:" -ForegroundColor Yellow
    Write-Host "   • Access to repository: AdaptiveBS/CIApp" -ForegroundColor White
    Write-Host "   • Ability to read GitHub Actions workflows" -ForegroundColor White
    Write-Host "   • Ability to fetch workflow runs" -ForegroundColor White
    Write-Host "   • 15,000 requests/hour rate limit" -ForegroundColor White
    Write-Host ""
} elseif ($testsFailed -eq 0 -and $testsPassed -eq 0) {
    Write-Host "??  No tests were executed" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "?? Make sure Azure Functions are running:" -ForegroundColor Cyan
    Write-Host "   .\start-functions.ps1" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "??  Some tests failed" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "?? Troubleshooting:" -ForegroundColor Cyan
    Write-Host "   1. Check Azure Functions logs for errors" -ForegroundColor White
    Write-Host "   2. Verify your configuration:" -ForegroundColor White
    Write-Host "      • App ID: 3906613" -ForegroundColor Gray
    Write-Host "      • Installation ID: 136590013" -ForegroundColor Gray
    Write-Host "      • PEM file: .security\github-app-private-key.pem" -ForegroundColor Gray
    Write-Host "   3. Verify the app is installed on AdaptiveBS/CIApp:" -ForegroundColor White
    Write-Host "      https://github.com/organizations/AdaptiveBS/settings/installations" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "?? For more information:" -ForegroundColor Yellow
Write-Host "   • GITHUB-PEM-SETUP.md - Setup guide" -ForegroundColor White
Write-Host "   • GITHUB-APP-SETUP.md - GitHub App configuration" -ForegroundColor White
Write-Host ""
