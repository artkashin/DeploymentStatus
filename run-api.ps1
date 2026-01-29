# Script to run and test Deployment API

Write-Host "Starting Deployment API..." -ForegroundColor Cyan
Write-Host ""

# Clean previous builds
Write-Host "Cleaning..." -ForegroundColor Yellow
dotnet clean --nologo --verbosity quiet
dotnet build --nologo --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Project built successfully" -ForegroundColor Green
Write-Host ""
Write-Host "To start, run:" -ForegroundColor Cyan
Write-Host "   cd DeploymentAPI" -ForegroundColor White
Write-Host "   func start" -ForegroundColor White
Write-Host ""
Write-Host "After starting, test with:" -ForegroundColor Cyan
Write-Host "   .\test-api.ps1" -ForegroundColor White
Write-Host ""
