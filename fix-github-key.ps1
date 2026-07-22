Write-Host "=== Quick Fix: Update GitHub App Private Key ===" -ForegroundColor Cyan
Write-Host ""

$pemPath = "DeploymentAPI\.security\github-app-private-key.pem"

Write-Host "This script will help you replace the GitHub App private key." -ForegroundColor Yellow
Write-Host ""
Write-Host "Steps:" -ForegroundColor White
Write-Host "  1. Generate a new private key from GitHub" -ForegroundColor Gray
Write-Host "  2. Download the .pem file" -ForegroundColor Gray
Write-Host "  3. This script will copy it to the correct location" -ForegroundColor Gray
Write-Host "  4. Restart Functions" -ForegroundColor Gray
Write-Host ""

# Step 1: Guide user to generate key
Write-Host "Step 1: Generate new private key" -ForegroundColor Cyan
Write-Host "  ? Opening GitHub App settings in your browser..." -ForegroundColor Yellow
Start-Process "https://github.com/settings/apps"
Write-Host ""
Write-Host "  In the browser:" -ForegroundColor White
Write-Host "    1. Click on your GitHub App" -ForegroundColor Gray
Write-Host "    2. Scroll down to 'Private keys' section" -ForegroundColor Gray
Write-Host "    3. Click 'Generate a private key'" -ForegroundColor Gray
Write-Host "    4. Download the .pem file" -ForegroundColor Gray
Write-Host "    5. Remember where you saved it (usually Downloads folder)" -ForegroundColor Gray
Write-Host ""

# Step 2: Wait for user to download
Write-Host "Step 2: Locate the downloaded key" -ForegroundColor Cyan
$downloadsPath = [Environment]::GetFolderPath("UserProfile") + "\Downloads"
Write-Host "  ? Your Downloads folder: $downloadsPath" -ForegroundColor Yellow
Write-Host ""

# Check if there's a recent .pem file in Downloads
$recentPems = Get-ChildItem -Path $downloadsPath -Filter "*.pem" -ErrorAction SilentlyContinue | 
              Where-Object { $_.LastWriteTime -gt (Get-Date).AddHours(-1) } |
              Sort-Object LastWriteTime -Descending

if ($recentPems) {
    Write-Host "  ? Found recent .pem files in Downloads:" -ForegroundColor Green
    $recentPems | ForEach-Object {
        $age = ((Get-Date) - $_.LastWriteTime).TotalMinutes
        Write-Host "    - $($_.Name) (downloaded $([math]::Round($age)) minutes ago)" -ForegroundColor Gray
    }
    Write-Host ""
    
    $mostRecent = $recentPems[0]
    Write-Host "  ? Most recent file: $($mostRecent.Name)" -ForegroundColor Cyan
    Write-Host ""
    
    $response = Read-Host "  Use this file? (Y/n)"
    if ($response -eq "" -or $response -eq "Y" -or $response -eq "y") {
        $sourceFile = $mostRecent.FullName
    } else {
        $sourceFile = $null
    }
} else {
    Write-Host "  ? No recent .pem files found in Downloads" -ForegroundColor Yellow
    $sourceFile = $null
}

# Step 3: Get file path if not auto-detected
if (-not $sourceFile) {
    Write-Host ""
    Write-Host "  Enter the full path to your downloaded .pem file:" -ForegroundColor White
    Write-Host "  (or drag and drop the file here)" -ForegroundColor Gray
    $sourceFile = Read-Host "  Path"
    $sourceFile = $sourceFile.Trim('"').Trim("'")
}

# Step 4: Validate and copy
Write-Host ""
Write-Host "Step 3: Copying key file" -ForegroundColor Cyan

if (-not (Test-Path $sourceFile)) {
    Write-Host "  ? File not found: $sourceFile" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Please check the path and try again." -ForegroundColor Yellow
    exit 1
}

# Validate it's a PEM file
$content = Get-Content $sourceFile -Raw
if ($content -notmatch "-----BEGIN.*PRIVATE KEY-----") {
    Write-Host "  ? File doesn't appear to be a valid PEM private key" -ForegroundColor Red
    Write-Host "  First line: $($content.Split("`n")[0])" -ForegroundColor Gray
    exit 1
}

Write-Host "  ? Valid PEM file found" -ForegroundColor Green

# Ensure directory exists
$securityDir = Split-Path -Parent $pemPath
if (-not (Test-Path $securityDir)) {
    New-Item -ItemType Directory -Path $securityDir -Force | Out-Null
    Write-Host "  ? Created .security directory" -ForegroundColor Green
}

# Backup existing file if it exists
if (Test-Path $pemPath) {
    $backupPath = "$pemPath.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    Copy-Item $pemPath $backupPath
    Write-Host "  ? Backed up existing key to: $backupPath" -ForegroundColor Green
}

# Copy new file
Copy-Item $sourceFile $pemPath -Force
Write-Host "  ? Copied new key to: $pemPath" -ForegroundColor Green

# Step 5: Restart Functions
Write-Host ""
Write-Host "Step 4: Restarting Azure Functions" -ForegroundColor Cyan

# Stop existing Functions
$functionsProcess = Get-Process -Name "func" -ErrorAction SilentlyContinue
if ($functionsProcess) {
    Write-Host "  ? Stopping existing Functions process..." -ForegroundColor Yellow
    Stop-Process -Name "func" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Write-Host "  ? Functions stopped" -ForegroundColor Green
}

# Start Functions
Write-Host "  ? Starting Functions..." -ForegroundColor Yellow
Start-Process -FilePath "func" -ArgumentList "start" -WorkingDirectory "DeploymentAPI" -WindowStyle Normal
Start-Sleep -Seconds 3

Write-Host "  ? Functions starting in new window" -ForegroundColor Green

# Step 6: Wait and test
Write-Host ""
Write-Host "Step 5: Testing" -ForegroundColor Cyan
Write-Host "  ? Waiting 15 seconds for Functions to fully start..." -ForegroundColor Yellow

for ($i = 15; $i -gt 0; $i--) {
    Write-Host "  $i..." -NoNewline -ForegroundColor Gray
    Start-Sleep -Seconds 1
    if ($i -gt 1) { Write-Host "`r" -NoNewline }
}
Write-Host ""
Write-Host ""

Write-Host "  ? Testing GitHub API..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:7071/api/github/actions" -Method Get -ErrorAction Stop
    
    if ($response.error) {
        Write-Host "  ? Still getting error from Functions" -ForegroundColor Red
        Write-Host "  Error: $($response.message)" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "  Additional steps to try:" -ForegroundColor Cyan
        Write-Host "    1. Verify App ID and Installation ID in local.settings.json" -ForegroundColor White
        Write-Host "    2. Check GitHub App permissions" -ForegroundColor White
        Write-Host "    3. Ensure app is installed on the repository" -ForegroundColor White
        Write-Host "    4. See GITHUB-AUTH-TROUBLESHOOTING.md for details" -ForegroundColor White
    } else {
        Write-Host "  ? SUCCESS! GitHub API is working!" -ForegroundColor Green
        Write-Host "  ? Retrieved $($response.Count) workflow runs" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "=== Problem Fixed! ===" -ForegroundColor Green
        Write-Host ""
        Write-Host "You can now run: .\test-workflows.ps1" -ForegroundColor Cyan
    }
} catch {
    Write-Host "  ? Functions not responding yet" -ForegroundColor Yellow
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Functions may need more time to start." -ForegroundColor Yellow
    Write-Host "  Wait another 10 seconds and run: .\test-workflows.ps1" -ForegroundColor Cyan
}

Write-Host ""
