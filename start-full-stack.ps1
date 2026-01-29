# Start both API and Dashboard

Write-Host "Starting Deployment API + Dashboard" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "This will start:" -ForegroundColor Yellow
Write-Host "  1. Azure Functions API (port 7071)" -ForegroundColor White
Write-Host "  2. Dashboard Web App (port 8080)" -ForegroundColor White
Write-Host ""

# Start API in background
Write-Host "Starting API..." -ForegroundColor Yellow
$apiJob = Start-Job -ScriptBlock {
    Set-Location $using:PWD
    .\rebuild-and-start.ps1
}

Write-Host "   API starting in background (Job ID: $($apiJob.Id))" -ForegroundColor Green
Write-Host "   Waiting for API to start..." -ForegroundColor Gray
Start-Sleep -Seconds 10

# Check if API is running
try {
    $null = Invoke-WebRequest -Uri "http://localhost:7071/api/cicd/version" -Method Get -TimeoutSec 5 -ErrorAction Stop
    Write-Host "   API is running!" -ForegroundColor Green
} catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 404) {
        Write-Host "   API is running (no version set yet)" -ForegroundColor Green
    } else {
        Write-Host "   API may still be starting..." -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Starting Dashboard..." -ForegroundColor Yellow

# Open browser
Write-Host "   Opening browser..." -ForegroundColor Gray
Start-Process "http://localhost:8080"

Write-Host ""
Write-Host "Services are running!" -ForegroundColor Green
Write-Host "  API:       http://localhost:7071" -ForegroundColor White
Write-Host "  Dashboard: http://localhost:8080" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to stop all services" -ForegroundColor Yellow
Write-Host ""

# Start dashboard (blocking)
Set-Location DeploymentDashboard
try {
    python -m http.server 8080
} finally {
    # Cleanup
    Write-Host ""
    Write-Host "Stopping services..." -ForegroundColor Yellow
    Stop-Job -Job $apiJob
    Remove-Job -Job $apiJob
    Write-Host "Services stopped" -ForegroundColor Green
}
