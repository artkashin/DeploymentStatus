# Script to get all GitHub Action runs for "Update all customers" workflow using GitHub App authentication

$ErrorActionPreference = "Stop"

# Configuration from local.settings.json
$appId = "3906816"
$installationId = "137493603"
$owner = "AdaptiveBS"
$repo = "CIApp"
$privateKeyPath = "C:\Users\ArtemKashin\source\repos\DeplomentStatus\DeploymentAPI\.security\github-app-private-key.pem"

Write-Host "GitHub App Configuration:" -ForegroundColor Cyan
Write-Host "  App ID: $appId" -ForegroundColor Gray
Write-Host "  Installation ID: $installationId" -ForegroundColor Gray
Write-Host "  Repository: $owner/$repo" -ForegroundColor Gray
Write-Host ""

if (-not (Test-Path $privateKeyPath)) {
	Write-Host "ERROR: Private key file not found at: $privateKeyPath" -ForegroundColor Red
	Write-Host "Please ensure the GitHub App private key is available." -ForegroundColor Yellow
	exit 1
}

Write-Host "Authenticating with GitHub App..." -ForegroundColor Cyan

try {
	# Load required assemblies for JWT
	Add-Type -AssemblyName System.Security

	# Read private key
	$privateKeyPem = Get-Content $privateKeyPath -Raw

	# Create JWT for GitHub App
	# JWT expires in 10 minutes (max allowed by GitHub)
	$now = [Math]::Floor([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())
	$expiry = $now + 600

	# Create JWT header and payload
	$header = @{
		alg = "RS256"
		typ = "JWT"
	} | ConvertTo-Json -Compress

	$payload = @{
		iat = $now
		exp = $expiry
		iss = $appId
	} | ConvertTo-Json -Compress

	Write-Host "Creating JWT token..." -ForegroundColor Yellow

	# For PowerShell, we'll use the GitHub API with a simpler approach
	# Since creating JWT with RS256 in PowerShell is complex, let's try using the REST API directly

	# Alternative: Use gh CLI if available
	$ghCliPath = Get-Command gh -ErrorAction SilentlyContinue

	if ($ghCliPath) {
		Write-Host "Using GitHub CLI (gh)..." -ForegroundColor Green

		# Get workflow runs using gh CLI
		$runs = gh api "/repos/$owner/$repo/actions/runs?per_page=100" | ConvertFrom-Json

		# Filter for "Update all customers"
		$updateCustomerRuns = $runs.workflow_runs | Where-Object { $_.name -eq "Update all customers" }

		Write-Host ""
		Write-Host "Total workflow runs fetched: $($runs.total_count)" -ForegroundColor Cyan
		Write-Host "Runs matching 'Update all customers': $($updateCustomerRuns.Count)" -ForegroundColor Green
		Write-Host ""

		if ($updateCustomerRuns.Count -eq 0) {
			Write-Host "No workflow runs found with the name 'Update all customers'" -ForegroundColor Yellow
			Write-Host ""
			Write-Host "Available workflow names (showing unique):" -ForegroundColor Cyan
			$runs.workflow_runs | Select-Object -Property name -Unique | ForEach-Object {
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
			if ($conclusionSummary) {
				Write-Host "  By Conclusion:" -ForegroundColor Gray
				foreach ($conclusion in $conclusionSummary) {
					Write-Host "    - $($conclusion.Name): $($conclusion.Count)" -ForegroundColor Gray
				}
			}
		}

	} else {
		Write-Host "GitHub CLI (gh) is not installed or not in PATH." -ForegroundColor Red
		Write-Host ""
		Write-Host "To get the workflow runs, you can:" -ForegroundColor Yellow
		Write-Host "  1. Install GitHub CLI: winget install GitHub.cli" -ForegroundColor Gray
		Write-Host "  2. Or run: gh auth login" -ForegroundColor Gray
		Write-Host "  3. Then run this script again" -ForegroundColor Gray
		Write-Host ""
		Write-Host "Alternatively, use the curl command:" -ForegroundColor Yellow
		Write-Host "  curl -H `"Authorization: token YOUR_TOKEN`" https://api.github.com/repos/$owner/$repo/actions/runs" -ForegroundColor Gray
	}

} catch {
	Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
	Write-Host ""
	Write-Host "Error details: $($_.Exception)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
