# Minimal Azure Functions startup test

Write-Host "Checking environment..." -ForegroundColor Cyan

# Check .NET SDK
Write-Host "`n1. Checking .NET SDK:" -ForegroundColor Yellow
$dotnetVersion = dotnet --version
Write-Host "   .NET SDK version: $dotnetVersion" -ForegroundColor Green

# Check Azure Functions Core Tools
Write-Host "`n2. Checking Azure Functions Core Tools:" -ForegroundColor Yellow
try {
    $funcVersion = func --version
    Write-Host "   Azure Functions Core Tools: $funcVersion" -ForegroundColor Green
} catch {
    Write-Host "   Azure Functions Core Tools not installed!" -ForegroundColor Red
    Write-Host "   Install: npm install -g azure-functions-core-tools@4 --unsafe-perm true" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n3. Building project:" -ForegroundColor Yellow
Set-Location DeploymentAPI
dotnet clean --nologo --verbosity quiet | Out-Null
dotnet build --nologo --verbosity quiet

if ($LASTEXITCODE -eq 0) {
    Write-Host "   Project built successfully" -ForegroundColor Green
} else {
    Write-Host "   Build failed!" -ForegroundColor Red
    Set-Location ..
    exit 1
}

Write-Host "`nStarting Azure Functions..." -ForegroundColor Cyan
Write-Host "   (Press Ctrl+C to stop)" -ForegroundColor Gray
Write-Host ""

# Start Functions
func start --verbose

Set-Location ..
