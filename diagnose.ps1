# Diagnostic script for Azure Functions

Write-Host "Azure Functions Diagnostics" -ForegroundColor Cyan
Write-Host "================================`n" -ForegroundColor Cyan

# 1. Check .NET Runtime
Write-Host "1. .NET Runtimes:" -ForegroundColor Yellow
dotnet --list-runtimes | Select-String "Microsoft.NETCore.App"
Write-Host ""

# 2. Check .NET SDK
Write-Host "2. .NET SDK:" -ForegroundColor Yellow
dotnet --list-sdks
Write-Host ""

# 3. Check Azure Functions Core Tools
Write-Host "3. Azure Functions Core Tools:" -ForegroundColor Yellow
try {
    func --version
} catch {
    Write-Host "   NOT INSTALLED!" -ForegroundColor Red
}
Write-Host ""

# 4. Check project structure
Write-Host "4. DeploymentAPI project structure:" -ForegroundColor Yellow
if (Test-Path "DeploymentAPI") {
    Write-Host "   DeploymentAPI folder found" -ForegroundColor Green
    
    $requiredFiles = @("Program.cs", "host.json", "local.settings.json", "DeploymentAPI.csproj")
    foreach ($file in $requiredFiles) {
        $path = "DeploymentAPI\$file"
        if (Test-Path $path) {
            Write-Host "   $file" -ForegroundColor Green
        } else {
            Write-Host "   $file MISSING!" -ForegroundColor Red
        }
    }
} else {
    Write-Host "   DeploymentAPI folder not found!" -ForegroundColor Red
}
Write-Host ""

# 5. Check Functions
Write-Host "5. Azure Functions in project:" -ForegroundColor Yellow
$functionsDir = "DeploymentAPI\Functions"
if (Test-Path $functionsDir) {
    $functions = Get-ChildItem "$functionsDir\*.cs" -File
    Write-Host "   Functions found: $($functions.Count)" -ForegroundColor Gray
    foreach ($func in $functions) {
        Write-Host "   - $($func.BaseName)" -ForegroundColor Gray
    }
} else {
    Write-Host "   Functions folder not found!" -ForegroundColor Red
}
Write-Host ""

# 6. Try building
Write-Host "6. Building project:" -ForegroundColor Yellow
Push-Location DeploymentAPI
$buildOutput = dotnet build --nologo 2>&1
Pop-Location

if ($LASTEXITCODE -eq 0) {
    Write-Host "   Build successful" -ForegroundColor Green
} else {
    Write-Host "   Build failed:" -ForegroundColor Red
    Write-Host $buildOutput -ForegroundColor Red
}
Write-Host ""

# 7. Check bin folder
Write-Host "7. Checking output files:" -ForegroundColor Yellow
$binPath = "DeploymentAPI\bin\Debug\net8.0"
if (Test-Path $binPath) {
    Write-Host "   bin folder found" -ForegroundColor Green
    
    $dllPath = "$binPath\DeploymentAPI.dll"
    if (Test-Path $dllPath) {
        Write-Host "   DeploymentAPI.dll created" -ForegroundColor Green
        $dllInfo = Get-Item $dllPath
        Write-Host "   Size: $($dllInfo.Length) bytes" -ForegroundColor Gray
    } else {
        Write-Host "   DeploymentAPI.dll NOT FOUND!" -ForegroundColor Red
    }
} else {
    Write-Host "   bin folder not found!" -ForegroundColor Red
}
Write-Host ""

Write-Host "Recommendations:" -ForegroundColor Cyan
Write-Host "   1. Ensure .NET 8.0 Runtime is installed" -ForegroundColor White
Write-Host "   2. Install Azure Functions Core Tools v4:" -ForegroundColor White
Write-Host "      npm install -g azure-functions-core-tools@4 --unsafe-perm true" -ForegroundColor Gray
Write-Host "   3. Run: .\rebuild-and-start-en.ps1" -ForegroundColor White
Write-Host ""
