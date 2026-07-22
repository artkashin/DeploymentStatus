Write-Host "=== Simplified GitHub App Authentication Test ===" -ForegroundColor Cyan

# Configuration
$settingsPath = "DeploymentAPI\local.settings.json"
$settings = Get-Content $settingsPath | ConvertFrom-Json
$appId = $settings.Values.'GitHub:AppId'
$installationId = $settings.Values.'GitHub:InstallationId'
$owner = $settings.Values.'GitHub:Owner'
$repo = $settings.Values.'GitHub:Repository'

Write-Host "`nConfiguration:" -ForegroundColor Yellow
Write-Host "  App ID: $appId"
Write-Host "  Installation ID: $installationId"
Write-Host "  Owner: $owner"
Write-Host "  Repository: $repo"

# Test 1: Check if the installation exists
Write-Host "`n1. Checking GitHub App Installation..." -ForegroundColor Yellow
Write-Host "   Visit: https://github.com/settings/installations" -ForegroundColor Cyan
Write-Host "   Look for installation ID: $installationId" -ForegroundColor Gray
Write-Host "   The ID should be in the URL when you click 'Configure' on your app" -ForegroundColor Gray

# Test 2: Check Functions logs
Write-Host "`n2. Checking Azure Functions..." -ForegroundColor Yellow
$functionsProcess = Get-Process -Name "func" -ErrorAction SilentlyContinue
if ($functionsProcess) {
    Write-Host "   ? Functions process is running (PID: $($functionsProcess.Id))" -ForegroundColor Green
    Write-Host "   ? Check the Functions console window for error details" -ForegroundColor Cyan
} else {
    Write-Host "   ? Functions process not found" -ForegroundColor Red
    Write-Host "   ? Run: .\restart-and-fix.ps1" -ForegroundColor Cyan
}

# Test 3: Test the Functions endpoint directly
Write-Host "`n3. Testing Functions endpoint..." -ForegroundColor Yellow
try {
    $functionsUrl = "http://localhost:7071/api/github/actions"
    Write-Host "   Testing: $functionsUrl" -ForegroundColor Gray
    
    $response = Invoke-RestMethod -Uri $functionsUrl -Method Get -ErrorAction Stop
    
    if ($response.error) {
        Write-Host "   ? Functions returned error" -ForegroundColor Red
        Write-Host "   Error: $($response.error)" -ForegroundColor Red
        Write-Host "   Message: $($response.message)" -ForegroundColor Yellow
        
        # Parse the error message for clues
        if ($response.message -like "*401*" -or $response.message -like "*Unauthorized*") {
            Write-Host "`n   ? 401 Unauthorized indicates GitHub authentication is failing" -ForegroundColor Yellow
            Write-Host "   Possible causes:" -ForegroundColor Cyan
            Write-Host "     1. Wrong App ID or Installation ID" -ForegroundColor White
            Write-Host "     2. Private key doesn't match the GitHub App" -ForegroundColor White
            Write-Host "     3. GitHub App not installed on the repository" -ForegroundColor White
            Write-Host "     4. GitHub App permissions insufficient" -ForegroundColor White
        }
    } else {
        Write-Host "   ? SUCCESS! Functions are working" -ForegroundColor Green
    }
    
} catch {
    Write-Host "   ? Failed to call Functions" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Check GitHub App configuration
Write-Host "`n4. Verify GitHub App Configuration..." -ForegroundColor Yellow
Write-Host "   ? Open: https://github.com/settings/apps" -ForegroundColor Cyan
Write-Host "   ? Find your app (App ID: $appId)" -ForegroundColor Gray
Write-Host "   ? Check:" -ForegroundColor Gray
Write-Host "      • App ID matches: $appId" -ForegroundColor White
Write-Host "      • Private key is active (not revoked)" -ForegroundColor White
Write-Host "      • App is installed on: $owner/$repo" -ForegroundColor White
Write-Host "      • App has 'Actions' permission set to 'Read-only' or 'Read and write'" -ForegroundColor White

Write-Host "`n5. Check Installation ID..." -ForegroundColor Yellow
Write-Host "   ? Open: https://github.com/settings/installations" -ForegroundColor Cyan
Write-Host "   ? Click 'Configure' on your app" -ForegroundColor Gray
Write-Host "   ? The URL will look like: .../installations/INSTALLATION_ID" -ForegroundColor Gray
Write-Host "   ? Verify the ID matches: $installationId" -ForegroundColor White

Write-Host "`n=== Manual Verification Steps ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Step 1: Verify App ID" -ForegroundColor Yellow
Write-Host "  • Go to: https://github.com/settings/apps" -ForegroundColor White
Write-Host "  • Click on your app" -ForegroundColor White
Write-Host "  • Scroll to 'App ID' - should be: $appId" -ForegroundColor White
Write-Host ""
Write-Host "Step 2: Verify Installation ID" -ForegroundColor Yellow
Write-Host "  • Go to: https://github.com/organizations/$owner/settings/installations" -ForegroundColor White
Write-Host "  • Or: https://github.com/settings/installations (for personal account)" -ForegroundColor White
Write-Host "  • Click 'Configure' on your app" -ForegroundColor White
Write-Host "  • URL contains installation ID - should be: $installationId" -ForegroundColor White
Write-Host ""
Write-Host "Step 3: Verify Private Key" -ForegroundColor Yellow
Write-Host "  • Go to: https://github.com/settings/apps" -ForegroundColor White
Write-Host "  • Click on your app" -ForegroundColor White
Write-Host "  • Scroll to 'Private keys'" -ForegroundColor White
Write-Host "  • If you see 'No private keys', generate a new one" -ForegroundColor White
Write-Host "  • Download it and replace: DeploymentAPI\.security\github-app-private-key.pem" -ForegroundColor White
Write-Host ""
Write-Host "Step 4: Verify Permissions" -ForegroundColor Yellow
Write-Host "  • Go to: https://github.com/settings/apps" -ForegroundColor White
Write-Host "  • Click on your app" -ForegroundColor White
Write-Host "  • Check 'Repository permissions'" -ForegroundColor White
Write-Host "  • 'Actions' should be 'Read-only' (minimum)" -ForegroundColor White
Write-Host "  • 'Contents' should be 'Read-only' (minimum)" -ForegroundColor White
Write-Host ""
Write-Host "Step 5: Verify Repository Access" -ForegroundColor Yellow
Write-Host "  • Go to: https://github.com/settings/installations" -ForegroundColor White
Write-Host "  • Click 'Configure' on your app" -ForegroundColor White
Write-Host "  • Under 'Repository access', check:" -ForegroundColor White
Write-Host "    - If 'All repositories' is selected, or" -ForegroundColor White
Write-Host "    - If 'Only select repositories' includes: $owner/$repo" -ForegroundColor White
Write-Host ""
Write-Host "After verification, restart Functions:" -ForegroundColor Cyan
Write-Host "  .\restart-and-fix.ps1" -ForegroundColor White
Write-Host "  Wait 15 seconds" -ForegroundColor White
Write-Host "  .\test-workflows.ps1" -ForegroundColor White
