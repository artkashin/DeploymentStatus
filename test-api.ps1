# Test script for Deployment API

$baseUrl = "http://localhost:7071/api"

Write-Host "Testing Deployment API" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Check API availability
Write-Host "Checking API availability..." -ForegroundColor Yellow
try {
    $null = Invoke-RestMethod -Uri "$baseUrl/cicd/version" -Method Get -ErrorAction Stop
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 404) {
        Write-Host "API is available (version not yet set)" -ForegroundColor Green
    } else {
        Write-Host "API is not available. Make sure Functions are running (func start)" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""

# Test 1: Set CI/CD version
Write-Host "1. Setting CI/CD version..." -ForegroundColor Yellow
$cicdBody = @{
    version = "1.3.0"
    updatedBy = "Test Script"
    notes = "Automated test deployment"
} | ConvertTo-Json

try {
    $cicdResult = Invoke-RestMethod -Uri "$baseUrl/cicd/version" -Method Post -Body $cicdBody -ContentType "application/json"
    Write-Host "   CI/CD version: $($cicdResult.version.version)" -ForegroundColor Green
    Write-Host "   Updated: $($cicdResult.version.updatedAt)" -ForegroundColor Gray
} catch {
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test 2: Register deployments
Write-Host "2. Registering deployments..." -ForegroundColor Yellow

$deployments = @(
    @{ clientId="client-001"; clientName="Company ABC"; applicationId="app-hr"; applicationName="HR Module"; version="1.3.0"; status=0 },
    @{ clientId="client-001"; clientName="Company ABC"; applicationId="app-finance"; applicationName="Finance Module"; version="1.2.5"; status=0 },
    @{ clientId="client-002"; clientName="Company XYZ"; applicationId="app-hr"; applicationName="HR Module"; version="1.3.0"; status=0 },
    @{ clientId="client-002"; clientName="Company XYZ"; applicationId="app-finance"; applicationName="Finance Module"; version="1.3.0"; status=0 }
)

foreach ($dep in $deployments) {
    try {
        $depJson = $dep | ConvertTo-Json
        $null = Invoke-RestMethod -Uri "$baseUrl/deployments" -Method Post -Body $depJson -ContentType "application/json"
        Write-Host "   $($dep.clientName) - $($dep.applicationName) v$($dep.version)" -ForegroundColor Green
    } catch {
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""

# Test 3: Get client status
Write-Host "3. Checking client-001 status..." -ForegroundColor Yellow
try {
    $status = Invoke-RestMethod -Uri "$baseUrl/clients/client-001/status"
    Write-Host "   Client: $($status.clientName)" -ForegroundColor Green
    Write-Host "   Applications: $($status.applications.Count)" -ForegroundColor Gray
    Write-Host "   Min version: $($status.minVersion)" -ForegroundColor $(if($status.minVersion -eq "1.2.5"){"Green"}else{"Yellow"})
    Write-Host "   Max version: $($status.maxVersion)" -ForegroundColor $(if($status.maxVersion -eq "1.3.0"){"Green"}else{"Yellow"})
    Write-Host "   CI/CD version: $($status.ciCdVersion)" -ForegroundColor Gray
    
    if ($status.minVersion -ne $status.ciCdVersion) {
        Write-Host "   Client is behind CI/CD version!" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 4: Get all client statuses
Write-Host "4. Getting all client statuses..." -ForegroundColor Yellow
try {
    $allStatuses = Invoke-RestMethod -Uri "$baseUrl/clients/status"
    Write-Host "   Total clients: $($allStatuses.totalClients)" -ForegroundColor Green
    
    foreach ($client in $allStatuses.clients) {
        $status_icon = if ($client.minVersion -eq $client.ciCdVersion) { "OK" } else { "WARN" }
        Write-Host "   [$status_icon] $($client.clientName): $($client.minVersion) - $($client.maxVersion)" -ForegroundColor Gray
    }
} catch {
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 5: Get deployment history
Write-Host "5. Getting deployment history..." -ForegroundColor Yellow
try {
    $history = Invoke-RestMethod -Uri "$baseUrl/clients/client-001/history?limit=5"
    Write-Host "   History records: $($history.count)" -ForegroundColor Green
} catch {
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Testing complete!" -ForegroundColor Green
Write-Host ""
