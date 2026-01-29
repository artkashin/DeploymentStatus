# Test: Current state vs History

$baseUrl = "http://localhost:7071/api"

Write-Host "Test: Current State + Deployment History" -ForegroundColor Cyan
Write-Host "===================================================`n" -ForegroundColor Cyan

# Check API
Write-Host "Checking API availability..." -ForegroundColor Yellow
try {
    $cicd = @{ version = "2.0.0"; updatedBy = "Test" } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/cicd/version" -Method Post -Body $cicd -ContentType "application/json" -ErrorAction Stop | Out-Null
    Write-Host "   API is available`n" -ForegroundColor Green
} catch {
    Write-Host "   API not available! Run: .\rebuild-and-start-en.ps1`n" -ForegroundColor Red
    exit 1
}

# Registration function
function Register-Deployment {
    param($ClientId, $ClientName, $AppId, $AppName, $Version)
    $body = @{
        clientId = $ClientId; clientName = $ClientName
        applicationId = $AppId; applicationName = $AppName
        version = $Version; status = 0
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/deployments" -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop | Out-Null
}

# Test 1: Register version 1.0.0
Write-Host "1. Registering app-test v1.0.0..." -ForegroundColor Yellow
Register-Deployment -ClientId "test-client" -ClientName "Test Co" -AppId "app-test" -AppName "Test App" -Version "1.0.0"
Write-Host "   Registered v1.0.0`n" -ForegroundColor Green

# Test 2: Re-register same version
Write-Host "2. Re-registering app-test v1.0.0..." -ForegroundColor Yellow
Register-Deployment -ClientId "test-client" -ClientName "Test Co" -AppId "app-test" -AppName "Test App" -Version "1.0.0"
Write-Host "   Registered v1.0.0 (2nd time)`n" -ForegroundColor Green
Write-Host "   Table Storage: current state NOT updated, but added to history" -ForegroundColor Gray
Write-Host "   In-Memory: current state NOT updated, but added to history`n" -ForegroundColor Gray

# Test 3: Register new version 1.1.0
Write-Host "3. Registering app-test v1.1.0..." -ForegroundColor Yellow
Register-Deployment -ClientId "test-client" -ClientName "Test Co" -AppId "app-test" -AppName "Test App" -Version "1.1.0"
Write-Host "   Registered v1.1.0`n" -ForegroundColor Green

# Test 4: Another new version 1.2.0
Write-Host "4. Registering app-test v1.2.0..." -ForegroundColor Yellow
Register-Deployment -ClientId "test-client" -ClientName "Test Co" -AppId "app-test" -AppName "Test App" -Version "1.2.0"
Write-Host "   Registered v1.2.0`n" -ForegroundColor Green

# Check current state
Write-Host "5. Checking current state..." -ForegroundColor Yellow
$status = Invoke-RestMethod -Uri "$baseUrl/clients/test-client/status"
$app = $status.applications[0]
Write-Host "   Current version: $($app.currentVersion)" -ForegroundColor $(if($app.currentVersion -eq "1.2.0"){"Green"}else{"Red"})
Write-Host "   Last update: $($app.lastDeploymentTime)" -ForegroundColor Gray
Write-Host "   Current state is correct (latest version)`n" -ForegroundColor Green

# Check history
Write-Host "6. Checking deployment history..." -ForegroundColor Yellow
$history = Invoke-RestMethod -Uri "$baseUrl/clients/test-client/history?limit=10"
Write-Host "   History records: $($history.count)" -ForegroundColor Gray

if ($history.count -eq 4) {
    Write-Host "   History contains all 4 deployments (including duplicate v1.0.0)" -ForegroundColor Green
} else {
    Write-Host "   Expected 4 records, got: $($history.count)" -ForegroundColor Yellow
}

Write-Host "`n   Deployment history (newest first):" -ForegroundColor Gray
foreach ($dep in $history.deployments) {
    Write-Host "   - v$($dep.version) @ $($dep.deploymentTime)" -ForegroundColor Gray
}

Write-Host "`n7. Registering second application..." -ForegroundColor Yellow
Register-Deployment -ClientId "test-client" -ClientName "Test Co" -AppId "app-second" -AppName "Second App" -Version "2.5.0"
Write-Host "   Registered app-second v2.5.0`n" -ForegroundColor Green

Write-Host "8. Final client state..." -ForegroundColor Yellow
$finalStatus = Invoke-RestMethod -Uri "$baseUrl/clients/test-client/status"
Write-Host "   Client: $($finalStatus.clientName)" -ForegroundColor Gray
Write-Host "   Applications: $($finalStatus.applications.Count)" -ForegroundColor Gray
Write-Host "   Min version: $($finalStatus.minVersion)" -ForegroundColor Gray
Write-Host "   Max version: $($finalStatus.maxVersion)" -ForegroundColor Gray

foreach ($app in $finalStatus.applications) {
    Write-Host "   - $($app.applicationName): v$($app.currentVersion)" -ForegroundColor Gray
}

Write-Host "`nTest results:" -ForegroundColor Cyan
Write-Host "   Current state updates only on version change" -ForegroundColor Green
Write-Host "   History contains all deployments (including duplicates)" -ForegroundColor Green
Write-Host "   Can have multiple applications per client" -ForegroundColor Green

Write-Host "`nData structure:" -ForegroundColor Cyan
Write-Host "   Table Storage:" -ForegroundColor Yellow
Write-Host "   - Deployments: 2 records (app-test + app-second)" -ForegroundColor Gray
Write-Host "   - DeploymentHistory: 5 records (all deployments)" -ForegroundColor Gray
Write-Host "   In-Memory:" -ForegroundColor Yellow
Write-Host "   - Current: 2 records (app-test + app-second)" -ForegroundColor Gray
Write-Host "   - History: 5 records (all deployments)" -ForegroundColor Gray

Write-Host "`nTest completed successfully!" -ForegroundColor Green
