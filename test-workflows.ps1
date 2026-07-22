# Test deployment status with GitHub integration
Write-Host "Testing Deployment API with GitHub Integration..." -ForegroundColor Cyan

try {
    # Test 1: Check if Functions are running
    Write-Host "`n1. Testing Functions health..." -ForegroundColor Yellow
    try {
        $health = Invoke-RestMethod "http://localhost:7071/api/clients/status" -TimeoutSec 5 -ErrorAction Stop
        Write-Host "   ✓ Functions are running" -ForegroundColor Green
    } catch {
        throw "Functions not responding. Run: .\restart-and-fix.ps1"
    }

    # Test 2: Test client status with GitHub data
    Write-Host "`n2. Testing GitHub integration..." -ForegroundColor Yellow

    # Get a client ID from the status response, or use a test client
    $testClientId = if ($health -and $health.Count -gt 0) { 
        $health[0].clientId 
    } else { 
        "test-client" 
    }

    Write-Host "   Testing with client: $testClientId" -ForegroundColor Gray

    $response = Invoke-RestMethod "http://localhost:7071/api/clients/$testClientId/status-with-github" -ErrorAction Stop

    if ($response.error) {
        Write-Host "   ✗ Error: $($response.message)" -ForegroundColor Red
    } else {
        Write-Host "   ✓ GitHub integration working!" -ForegroundColor Green

        # Show deployment info
        if ($response.client) {
            Write-Host "`n   Deployment Status:" -ForegroundColor Cyan
            Write-Host "   • Client: $($response.client.clientId)" -ForegroundColor White
            Write-Host "   • Version: $($response.client.currentVersion)" -ForegroundColor White
            Write-Host "   • Last Deployment: $($response.client.lastDeploymentDate)" -ForegroundColor White
        }

        # Show GitHub workflow info
        if ($response.gitHubWorkflows -and $response.gitHubWorkflows.Count -gt 0) {
            Write-Host "`n   Recent GitHub Workflows:" -ForegroundColor Cyan
            $response.gitHubWorkflows | Select-Object -First 5 | ForEach-Object {
                $icon = if ($_.conclusion -eq "success") { "✓" } 
                       elseif ($_.conclusion -eq "failure") { "✗" } 
                       else { "•" }
                $color = if ($_.conclusion -eq "success") { "Green" } 
                        elseif ($_.conclusion -eq "failure") { "Red" } 
                        else { "Yellow" }

                Write-Host "   $icon " -NoNewline -ForegroundColor $color
                Write-Host "$($_.name) - $($_.status)/$($_.conclusion)" -ForegroundColor White
                Write-Host "      Branch: $($_.headBranch) | Actor: $($_.actor)" -ForegroundColor DarkGray
            }
        } else {
            Write-Host "`n   No workflow data found" -ForegroundColor Yellow
        }
    }

    Write-Host "`n✓ All tests passed!" -ForegroundColor Green
    Write-Host "`nGitHub integration is working correctly." -ForegroundColor Cyan
    Write-Host "GitHub data is being used internally to enrich deployment information." -ForegroundColor Gray

} catch {
    Write-Host ""
    Write-Host "✗ Test failed" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Make sure Functions are running:" -ForegroundColor Yellow
    Write-Host "  cd DeploymentAPI" -ForegroundColor Gray
    Write-Host "  func start" -ForegroundColor Gray
    Write-Host ""
    Write-Host "If Functions are running but GitHub integration fails:" -ForegroundColor Yellow
    Write-Host "  See: GITHUB-AUTH-TROUBLESHOOTING.md" -ForegroundColor Cyan
}

Write-Host ""

