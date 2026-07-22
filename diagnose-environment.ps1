Write-Host "=== Environment-Specific Troubleshooting ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Since the same config works elsewhere, let's check local environment issues..." -ForegroundColor Yellow
Write-Host ""

$pemPath = "C:\Users\ArtemKashin\source\repos\DeplomentStatus\DeploymentAPI\.security\github-app-private-key.pem"
$settingsPath = "DeploymentAPI\local.settings.json"

# 1. Check file access permissions
Write-Host "1. Checking file permissions..." -ForegroundColor Cyan
try {
    $pemAcl = Get-Acl $pemPath
    Write-Host "   ? Can read ACL for PEM file" -ForegroundColor Green
    
    # Try to read the file as the Functions process would
    $pemContent = [System.IO.File]::ReadAllText($pemPath)
    Write-Host "   ? Can read PEM file content ($($pemContent.Length) chars)" -ForegroundColor Green
    
    # Verify it's the right format
    if ($pemContent -match "-----BEGIN.*PRIVATE KEY-----") {
        Write-Host "   ? PEM file has correct header" -ForegroundColor Green
    } else {
        Write-Host "   ? PEM file format issue!" -ForegroundColor Red
    }
} catch {
    Write-Host "   ? Cannot read PEM file: $($_.Exception.Message)" -ForegroundColor Red
}

# 2. Check working directory
Write-Host "`n2. Checking working directory..." -ForegroundColor Cyan
$currentDir = Get-Location
Write-Host "   Current directory: $currentDir" -ForegroundColor Gray

Push-Location "DeploymentAPI"
$funcDir = Get-Location
Write-Host "   Functions directory: $funcDir" -ForegroundColor Gray

# Check if local.settings.json exists from Functions directory
if (Test-Path "local.settings.json") {
    Write-Host "   ? local.settings.json accessible from Functions dir" -ForegroundColor Green
} else {
    Write-Host "   ? local.settings.json NOT found from Functions dir" -ForegroundColor Red
}

# Check if PEM is accessible using the path in config
$configPemPath = "C:\Users\ArtemKashin\source\repos\DeplomentStatus\DeploymentAPI\.security\github-app-private-key.pem"
if (Test-Path $configPemPath) {
    Write-Host "   ? PEM file accessible via absolute path" -ForegroundColor Green
} else {
    Write-Host "   ? PEM file NOT accessible via absolute path" -ForegroundColor Red
}

Pop-Location

# 3. Check for multiple Functions processes
Write-Host "`n3. Checking for multiple Functions processes..." -ForegroundColor Cyan
$funcProcesses = Get-Process -Name "func" -ErrorAction SilentlyContinue
if ($funcProcesses) {
    Write-Host "   Found $($funcProcesses.Count) func process(es):" -ForegroundColor Yellow
    $funcProcesses | ForEach-Object {
        Write-Host "   - PID: $($_.Id), Started: $($_.StartTime), Memory: $([math]::Round($_.WorkingSet64/1MB))MB" -ForegroundColor Gray
    }
    
    if ($funcProcesses.Count -gt 1) {
        Write-Host "   ? Multiple Functions processes detected!" -ForegroundColor Yellow
        Write-Host "   ? This could cause configuration conflicts" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ? No Functions process running" -ForegroundColor Red
}

# 4. Check for cached assemblies or config
Write-Host "`n4. Checking for cached files..." -ForegroundColor Cyan
$binDebugPath = "DeploymentAPI\bin\Debug\net8.0"
if (Test-Path $binDebugPath) {
    $binFiles = Get-ChildItem $binDebugPath -Recurse
    Write-Host "   Found $($binFiles.Count) files in bin\Debug" -ForegroundColor Gray
    
    # Check if local.settings.json is in bin
    $binSettings = Get-ChildItem $binDebugPath -Filter "local.settings.json" -Recurse
    if ($binSettings) {
        Write-Host "   ? local.settings.json found in bin folder" -ForegroundColor Yellow
        Write-Host "   ? This copy might be outdated" -ForegroundColor Yellow
    }
}

# 5. Check environment variables
Write-Host "`n5. Checking environment variables..." -ForegroundColor Cyan
$relevantEnvVars = @(
    "FUNCTIONS_WORKER_RUNTIME",
    "AzureWebJobsStorage", 
    "ASPNETCORE_ENVIRONMENT",
    "GitHub__AuthType",
    "GitHub__AppId"
)

$foundEnvVars = $false
foreach ($envVar in $relevantEnvVars) {
    $value = [Environment]::GetEnvironmentVariable($envVar)
    if ($value) {
        Write-Host "   ? $envVar = $value" -ForegroundColor Yellow
        $foundEnvVars = $true
    }
}

if (-not $foundEnvVars) {
    Write-Host "   ? No conflicting environment variables" -ForegroundColor Green
} else {
    Write-Host "   ? Environment variables might override local.settings.json" -ForegroundColor Yellow
}

# 6. Check Functions Core Tools version
Write-Host "`n6. Checking Functions Core Tools..." -ForegroundColor Cyan
try {
    $funcVersion = func --version 2>&1
    Write-Host "   Version: $funcVersion" -ForegroundColor Gray
    
    if ($funcVersion -match "4\.") {
        Write-Host "   ? Using Functions Core Tools v4 (compatible with .NET 8)" -ForegroundColor Green
    } else {
        Write-Host "   ? Version might not be compatible with .NET 8" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ? Cannot determine func version" -ForegroundColor Red
}

# 7. Test PEM file hash consistency
Write-Host "`n7. Checking PEM file integrity..." -ForegroundColor Cyan
if (Test-Path $pemPath) {
    $pemHash = Get-FileHash $pemPath -Algorithm SHA256
    Write-Host "   SHA256: $($pemHash.Hash.Substring(0, 16))..." -ForegroundColor Gray
    Write-Host "   Size: $(([System.IO.FileInfo]$pemPath).Length) bytes" -ForegroundColor Gray
    Write-Host "   Modified: $((Get-Item $pemPath).LastWriteTime)" -ForegroundColor Gray
}

# 8. Clean build recommendation
Write-Host "`n=== Recommended Actions ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "Since this works in another environment, try:" -ForegroundColor Yellow
Write-Host ""

Write-Host "1. CLEAN RESTART (Most likely to fix):" -ForegroundColor White
Write-Host "   a) Stop all Functions processes:" -ForegroundColor Gray
Write-Host "      " -NoNewline
Write-Host "Get-Process -Name func | Stop-Process -Force" -ForegroundColor Cyan
Write-Host ""
Write-Host "   b) Clean bin/obj folders:" -ForegroundColor Gray
Write-Host "      " -NoNewline
Write-Host "Remove-Item DeploymentAPI\bin,DeploymentAPI\obj -Recurse -Force -ErrorAction SilentlyContinue" -ForegroundColor Cyan
Write-Host ""
Write-Host "   c) Rebuild and start:" -ForegroundColor Gray
Write-Host "      " -NoNewline
Write-Host "cd DeploymentAPI; dotnet build; func start" -ForegroundColor Cyan
Write-Host ""

Write-Host "2. Try relative path in config:" -ForegroundColor White
Write-Host "   Change in local.settings.json:" -ForegroundColor Gray
Write-Host '   "GitHub:PrivateKeyPath": ".security\\github-app-private-key.pem"' -ForegroundColor Cyan
Write-Host ""

Write-Host "3. Verify PEM file matches working environment:" -ForegroundColor White
Write-Host "   Compare file hash with working environment" -ForegroundColor Gray
Write-Host "   SHA256 (first 16 chars): $($pemHash.Hash.Substring(0, 16))..." -ForegroundColor Cyan
Write-Host ""

Write-Host "4. Check if antivirus/security software is blocking file access" -ForegroundColor White
Write-Host ""

# Automated clean restart option
Write-Host "=== Quick Fix ===" -ForegroundColor Cyan
Write-Host ""
$response = Read-Host "Would you like to do a clean restart now? (Y/n)"
if ($response -eq "" -or $response -eq "Y" -or $response -eq "y") {
    Write-Host ""
    Write-Host "Performing clean restart..." -ForegroundColor Yellow
    
    # Stop Functions
    Write-Host "  ? Stopping Functions..." -ForegroundColor Gray
    Get-Process -Name func -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2
    
    # Clean build artifacts
    Write-Host "  ? Cleaning build artifacts..." -ForegroundColor Gray
    Remove-Item "DeploymentAPI\bin" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "DeploymentAPI\obj" -Recurse -Force -ErrorAction SilentlyContinue
    
    # Rebuild
    Write-Host "  ? Building project..." -ForegroundColor Gray
    Push-Location "DeploymentAPI"
    $buildOutput = dotnet build 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ? Build successful" -ForegroundColor Green
        
        # Start Functions
        Write-Host "  ? Starting Functions..." -ForegroundColor Gray
        Start-Process -FilePath "func" -ArgumentList "start" -WorkingDirectory (Get-Location) -WindowStyle Normal
        
        Pop-Location
        
        Write-Host ""
        Write-Host "  ? Functions starting in new window" -ForegroundColor Green
        Write-Host ""
        Write-Host "  Wait 15 seconds, then test:" -ForegroundColor Yellow
        Write-Host "  .\test-workflows.ps1" -ForegroundColor Cyan
        
    } else {
        Write-Host "  ? Build failed" -ForegroundColor Red
        Write-Host $buildOutput -ForegroundColor Gray
        Pop-Location
    }
} else {
    Write-Host ""
    Write-Host "Skipped. Run the commands manually when ready." -ForegroundColor Gray
}

Write-Host ""
