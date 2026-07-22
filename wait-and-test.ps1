Write-Host "Waiting 20 seconds for Functions to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 20
Write-Host ""
& ".\test-workflows.ps1"
