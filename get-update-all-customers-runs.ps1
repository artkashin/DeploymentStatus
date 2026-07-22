# Script to get all GitHub Action runs for "Update all customers" workflow

# Read configuration
$configPath = "DeploymentAPI\local.settings.json"
if (Test-Path $configPath) {
	$config = Get-Content $configPath | ConvertFrom-Json
	$owner = $config.Values.'GitHub:Owner'
	$repo = $config.Values.'GitHub:Repository'
	$token = $config.Values.'GitHub:PersonalAccessToken'
	$appId = $config.Values.'GitHubApp:AppId'
} else {
	Write-Host "Configuration file not found. Using defaults."
	$owner = "AdaptiveBS"
	$repo = "CIApp"
}

Write-Host "Fetching workflow runs from: $owner/$repo" -ForegroundColor Cyan
Write-Host ""

# GitHub API endpoint
$apiUrl = "https://api.github.com/repos/$owner/$repo/actions/runs?per_page=100"

# Headers
$headers = @{
	"Accept" = "application/vnd.github+json"
	"X-GitHub-Api-Version" = "2022-11-28"
	"User-Agent" = "DeploymentAPI-Script"
}

# Add authentication if token is available
if ($token) {
	$headers["Authorization"] = "Bearer $token"
	Write-Host "Using Personal Access Token for authentication" -ForegroundColor Green
} else {
	Write-Host "No authentication token found. Making unauthenticated request (limited to 60 requests/hour)" -ForegroundColor Yellow
}

Write-Host ""

try {
	# Fetch workflow runs
	$response = Invoke-RestMethod -Uri $apiUrl -Headers $headers -Method Get

	# Filter runs with name "Update all customers"
	$updateCustomerRuns = $response.workflow_runs | Where-Object { $_.name -eq "Update all customers" }

	Write-Host "Total workflow runs fetched: $($response.total_count)" -ForegroundColor Cyan
	Write-Host "Runs matching 'Update all customers': $($updateCustomerRuns.Count)" -ForegroundColor Green
	Write-Host ""

	if ($updateCustomerRuns.Count -eq 0) {
		Write-Host "No workflow runs found with the name 'Update all customers'" -ForegroundColor Yellow
		Write-Host ""
		Write-Host "Available workflow names:" -ForegroundColor Cyan
		$response.workflow_runs | Select-Object -Property name -Unique | ForEach-Object {
			Write-Host "  - $($_.name)" -ForegroundColor Gray
		}
	} else {
		Write-Host "=====================================================================================================" -ForegroundColor Cyan

		foreach ($run in $updateCustomerRuns) {
			Write-Host ""
			Write-Host "Run ID: $($run.id)" -ForegroundColor White
			Write-Host "  Name: $($run.name)" -ForegroundColor Cyan
			Write-Host "  Display Title: $($run.display_title)" -ForegroundColor Gray
			Write-Host "  Status: $($run.status)" -ForegroundColor $(if ($run.status -eq "completed") { "Green" } else { "Yellow" })
			Write-Host "  Conclusion: $($run.conclusion)" -ForegroundColor $(
				switch ($run.conclusion) {
					"success" { "Green" }
					"failure" { "Red" }
					"cancelled" { "Yellow" }
					default { "Gray" }
				}
			)
			Write-Host "  Branch: $($run.head_branch)" -ForegroundColor Gray
			Write-Host "  Event: $($run.event)" -ForegroundColor Gray
			Write-Host "  Run Number: $($run.run_number)" -ForegroundColor Gray
			Write-Host "  Created At: $($run.created_at)" -ForegroundColor Gray
			Write-Host "  Updated At: $($run.updated_at)" -ForegroundColor Gray
			Write-Host "  Actor: $($run.actor.login)" -ForegroundColor Gray
			Write-Host "  URL: $($run.html_url)" -ForegroundColor Blue
			Write-Host "  -------------------------------------------------------------------------------------------------"
		}

		Write-Host ""
		Write-Host "Summary:" -ForegroundColor Cyan
		Write-Host "  Total 'Update all customers' runs: $($updateCustomerRuns.Count)" -ForegroundColor White

		$statusSummary = $updateCustomerRuns | Group-Object -Property status
		Write-Host "  By Status:" -ForegroundColor Gray
		foreach ($status in $statusSummary) {
			Write-Host "    - $($status.Name): $($status.Count)" -ForegroundColor Gray
		}

		$conclusionSummary = $updateCustomerRuns | Where-Object { $_.conclusion } | Group-Object -Property conclusion
		Write-Host "  By Conclusion:" -ForegroundColor Gray
		foreach ($conclusion in $conclusionSummary) {
			Write-Host "    - $($conclusion.Name): $($conclusion.Count)" -ForegroundColor Gray
		}
	}

} catch {
	Write-Host "Error fetching workflow runs: $($_.Exception.Message)" -ForegroundColor Red
	Write-Host ""
	Write-Host "Error details: $($_.Exception)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
