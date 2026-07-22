# GitHub App Configuration Verification
Write-Host "=== GitHub App Configuration Mismatch Detection ===" -ForegroundColor Cyan
Write-Host ""

$settings = Get-Content "DeploymentAPI\local.settings.json" | ConvertFrom-Json
$appId = $settings.Values.'GitHub:AppId'
$installationId = $settings.Values.'GitHub:InstallationId'
$pemPath = $settings.Values.'GitHub:PrivateKeyPath'

Write-Host "Current Configuration:" -ForegroundColor Yellow
Write-Host "  App ID: $appId"
Write-Host "  Installation ID: $installationId"
Write-Host "  PEM Path: $pemPath"
Write-Host ""

# Calculate PEM file fingerprint
if (Test-Path $pemPath) {
	$pemHash = (Get-FileHash $pemPath -Algorithm SHA256).Hash
	Write-Host "PEM File Fingerprint: $($pemHash.Substring(0, 16))..." -ForegroundColor Gray
	Write-Host ""
}

Write-Host "=== The Problem ===" -ForegroundColor Red
Write-Host ""
Write-Host "GitHub API is rejecting your JWT token with 401 Unauthorized." -ForegroundColor White
Write-Host "This happens when requesting an installation access token:" -ForegroundColor White
Write-Host "  POST /app/installations/$installationId/access_tokens" -ForegroundColor Gray
Write-Host ""

Write-Host "=== Why This Happens ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. PRIVATE KEY MISMATCH (Most Common)" -ForegroundColor White
Write-Host "   The PEM file doesn't belong to GitHub App $appId" -ForegroundColor Gray
Write-Host "   → Someone regenerated the key in GitHub but didn't update your local file" -ForegroundColor Gray
Write-Host ""
Write-Host "2. INSTALLATION ID WRONG" -ForegroundColor White
Write-Host "   Installation $installationId doesn't belong to App $appId" -ForegroundColor Gray
Write-Host "   → App was uninstalled/reinstalled, changing the Installation ID" -ForegroundColor Gray
Write-Host ""
Write-Host "3. APP ID WRONG" -ForegroundColor White
Write-Host "   You have the wrong App ID configured" -ForegroundColor Gray
Write-Host "   → Less likely if this worked before" -ForegroundColor Gray
Write-Host ""

Write-Host "=== How to Fix ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "OPTION 1: Get Fresh Credentials from Working Environment" -ForegroundColor Green
Write-Host "Since you said this works in another environment:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  a) On the WORKING machine, find the PEM file" -ForegroundColor White
Write-Host "  b) Calculate its hash:" -ForegroundColor White
Write-Host "     " -NoNewline
Write-Host "(Get-FileHash 'path\to\file.pem' -Algorithm SHA256).Hash.Substring(0,16)" -ForegroundColor Cyan
Write-Host ""
Write-Host "  c) Compare with your local hash: $($pemHash.Substring(0, 16))..." -ForegroundColor White
Write-Host ""
Write-Host "  d) If different, copy the PEM file from working machine to:" -ForegroundColor White  
Write-Host "     " -NoNewline
Write-Host "$pemPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "  e) Verify App ID and Installation ID match the working environment" -ForegroundColor White
Write-Host ""

Write-Host "OPTION 2: Regenerate Everything" -ForegroundColor Green
Write-Host ""
Write-Host "Step 1: Get the correct App ID" -ForegroundColor White
Write-Host "  → Visit: https://github.com/settings/apps" -ForegroundColor Cyan
Write-Host "  → Click on your app" -ForegroundColor Gray
Write-Host "  → Note the 'App ID' number" -ForegroundColor Gray
Write-Host ""

Write-Host "Step 2: Generate new private key" -ForegroundColor White
Write-Host "  → On the same page, scroll to 'Private keys'" -ForegroundColor Gray
Write-Host "  → Click 'Generate a private key'" -ForegroundColor Gray
Write-Host "  → Download the .pem file" -ForegroundColor Gray
Write-Host "  → Copy to: $pemPath" -ForegroundColor Cyan
Write-Host ""

Write-Host "Step 3: Get Installation ID" -ForegroundColor White
Write-Host "  → Visit: https://github.com/settings/installations" -ForegroundColor Cyan
Write-Host "  → Click 'Configure' on your app" -ForegroundColor Gray
Write-Host "  → URL shows: .../installations/INSTALLATION_ID" -ForegroundColor Gray
Write-Host "  → Note this number" -ForegroundColor Gray
Write-Host ""

Write-Host "Step 4: Update local.settings.json" -ForegroundColor White
Write-Host '  "GitHub:AppId": "YOUR_APP_ID",' -ForegroundColor Cyan
Write-Host '  "GitHub:InstallationId": "YOUR_INSTALLATION_ID"' -ForegroundColor Cyan
Write-Host ""

Write-Host "Step 5: Restart" -ForegroundColor White
Write-Host "  → Stop Functions (Ctrl+C)" -ForegroundColor Gray
Write-Host "  → Run: func start" -ForegroundColor Cyan
Write-Host "  → Test: .\test-workflows.ps1" -ForegroundColor Cyan
Write-Host ""

Write-Host "OPTION 3: Contact Person with Working Environment" -ForegroundColor Green
Write-Host ""
Write-Host "Ask them to share:" -ForegroundColor White
Write-Host "  1. The exact App ID" -ForegroundColor Gray
Write-Host "  2. The exact Installation ID" -ForegroundColor Gray
Write-Host "  3. The PEM file (or regenerate a new one together)" -ForegroundColor Gray
Write-Host ""

Write-Host "=== Quick Test ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Compare your configuration with the working environment:" -ForegroundColor Yellow
Write-Host ""
Write-Host "YOUR VALUES:" -ForegroundColor White
Write-Host "  App ID: $appId"
Write-Host "  Installation ID: $installationId"
Write-Host "  PEM Hash: $($pemHash.Substring(0, 16))..."
Write-Host ""
Write-Host "WORKING ENVIRONMENT VALUES:" -ForegroundColor White
Write-Host "  App ID: ________"
Write-Host "  Installation ID: ________"
Write-Host "  PEM Hash: ________"
Write-Host ""
Write-Host "If ANY of these don't match, you've found the problem!" -ForegroundColor Yellow
Write-Host ""
