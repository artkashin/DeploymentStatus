# Dashboard Debug Test
# This script tests the dashboard configuration and API connectivity

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Dashboard Configuration Test" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Check if config file exists and is correct
Write-Host "1. Checking config.js file..." -ForegroundColor Yellow
$configPath = "DeploymentDashboard\js\config.js"

if (Test-Path $configPath) {
	Write-Host "   ✓ config.js found" -ForegroundColor Green

	$configContent = Get-Content $configPath -Raw

	if ($configContent -match "useProd:\s*true") {
		Write-Host "   ✓ useProd is set to TRUE (production forced)" -ForegroundColor Green
	} else {
		Write-Host "   ✗ useProd is NOT set to true" -ForegroundColor Red
	}

	if ($configContent -match "func-deployment-status-api") {
		Write-Host "   ✓ Production API URL found" -ForegroundColor Green
	} else {
		Write-Host "   ✗ Production API URL missing" -ForegroundColor Red
	}
} else {
	Write-Host "   ✗ config.js NOT found!" -ForegroundColor Red
	exit 1
}

Write-Host ""

# Test 2: Check if server is running
Write-Host "2. Checking if HTTP server is running..." -ForegroundColor Yellow
try {
	$response = Invoke-WebRequest -Uri "http://localhost:8080" -TimeoutSec 2 -UseBasicParsing
	Write-Host "   ✓ Server is running on port 8080" -ForegroundColor Green
} catch {
	Write-Host "   ✗ Server is NOT running on port 8080" -ForegroundColor Red
	Write-Host "   Run: .\start-dashboard.ps1" -ForegroundColor Yellow
}

Write-Host ""

# Test 3: Test production API connectivity
Write-Host "3. Testing production API connection..." -ForegroundColor Yellow
$prodAPI = "https://func-deployment-status-api-g0egd2dbc9d9c2d9.eastus2-01.azurewebsites.net/api/clients/status"

try {
	Write-Host "   Testing: $prodAPI" -ForegroundColor Gray
	$apiResponse = Invoke-RestMethod -Uri $prodAPI -TimeoutSec 10
	Write-Host "   ✓ Production API is ACCESSIBLE" -ForegroundColor Green
	Write-Host "   Response: $($apiResponse.Count) clients found" -ForegroundColor Green
} catch {
	Write-Host "   ✗ Production API NOT accessible" -ForegroundColor Red
	Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 4: Check if local API is running (optional)
Write-Host "4. Checking local API (optional)..." -ForegroundColor Yellow
try {
	$localResponse = Invoke-WebRequest -Uri "http://localhost:7071/api/clients/status" -TimeoutSec 2 -UseBasicParsing
	Write-Host "   ✓ Local API is running on port 7071" -ForegroundColor Green
} catch {
	Write-Host "   ℹ Local API not running (this is OK if using production)" -ForegroundColor Gray
}

Write-Host ""

# Test 5: Check browser cache issue
Write-Host "5. Recommendations..." -ForegroundColor Yellow
Write-Host "   • Dashboard URL: http://localhost:8080" -ForegroundColor Cyan
Write-Host "   • Debug page: http://localhost:8080/debug.html" -ForegroundColor Cyan
Write-Host ""
Write-Host "   To fix browser cache issues:" -ForegroundColor Yellow
Write-Host "   - Press Ctrl+Shift+R (hard refresh)" -ForegroundColor White
Write-Host "   - Or open debug.html to verify config" -ForegroundColor White
Write-Host ""

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Test Complete!" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Open debug page: http://localhost:8080/debug.html" -ForegroundColor Green
Write-Host ""
