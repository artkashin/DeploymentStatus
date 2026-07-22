# Test script for Workflow Run Customer Status API

Write-Host "`n===============================================================================================================" -ForegroundColor Cyan
Write-Host "Testing Workflow Run Customer Status API" -ForegroundColor White
Write-Host "===============================================================================================================`n" -ForegroundColor Cyan

$baseUrl = "http://localhost:7071"
$runId = 29418806053

Write-Host "Base URL: $baseUrl" -ForegroundColor Gray
Write-Host "Test Run ID: $runId`n" -ForegroundColor Gray

# Test 1: Get specific workflow run customer status
Write-Host "[TEST 1] GET /api/workflow-runs/$runId/customer-status" -ForegroundColor Cyan
Write-Host "---------------------------------------------------------------------------------------------------" -ForegroundColor Gray

try {
	$response = Invoke-RestMethod -Uri "$baseUrl/api/workflow-runs/$runId/customer-status" -Method Get

	Write-Host "✓ Request successful!" -ForegroundColor Green
	Write-Host ""
	Write-Host "Response Summary:" -ForegroundColor White
	Write-Host "  Run ID: $($response.runId)" -ForegroundColor Gray
	Write-Host "  Run Number: #$($response.runNumber)" -ForegroundColor Gray
	Write-Host "  Workflow: $($response.workflowName)" -ForegroundColor Gray
	Write-Host "  Status: $($response.status)" -ForegroundColor Gray
	Write-Host "  Overall Success: $($response.overallSuccess)" -ForegroundColor $(if ($response.overallSuccess) { 'Green' } else { 'Yellow' })
	Write-Host "  Total Customers: $($response.totalCustomers)" -ForegroundColor Gray
	Write-Host "  Successful: $($response.successfulInstallations)" -ForegroundColor Green
	Write-Host "  Failed: $($response.failedInstallations)" -ForegroundColor Red
	Write-Host ""

	Write-Host "Customers:" -ForegroundColor White
	foreach ($customer in $response.customers) {
		$icon = if ($customer.installed) { '✓' } else { '✗' }
		$color = if ($customer.installed) { 'Green' } else { 'Red' }
		Write-Host "  $icon $($customer.name) - $($customer.status) ($($customer.durationSeconds)s on $($customer.runner))" -ForegroundColor $color
	}

	Write-Host ""
	Write-Host "✓ TEST 1 PASSED" -ForegroundColor Green

} catch {
	Write-Host "✗ TEST 1 FAILED" -ForegroundColor Red
	Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red

	if ($_.Exception.Response) {
		$statusCode = $_.Exception.Response.StatusCode.value__
		Write-Host "Status Code: $statusCode" -ForegroundColor Red
	}
}

Write-Host ""
Write-Host "===============================================================================================================`n" -ForegroundColor Cyan

# Test 2: Get latest "Update all customers" status
Write-Host "[TEST 2] GET /api/update-all-customers/latest" -ForegroundColor Cyan
Write-Host "---------------------------------------------------------------------------------------------------" -ForegroundColor Gray

try {
	$response = Invoke-RestMethod -Uri "$baseUrl/api/update-all-customers/latest" -Method Get

	Write-Host "✓ Request successful!" -ForegroundColor Green
	Write-Host ""
	Write-Host "Latest Run Summary:" -ForegroundColor White
	Write-Host "  Run ID: $($response.runId)" -ForegroundColor Gray
	Write-Host "  Run Number: #$($response.runNumber)" -ForegroundColor Gray
	Write-Host "  Workflow: $($response.workflowName)" -ForegroundColor Gray
	Write-Host "  Status: $($response.status)" -ForegroundColor Gray
	Write-Host "  Total Customers: $($response.totalCustomers)" -ForegroundColor Gray
	Write-Host "  Successful: $($response.successfulInstallations)" -ForegroundColor Green
	Write-Host "  Failed: $($response.failedInstallations)" -ForegroundColor Red
	Write-Host ""

	Write-Host "Success Rate: $([math]::Round(($response.successfulInstallations / $response.totalCustomers) * 100, 1))%" -ForegroundColor Cyan
	Write-Host ""
	Write-Host "✓ TEST 2 PASSED" -ForegroundColor Green

} catch {
	Write-Host "✗ TEST 2 FAILED" -ForegroundColor Red
	Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red

	if ($_.Exception.Response) {
		$statusCode = $_.Exception.Response.StatusCode.value__
		Write-Host "Status Code: $statusCode" -ForegroundColor Red
	}
}

Write-Host ""
Write-Host "===============================================================================================================" -ForegroundColor Cyan
Write-Host "Test Complete!" -ForegroundColor Green
Write-Host "===============================================================================================================`n" -ForegroundColor Cyan
