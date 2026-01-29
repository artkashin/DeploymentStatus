# Script to setup Azurite (Azure Storage Emulator)

Write-Host "Setting up Azurite for local testing" -ForegroundColor Cyan
Write-Host "================================================`n" -ForegroundColor Cyan

# Check Azurite installation
Write-Host "1. Checking Azurite..." -ForegroundColor Yellow
try {
    $azuriteVersion = azurite --version 2>&1 | Select-String "azurite" | Select-Object -First 1
    if ($azuriteVersion) {
        Write-Host "   Azurite installed: $azuriteVersion" -ForegroundColor Green
    } else {
        throw "Azurite not found"
    }
} catch {
    Write-Host "   Azurite is not installed!" -ForegroundColor Red
    Write-Host "`n   Install Azurite:" -ForegroundColor Yellow
    Write-Host "   npm install -g azurite`n" -ForegroundColor White
    
    $install = Read-Host "Install Azurite now? (Y/N)"
    if ($install -eq "Y" -or $install -eq "y") {
        Write-Host "   Installing Azurite..." -ForegroundColor Yellow
        npm install -g azurite
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   Azurite installed!" -ForegroundColor Green
        } else {
            Write-Host "   Installation failed!" -ForegroundColor Red
            exit 1
        }
    } else {
        exit 1
    }
}

# Create data directory
Write-Host "`n2. Setting up data directory..." -ForegroundColor Yellow
$azuriteDir = "$env:USERPROFILE\.azurite"
if (-not (Test-Path $azuriteDir)) {
    New-Item -ItemType Directory -Path $azuriteDir | Out-Null
    Write-Host "   Created directory: $azuriteDir" -ForegroundColor Green
} else {
    Write-Host "   Directory exists: $azuriteDir" -ForegroundColor Green
}

# Check ports
Write-Host "`n3. Checking port availability..." -ForegroundColor Yellow
$ports = @(10000, 10001, 10002)
$portsAvailable = $true

foreach ($port in $ports) {
    $connection = Test-NetConnection -ComputerName localhost -Port $port -InformationLevel Quiet -WarningAction SilentlyContinue
    if ($connection) {
        Write-Host "   Port $port is in use!" -ForegroundColor Yellow
        $portsAvailable = $false
    } else {
        Write-Host "   Port $port is available" -ForegroundColor Green
    }
}

if (-not $portsAvailable) {
    Write-Host "`n   Some ports are in use. Azurite may not start." -ForegroundColor Yellow
    Write-Host "   Close other Azurite or Storage Emulator instances.`n" -ForegroundColor Yellow
}

# Update local.settings.json
Write-Host "`n4. Updating project settings..." -ForegroundColor Yellow
$settingsPath = "DeploymentAPI\local.settings.json"
if (Test-Path $settingsPath) {
    $settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
    $settings.Values.AzureWebJobsStorage = "UseDevelopmentStorage=true"
    $settings.Values.StorageType = "TableStorage"
    $settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath
    Write-Host "   local.settings.json updated for Table Storage" -ForegroundColor Green
} else {
    Write-Host "   local.settings.json not found" -ForegroundColor Yellow
}

# Start Azurite
Write-Host "`n5. Starting Azurite..." -ForegroundColor Yellow
Write-Host "   Data directory: $azuriteDir" -ForegroundColor Gray
Write-Host "   Ports: 10000 (Blob), 10001 (Queue), 10002 (Table)" -ForegroundColor Gray
Write-Host "`n   Azurite is running! (Press Ctrl+C to stop)`n" -ForegroundColor Green

# Start Azurite with logging
azurite --silent --location $azuriteDir --debug $azuriteDir\debug.log
