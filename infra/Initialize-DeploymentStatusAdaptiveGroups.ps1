[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$KeyVaultName,
    [string]$AccessSecretName = 'deployment-status-access',
    [string]$SeedPath = (Join-Path $PSScriptRoot 'customer-access.example.json')
)

$ErrorActionPreference = 'Stop'
$accessJson = az keyvault secret show --vault-name $KeyVaultName --name $AccessSecretName --query value -o tsv 2>$null
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($accessJson)) {
    if (-not (Test-Path -LiteralPath $SeedPath)) { throw "Key Vault secret '$AccessSecretName' was not found and seed file '$SeedPath' is missing." }
    $access = Get-Content -LiteralPath $SeedPath -Raw | ConvertFrom-Json -AsHashtable
} else { $access = $accessJson | ConvertFrom-Json -AsHashtable }
$graphToken = az account get-access-token --resource-type ms-graph --query accessToken -o tsv
if ($LASTEXITCODE -ne 0 -or -not $graphToken) { throw 'Unable to acquire a Microsoft Graph access token.' }
$headers = @{ Authorization = "Bearer $graphToken" }
$currentUserId = az ad signed-in-user show --query id -o tsv
if ($LASTEXITCODE -ne 0 -or -not $currentUserId) { throw 'Unable to resolve the signed-in Entra user.' }

function Invoke-Graph([string]$Method, [string]$Path, [object]$Body) {
    $parameters = @{ Method = $Method; Uri = "https://graph.microsoft.com/v1.0/$Path"; Headers = $headers }
    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json'
        $parameters.Body = $Body | ConvertTo-Json -Depth 12 -Compress
    }
    Invoke-RestMethod @parameters
}

function Get-OrCreateGroup([string]$DisplayName, [string]$MailNickname, [string]$Description, [string]$MembershipRule) {
    $escaped = $DisplayName.Replace("'", "''")
    $existing = @((Invoke-Graph GET "groups?`$filter=displayName eq '$escaped'&`$select=id,displayName,membershipRule" $null).value) | Select-Object -First 1
    if ($existing) { return $existing }

    $body = [ordered]@{
        displayName = $DisplayName
        description = $Description
        mailEnabled = $false
        mailNickname = $MailNickname
        securityEnabled = $true
        'owners@odata.bind' = @("https://graph.microsoft.com/v1.0/users/$currentUserId")
    }
    if ($MembershipRule) {
        $body.groupTypes = @('DynamicMembership')
        $body.membershipRule = $MembershipRule
        $body.membershipRuleProcessingState = 'On'
    }
    Invoke-Graph POST 'groups' $body
}

$members = Get-OrCreateGroup 'DeploymentStatus - Adaptive Members' 'deploymentstatusadaptivemembers' 'All enabled internal Adaptive tenant members with DeploymentStatus Adaptive access.' '(user.userType -eq "Member") and (user.accountEnabled -eq true)'
$access.adaptiveGroupObjectIds = @([string]$members.id)
$value = $access | ConvertTo-Json -Depth 10 -Compress
az keyvault secret set --vault-name $KeyVaultName --name $AccessSecretName --value $value --output none
if ($LASTEXITCODE -ne 0) { throw "Unable to write Key Vault secret '$AccessSecretName'." }

Write-Host "Adaptive members group: $($members.id)"
Write-Host "Run Setup-Entra.ps1 -KeyVaultName $KeyVaultName next to assign DeploymentStatus.Adaptive.All to this group."
