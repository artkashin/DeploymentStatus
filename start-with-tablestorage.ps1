# Quick start with Table Storage

Write-Host "Starting Deployment API with Azure Table Storage" -ForegroundColor Cyan
Write-Host "===============================================`n" -ForegroundColor Cyan

# Function to check if process is running
function Test-ProcessRunning {
    param([string]$ProcessName)
    return (Get-Process -Name $ProcessName -ErrorAction SilentlyContinue) -ne $null
}

# Check Azurite
Write-Host "1. Checking Azurite..." -ForegroundColor Yellow
if (Test-ProcessRunning -ProcessName "azurite") {
    Write-Host "   Azurite is already running" -ForegroundColor Green
} else {
    Write-Host "   Azurite is not running. Starting..." -ForegroundColor Yellow
    
    # Start Azurite in background
    $azuriteDir = "$env:USERPROFILE\.azurite"
    if (-not (Test-Path $azuriteDir)) {
        New-Item -ItemType Directory -Path $azuriteDir | Out-Null
    }
    
    Start-Process -FilePath "azurite" -ArgumentList "--silent", "--location", $azuriteDir -WindowStyle Hidden
    Start-Sleep -Seconds 3
    
    if (Test-ProcessRunning -ProcessName "azurite") {
        Write-Host "   Azurite started" -ForegroundColor Green
    } else {
        Write-Host "   Failed to start Azurite!" -ForegroundColor Red
        Write-Host "   Install: npm install -g azurite" -ForegroundColor Yellow
        exit 1
    }
}

# Configure local.settings.json
Write-Host "`n2. Configuring settings..." -ForegroundColor Yellow
$settingsPath = "DeploymentAPI\local.settings.json"
$settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
$settings.Values.StorageType = "TableStorage"
$settings.Values.AzureWebJobsStorage = "UseDevelopmentStorage=true"
$settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath
Write-Host "   StorageType = TableStorage" -ForegroundColor Green

# Build project
Write-Host "`n3. Building project..." -ForegroundColor Yellow
Set-Location DeploymentAPI
dotnet clean --nologo --verbosity quiet | Out-Null
dotnet build --nologo --verbosity quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host "   Project built" -ForegroundColor Green
} else {
    Write-Host "   Build failed!" -ForegroundColor Red
    Set-Location ..
    exit 1
}

# Start Functions
Write-Host "`n4. Starting Azure Functions..." -ForegroundColor Yellow
Write-Host "   (Press Ctrl+C to stop)`n" -ForegroundColor Gray
Write-Host "Data will be saved to Azure Table Storage (emulator)" -ForegroundColor Cyan
Write-Host "Data directory: $azuriteDir`n" -ForegroundColor Gray

func start

Set-Location ..
