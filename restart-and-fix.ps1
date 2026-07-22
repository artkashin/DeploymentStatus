# Restart Functions with PEM file in place
Write-Host "Stopping Functions..." -ForegroundColor Yellow
Get-Process -Name "func" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

Write-Host "Verifying PEM file..." -ForegroundColor Yellow
$pemPath = "DeploymentAPI\bin\output\.security\github-app-private-key.pem"
if (Test-Path $pemPath) {
    $size = (Get-Item $pemPath).Length
    Write-Host "? PEM file exists: $size bytes" -ForegroundColor Green
} else {
    Write-Host "? PEM file missing - copying now..." -ForegroundColor Red
    New-Item -Path "DeploymentAPI\bin\output\.security" -ItemType Directory -Force | Out-Null
    Copy-Item "DeploymentAPI\.security\github-app-private-key.pem" $pemPath -Force
    Write-Host "? PEM file copied" -ForegroundColor Green
}

Write-Host "Starting Functions..." -ForegroundColor Yellow
Push-Location DeploymentAPI
Start-Process powershell -ArgumentList "-NoExit","-Command","func start"
Pop-Location

Write-Host ""
Write-Host "? Functions starting in new window" -ForegroundColor Green
Write-Host "? Wait 15 seconds for startup, then run: .\test-workflows.ps1" -ForegroundColor Cyan
