# Start Dashboard locally

Write-Host "Starting Deployment Dashboard..." -ForegroundColor Cyan
Write-Host ""

# Check if Python is installed
Write-Host "1. Checking Python..." -ForegroundColor Yellow
try {
    $pythonVersion = python --version 2>&1
    Write-Host "   Python found: $pythonVersion" -ForegroundColor Green
} catch {
    Write-Host "   Python not installed!" -ForegroundColor Red
    Write-Host "   Install Python from: https://www.python.org/downloads/" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "   Alternative: Use VS Code Live Server extension" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "2. Starting HTTP server..." -ForegroundColor Yellow
Write-Host "   Dashboard URL: http://localhost:8080" -ForegroundColor Green
Write-Host "   Make sure Azure Functions are running on port 7071" -ForegroundColor Yellow
Write-Host ""
Write-Host "   Press Ctrl+C to stop" -ForegroundColor Gray
Write-Host ""

# Start server
Set-Location DeploymentDashboard
python -m http.server 8080
