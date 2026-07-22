Write-Host "=== GitHub Connection Test ===" -ForegroundColor Cyan
Write-Host ""

$testProject = "GitHubConnectionTest"

# Build the test project
Write-Host "Building test project..." -ForegroundColor Yellow
cd $testProject
dotnet build --configuration Release

if ($LASTEXITCODE -ne 0) {
	Write-Host "Build failed!" -ForegroundColor Red
	cd ..
	exit 1
}

Write-Host "✓ Build successful" -ForegroundColor Green
Write-Host ""

# Run the test
Write-Host "Running connection test..." -ForegroundColor Yellow
Write-Host ""
Write-Host ("=" * 60) -ForegroundColor Gray

dotnet run --configuration Release --no-build

$exitCode = $LASTEXITCODE

Write-Host ("=" * 60) -ForegroundColor Gray
Write-Host ""

cd ..

if ($exitCode -eq 0) {
	Write-Host "✓ Test completed successfully!" -ForegroundColor Green
	Write-Host ""
	Write-Host "Your GitHub App configuration is working!" -ForegroundColor Cyan
	Write-Host "The issue is likely with the Azure Functions environment." -ForegroundColor Yellow
	Write-Host ""
	Write-Host "Try:" -ForegroundColor White
	Write-Host "  1. Clean Functions build: Remove-Item DeploymentAPI\bin,DeploymentAPI\obj -Recurse -Force" -ForegroundColor Gray
	Write-Host "  2. Rebuild: cd DeploymentAPI; dotnet build" -ForegroundColor Gray
	Write-Host "  3. Start: func start" -ForegroundColor Gray
} else {
	Write-Host "✗ Test failed!" -ForegroundColor Red
	Write-Host ""
	Write-Host "Check the error messages above for details." -ForegroundColor Yellow
	Write-Host ""
	Write-Host "Common issues:" -ForegroundColor White
	Write-Host "  • Wrong App ID or Installation ID" -ForegroundColor Gray
	Write-Host "  • Private key doesn't match the GitHub App" -ForegroundColor Gray
	Write-Host "  • GitHub App not installed on the repository" -ForegroundColor Gray
	Write-Host "  • PEM file path incorrect" -ForegroundColor Gray
}

Write-Host ""
