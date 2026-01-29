# Master startup script

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  Deployment Status - Startup Menu" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Available commands:" -ForegroundColor Yellow
Write-Host "  1. Start Full Stack (API + Dashboard)" -ForegroundColor White
Write-Host "  2. Start API with In-Memory storage" -ForegroundColor White
Write-Host "  3. Start API with Table Storage (Azurite)" -ForegroundColor White
Write-Host "  4. Start Dashboard only" -ForegroundColor White
Write-Host "  5. Run diagnostics" -ForegroundColor White
Write-Host "  6. Run API tests" -ForegroundColor White
Write-Host "  7. Test version logic" -ForegroundColor White
Write-Host ""

$choice = Read-Host "Select option (1-7)"

switch ($choice) {
    "1" {
        Write-Host "`nStarting Full Stack (API + Dashboard)..." -ForegroundColor Green
        .\start-full-stack.ps1
    }
    "2" {
        Write-Host "`nStarting API with In-Memory storage..." -ForegroundColor Green
        .\rebuild-and-start.ps1
    }
    "3" {
        Write-Host "`nStarting API with Table Storage..." -ForegroundColor Green
        .\start-with-tablestorage.ps1
    }
    "4" {
        Write-Host "`nStarting Dashboard only..." -ForegroundColor Green
        Write-Host "Make sure API is running on port 7071!" -ForegroundColor Yellow
        .\start-dashboard.ps1
    }
    "5" {
        Write-Host "`nRunning diagnostics..." -ForegroundColor Green
        .\diagnose.ps1
    }
    "6" {
        Write-Host "`nRunning API tests..." -ForegroundColor Green
        Write-Host "Make sure the API is running in another terminal!" -ForegroundColor Yellow
        Start-Sleep -Seconds 2
        .\test-api.ps1
    }
    "7" {
        Write-Host "`nRunning version logic tests..." -ForegroundColor Green
        Write-Host "Make sure the API is running in another terminal!" -ForegroundColor Yellow
        Start-Sleep -Seconds 2
        .\test-version-logic.ps1
    }
    default {
        Write-Host "`nInvalid option. Starting Full Stack..." -ForegroundColor Yellow
        .\start-full-stack.ps1
    }
}

