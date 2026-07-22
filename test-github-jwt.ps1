Write-Host "=== GitHub App JWT Token Test ===" -ForegroundColor Cyan

# Load configuration
$settingsPath = "DeploymentAPI\local.settings.json"
$settings = Get-Content $settingsPath | ConvertFrom-Json
$appId = $settings.Values.'GitHub:AppId'
$installationId = $settings.Values.'GitHub:InstallationId'
$pemPath = $settings.Values.'GitHub:PrivateKeyPath'
$owner = $settings.Values.'GitHub:Owner'
$repo = $settings.Values.'GitHub:Repository'

Write-Host "`nConfiguration:" -ForegroundColor Yellow
Write-Host "  App ID: $appId"
Write-Host "  Installation ID: $installationId"
Write-Host "  Owner: $owner"
Write-Host "  Repository: $repo"
Write-Host "  PEM Path: $pemPath"

# Step 1: Read and parse the PEM file
Write-Host "`n1. Reading PEM file..." -ForegroundColor Yellow
$pemContent = Get-Content $pemPath -Raw
$base64Key = $pemContent `
    -replace "-----BEGIN.*PRIVATE KEY-----", "" `
    -replace "-----END.*PRIVATE KEY-----", "" `
    -replace "`n", "" `
    -replace "`r", "" `
    -replace " ", ""

Write-Host "   ? PEM loaded ($($base64Key.Length) base64 chars)" -ForegroundColor Green

# Step 2: Try to generate JWT using .NET (simulating what the app does)
Write-Host "`n2. Generating JWT token..." -ForegroundColor Yellow
try {
    # Load required assemblies
    Add-Type -AssemblyName System.IdentityModel.Tokens.Jwt
    Add-Type -AssemblyName System.Security.Cryptography
    
    # Decode private key
    $keyBytes = [Convert]::FromBase64String($base64Key)
    Write-Host "   ? Decoded key ($($keyBytes.Length) bytes)" -ForegroundColor Green
    
    # Create RSA from key
    $rsa = [System.Security.Cryptography.RSA]::Create()
    $bytesRead = 0
    $rsa.ImportRSAPrivateKey($keyBytes, [ref]$bytesRead)
    Write-Host "   ? RSA key imported" -ForegroundColor Green
    
    # Create JWT
    $now = [DateTimeOffset]::UtcNow
    $expires = $now.AddMinutes(10)
    
    $signingCredentials = New-Object Microsoft.IdentityModel.Tokens.SigningCredentials(
        (New-Object Microsoft.IdentityModel.Tokens.RsaSecurityKey($rsa)),
        [Microsoft.IdentityModel.Tokens.SecurityAlgorithms]::RsaSha256
    )
    
    $jwt = New-Object System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        $appId,
        $null,
        $null,
        $now.UtcDateTime,
        $expires.UtcDateTime,
        $signingCredentials
    )
    
    $tokenHandler = New-Object System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler
    $jwtToken = $tokenHandler.WriteToken($jwt)
    
    Write-Host "   ? JWT token generated" -ForegroundColor Green
    Write-Host "   Token (first 50 chars): $($jwtToken.Substring(0, [Math]::Min(50, $jwtToken.Length)))..." -ForegroundColor Gray
    
    # Step 3: Test the JWT by getting an installation token
    Write-Host "`n3. Testing JWT with GitHub API..." -ForegroundColor Yellow
    
    $headers = @{
        "Authorization" = "Bearer $jwtToken"
        "Accept" = "application/vnd.github+json"
        "User-Agent" = "DeploymentAPI-Test"
        "X-GitHub-Api-Version" = "2022-11-28"
    }
    
    # First, verify the app itself
    Write-Host "`n   a) Verifying GitHub App..." -ForegroundColor Cyan
    try {
        $appResponse = Invoke-RestMethod -Uri "https://api.github.com/app" -Headers $headers -Method Get
        Write-Host "      ? App verified!" -ForegroundColor Green
        Write-Host "      App Name: $($appResponse.name)" -ForegroundColor Gray
        Write-Host "      App ID: $($appResponse.id)" -ForegroundColor Gray
        Write-Host "      Owner: $($appResponse.owner.login)" -ForegroundColor Gray
    } catch {
        Write-Host "      ? Failed to verify app" -ForegroundColor Red
        Write-Host "      Error: $($_.Exception.Message)" -ForegroundColor Red
        
        if ($_.ErrorDetails.Message) {
            $errorObj = $_.ErrorDetails.Message | ConvertFrom-Json
            Write-Host "      GitHub says: $($errorObj.message)" -ForegroundColor Yellow
        }
        exit 1
    }
    
    # Second, try to get installation token
    Write-Host "`n   b) Getting installation access token..." -ForegroundColor Cyan
    try {
        $tokenUrl = "https://api.github.com/app/installations/$installationId/access_tokens"
        $tokenResponse = Invoke-RestMethod -Uri $tokenUrl -Headers $headers -Method Post
        
        Write-Host "      ? Installation token obtained!" -ForegroundColor Green
        Write-Host "      Token (first 20 chars): $($tokenResponse.token.Substring(0, 20))..." -ForegroundColor Gray
        Write-Host "      Expires at: $($tokenResponse.expires_at)" -ForegroundColor Gray
        
        # Step 4: Test the installation token with actual API call
        Write-Host "`n4. Testing installation token with API..." -ForegroundColor Yellow
        
        $apiHeaders = @{
            "Authorization" = "Bearer $($tokenResponse.token)"
            "Accept" = "application/vnd.github+json"
            "User-Agent" = "DeploymentAPI-Test"
            "X-GitHub-Api-Version" = "2022-11-28"
        }
        
        # Try to get workflow runs
        Write-Host "`n   a) Fetching workflow runs..." -ForegroundColor Cyan
        try {
            $runsUrl = "https://api.github.com/repos/$owner/$repo/actions/runs?per_page=5"
            $runsResponse = Invoke-RestMethod -Uri $runsUrl -Headers $apiHeaders -Method Get
            
            Write-Host "      ? Successfully retrieved workflow runs!" -ForegroundColor Green
            Write-Host "      Total count: $($runsResponse.total_count)" -ForegroundColor Gray
            Write-Host "      Retrieved: $($runsResponse.workflow_runs.Count) runs" -ForegroundColor Gray
            
            if ($runsResponse.workflow_runs.Count -gt 0) {
                Write-Host "`n      Recent runs:" -ForegroundColor Gray
                $runsResponse.workflow_runs | Select-Object -First 3 | ForEach-Object {
                    Write-Host "      - $($_.name): $($_.status)/$($_.conclusion)" -ForegroundColor DarkGray
                }
            }
            
            Write-Host "`n=== ? SUCCESS! GitHub App authentication is working ===" -ForegroundColor Green
            Write-Host "`nThe issue must be in the Azure Functions runtime." -ForegroundColor Yellow
            Write-Host "Possible causes:" -ForegroundColor Yellow
            Write-Host "  1. Functions not restarted after config change" -ForegroundColor White
            Write-Host "  2. PEM file path not accessible from Functions process" -ForegroundColor White
            Write-Host "  3. Caching issue with configuration" -ForegroundColor White
            Write-Host "`nTry:" -ForegroundColor Cyan
            Write-Host "  1. Stop Functions (Ctrl+C in Functions window)" -ForegroundColor White
            Write-Host "  2. Run: .\restart-and-fix.ps1" -ForegroundColor White
            Write-Host "  3. Wait 15 seconds for full startup" -ForegroundColor White
            Write-Host "  4. Run: .\test-workflows.ps1" -ForegroundColor White
            
        } catch {
            Write-Host "      ? Failed to fetch workflow runs" -ForegroundColor Red
            Write-Host "      Error: $($_.Exception.Message)" -ForegroundColor Red
            
            if ($_.ErrorDetails.Message) {
                $errorObj = $_.ErrorDetails.Message | ConvertFrom-Json
                Write-Host "      GitHub says: $($errorObj.message)" -ForegroundColor Yellow
                
                if ($errorObj.message -like "*Resource not accessible*") {
                    Write-Host "`n      ? The GitHub App doesn't have access to this repository!" -ForegroundColor Yellow
                    Write-Host "      ? Install the app on the repository:" -ForegroundColor Cyan
                    Write-Host "         https://github.com/apps/YOUR-APP-NAME/installations/new" -ForegroundColor White
                }
            }
        }
        
    } catch {
        Write-Host "      ? Failed to get installation token" -ForegroundColor Red
        Write-Host "      Error: $($_.Exception.Message)" -ForegroundColor Red
        
        if ($_.ErrorDetails.Message) {
            $errorObj = $_.ErrorDetails.Message | ConvertFrom-Json
            Write-Host "      GitHub says: $($errorObj.message)" -ForegroundColor Yellow
            
            if ($errorObj.message -like "*installation*not found*" -or $errorObj.message -like "*404*") {
                Write-Host "`n      ? Installation ID $installationId not found!" -ForegroundColor Yellow
                Write-Host "      ? Check your installation ID at:" -ForegroundColor Cyan
                Write-Host "         https://github.com/settings/installations" -ForegroundColor White
                Write-Host "         (Click on the app, ID is in the URL)" -ForegroundColor Gray
            }
        }
    }
    
} catch {
    Write-Host "   ? Failed to generate JWT" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "`n   ? The private key format might be incorrect" -ForegroundColor Yellow
    Write-Host "   ? Regenerate the key from GitHub:" -ForegroundColor Cyan
    Write-Host "      1. Go to https://github.com/settings/apps" -ForegroundColor White
    Write-Host "      2. Select your app" -ForegroundColor White
    Write-Host "      3. Generate a new private key" -ForegroundColor White
    Write-Host "      4. Download and replace $pemPath" -ForegroundColor White
}
