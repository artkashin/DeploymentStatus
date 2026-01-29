# Test storage persistence

$baseUrl = "http://localhost:7071/api"

Write-Host "Test: Storage Persistence" -ForegroundColor Cyan
Write-Host "===================================================`n" -ForegroundColor Cyan

Write-Host "Make sure Azure Functions are running!`n" -ForegroundColor Yellow

# Function to register deployment
function Register-TestDeployment {
    param(
        [string]$ClientId,
        [string]$ClientName,
        [string]$AppId,
        [string]$AppName,
        [string]$Version
    )
    
    $body = @{
        clientId = $ClientId
        clientName = $ClientName
        applicationId = $AppId
        applicationName = $AppName
        version = $Version
        status = 0
    } | ConvertTo-Json
    
    try {
        Invoke-RestMethod -Uri "$baseUrl/deployments" -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop | Out-Null
        return $true
    } catch {
        return $false
    }
}

# Test 1: Check API availability
Write-Host "1. Checking API..." -ForegroundColor Yellow
try {
    $cicd = @{ version = "2.0.0"; updatedBy = "Test" } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/cicd/version" -Method Post -Body $cicd -ContentType "application/json" -ErrorAction Stop | Out-Null
    Write-Host "   API is available" -ForegroundColor Green
} catch {
    Write-Host "   API not available! Start Functions first." -ForegroundColor Red
    exit 1
}

# Test 2: Register test data
Write-Host "`n2. Registering test data..." -ForegroundColor Yellow
$success = Register-TestDeployment -ClientId "test-001" -ClientName "Test Company" `
                                    -AppId "test-app" -AppName "Test App" -Version "2.0.0"
if ($success) {
    Write-Host "   Data registered" -ForegroundColor Green
} else {
    Write-Host "   Registration failed" -ForegroundColor Red
    exit 1
}

# Test 3: Check data persistence
Write-Host "`n3. Checking data persistence..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
try {
    $status = Invoke-RestMethod -Uri "$baseUrl/clients/test-001/status" -ErrorAction Stop
    if ($status.clientId -eq "test-001") {
        Write-Host "   Data saved and accessible" -ForegroundColor Green
        Write-Host "   Client: $($status.clientName)" -ForegroundColor Gray
        Write-Host "   Applications: $($status.applications.Count)" -ForegroundColor Gray
        Write-Host "   Version: $($status.applications[0].currentVersion)" -ForegroundColor Gray
    }
} catch {
    Write-Host "   Error retrieving data" -ForegroundColor Red
    exit 1
}

# Test 4: Persistence test (only works with Table Storage)
Write-Host "`n4. Data persistence test..." -ForegroundColor Yellow
Write-Host "   This test only works with Table Storage" -ForegroundColor Gray
Write-Host "   With In-Memory, data is lost on restart`n" -ForegroundColor Gray

Write-Host "Instructions to test persistence:" -ForegroundColor Cyan
Write-Host "   1. Stop Functions (Ctrl+C)" -ForegroundColor White
Write-Host "   2. Restart: .\rebuild-and-start.ps1" -ForegroundColor White
Write-Host "   3. Run this query:" -ForegroundColor White
Write-Host "      Invoke-RestMethod -Uri '$baseUrl/clients/test-001/status'" -ForegroundColor Gray
Write-Host "`n   Table Storage: data will remain" -ForegroundColor Green
Write-Host "   In-Memory: will get 404 Not Found" -ForegroundColor Yellow

Write-Host "`nTest completed!" -ForegroundColor Green
