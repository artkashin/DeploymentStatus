Write-Host "=== GitHub App Authentication Diagnostics ===" -ForegroundColor Cyan

# 1. Check PEM file
$pemPath = "C:\Users\ArtemKashin\source\repos\DeplomentStatus\DeploymentAPI\.security\github-app-private-key.pem"
Write-Host "`n1. Checking PEM file..." -ForegroundColor Yellow
if (Test-Path $pemPath) {
    Write-Host "   ? PEM file exists: $pemPath" -ForegroundColor Green
    $content = Get-Content $pemPath -Raw
    $lines = ($content -split "`n").Count
    Write-Host "   ? File has $lines lines" -ForegroundColor Green
    
    if ($content -match "-----BEGIN.*PRIVATE KEY-----") {
        Write-Host "   ? PEM header found" -ForegroundColor Green
    } else {
        Write-Host "   ? PEM header missing!" -ForegroundColor Red
    }
    
    if ($content -match "-----END.*PRIVATE KEY-----") {
        Write-Host "   ? PEM footer found" -ForegroundColor Green
    } else {
        Write-Host "   ? PEM footer missing!" -ForegroundColor Red
    }
} else {
    Write-Host "   ? PEM file NOT found: $pemPath" -ForegroundColor Red
    exit 1
}

# 2. Check local.settings.json
Write-Host "`n2. Checking local.settings.json..." -ForegroundColor Yellow
$settingsPath = "DeploymentAPI\local.settings.json"
if (Test-Path $settingsPath) {
    $settings = Get-Content $settingsPath | ConvertFrom-Json
    $appId = $settings.Values.'GitHub:AppId'
    $installationId = $settings.Values.'GitHub:InstallationId'
    $authType = $settings.Values.'GitHub:AuthType'
    $keySource = $settings.Values.'GitHub:PrivateKeySource'
    $keyPath = $settings.Values.'GitHub:PrivateKeyPath'
    
    Write-Host "   ? Auth Type: $authType" -ForegroundColor Green
    Write-Host "   ? App ID: $appId" -ForegroundColor Green
    Write-Host "   ? Installation ID: $installationId" -ForegroundColor Green
    Write-Host "   ? Key Source: $keySource" -ForegroundColor Green
    Write-Host "   ? Key Path: $keyPath" -ForegroundColor Green
} else {
    Write-Host "   ? local.settings.json NOT found" -ForegroundColor Red
    exit 1
}

# 3. Test JWT generation (simplified)
Write-Host "`n3. Testing JWT Token generation..." -ForegroundColor Yellow
try {
    # Read the PEM file
    $pemContent = Get-Content $pemPath -Raw
    
    # Extract the base64 content
    $base64Key = $pemContent `
        -replace "-----BEGIN.*PRIVATE KEY-----", "" `
        -replace "-----END.*PRIVATE KEY-----", "" `
        -replace "`n", "" `
        -replace "`r", "" `
        -replace " ", ""
    
    Write-Host "   ? PEM content extracted (length: $($base64Key.Length) chars)" -ForegroundColor Green
    
    # Try to decode base64
    try {
        $keyBytes = [Convert]::FromBase64String($base64Key)
        Write-Host "   ? Base64 decoded successfully ($($keyBytes.Length) bytes)" -ForegroundColor Green
    } catch {
        Write-Host "   ? Failed to decode base64: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "   First 50 chars of base64: $($base64Key.Substring(0, [Math]::Min(50, $base64Key.Length)))" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "   ? Error processing PEM: $($_.Exception.Message)" -ForegroundColor Red
}

# 4. Test GitHub API with App ID
Write-Host "`n4. Testing GitHub App API access..." -ForegroundColor Yellow
try {
    # First, let's verify the App ID is correct by checking the app endpoint
    $headers = @{
        "Accept" = "application/vnd.github+json"
        "User-Agent" = "DeploymentAPI-Diagnostic"
        "X-GitHub-Api-Version" = "2022-11-28"
    }
    
    # This endpoint doesn't require authentication
    $appUrl = "https://api.github.com/app"
    Write-Host "   Testing endpoint: $appUrl" -ForegroundColor Gray
    
    # Note: Without JWT, this will return 401 but with a helpful message
    try {
        $response = Invoke-WebRequest -Uri $appUrl -Headers $headers -Method Get
        Write-Host "   ? Unexpected success (should require auth)" -ForegroundColor Yellow
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 401) {
            Write-Host "   ? API endpoint responding (401 expected without auth)" -ForegroundColor Green
        } else {
            Write-Host "   ? Unexpected status code: $statusCode" -ForegroundColor Red
        }
    }
    
} catch {
    Write-Host "   ? Error: $($_.Exception.Message)" -ForegroundColor Red
}

# 5. Test local Functions endpoint
Write-Host "`n5. Testing local Functions API..." -ForegroundColor Yellow
try {
    $functionsUrl = "http://localhost:7071/api/GetGitHubWorkflows"
    Write-Host "   Testing endpoint: $functionsUrl" -ForegroundColor Gray
    
    $response = Invoke-WebRequest -Uri $functionsUrl -Method Get -ErrorAction Stop
    Write-Host "   ? Functions responding (Status: $($response.StatusCode))" -ForegroundColor Green
    
    # Parse and display error details
    $content = $response.Content | ConvertFrom-Json
    Write-Host "   Response: $($content | ConvertTo-Json -Depth 2)" -ForegroundColor Gray
    
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-Host "   ? Functions error (Status: $statusCode)" -ForegroundColor Red
    
    if ($statusCode -eq 401) {
        Write-Host "   ? This indicates GitHub authentication is failing" -ForegroundColor Yellow
        Write-Host "   ? Check the Function host console for detailed error messages" -ForegroundColor Yellow
    }
    
    # Try to get error details
    try {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        $reader.Close()
        Write-Host "`n   Error details:" -ForegroundColor Yellow
        Write-Host "   $errorBody" -ForegroundColor Gray
    } catch {
        Write-Host "   Could not read error details" -ForegroundColor Gray
    }
}

# 6. Suggestions
Write-Host "`n=== Recommendations ===" -ForegroundColor Cyan
Write-Host "1. Check the Azure Functions console window for detailed error logs" -ForegroundColor White
Write-Host "2. Verify your GitHub App settings at: https://github.com/settings/apps" -ForegroundColor White
Write-Host "3. Confirm the App ID ($appId) and Installation ID ($installationId) are correct" -ForegroundColor White
Write-Host "4. Ensure the private key matches the one configured in your GitHub App" -ForegroundColor White
Write-Host "5. Check that the GitHub App has the necessary permissions installed" -ForegroundColor White
Write-Host "`nTo regenerate the private key:" -ForegroundColor Yellow
Write-Host "  1. Go to https://github.com/settings/apps" -ForegroundColor Gray
Write-Host "  2. Select your app" -ForegroundColor Gray
Write-Host "  3. Scroll to 'Private keys'" -ForegroundColor Gray
Write-Host "  4. Generate a new key and download it" -ForegroundColor Gray
Write-Host "  5. Copy it to: $pemPath" -ForegroundColor Gray
Write-Host "  6. Run: .\restart-and-fix.ps1" -ForegroundColor Gray
