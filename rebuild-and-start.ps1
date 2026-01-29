# Clean rebuild and start script

Write-Host "Cleaning and rebuilding project..." -ForegroundColor Cyan

Set-Location DeploymentAPI

# Clean
Write-Host "`n1. Cleaning old files..." -ForegroundColor Yellow
Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
dotnet clean --nologo --verbosity quiet

# Restore packages
Write-Host "2. Restoring packages..." -ForegroundColor Yellow
dotnet restore --nologo --verbosity quiet

# Build
Write-Host "3. Building project..." -ForegroundColor Yellow
dotnet build --nologo --verbosity quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host "   Build successful!" -ForegroundColor Green
    
    Write-Host "`n4. Checking runtime configuration..." -ForegroundColor Yellow
    $runtimeConfigPath = "bin\Debug\net8.0\DeploymentAPI.runtimeconfig.json"
    if (Test-Path $runtimeConfigPath) {
        Write-Host "   Runtime config found" -ForegroundColor Green
        Get-Content $runtimeConfigPath | ConvertFrom-Json | ConvertTo-Json -Depth 10
    }
    
    Write-Host "`nStarting Azure Functions..." -ForegroundColor Cyan
    Write-Host "   (Press Ctrl+C to stop)`n" -ForegroundColor Gray
    
    func start
} else {
    Write-Host "   Build failed!" -ForegroundColor Red
}

Set-Location ..
