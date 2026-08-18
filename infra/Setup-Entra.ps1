[CmdletBinding()]
param(
    [string]$ApiDisplayName = 'DeploymentStatus API',
    [string]$SpaDisplayName = 'DeploymentStatus Dashboard',
    [Alias('StaticWebAppUrl')]
    [string[]]$StaticWebAppUrls = @(),
    [Parameter(Mandatory)][string]$KeyVaultName,
    [string]$AccessSecretName = 'deployment-status-access'
)

$ErrorActionPreference = 'Stop'
if (-not (Get-Command az -ErrorAction SilentlyContinue)) { throw 'Azure CLI is required.' }
$accessJson = az keyvault secret show --vault-name $KeyVaultName --name $AccessSecretName --query value -o tsv
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($accessJson)) { throw "Unable to read Key Vault secret '$AccessSecretName' from '$KeyVaultName'." }
$access = $accessJson | ConvertFrom-Json -AsHashtable
$graphToken = az account get-access-token --resource-type ms-graph --query accessToken -o tsv
if ($LASTEXITCODE -ne 0 -or -not $graphToken) { throw 'Unable to acquire a Microsoft Graph access token.' }
$graphHeaders = @{ Authorization = "Bearer $graphToken" }

function Invoke-Graph([string]$Method, [string]$Path, [object]$Body) {
    $parameters = @{ Method = $Method; Uri = "https://graph.microsoft.com/v1.0/$Path"; Headers = $graphHeaders }
    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json'
        $parameters.Body = $Body | ConvertTo-Json -Depth 12 -Compress
    }
    Invoke-RestMethod @parameters
}

function Get-OrCreateApp([string]$DisplayName) {
    $escaped = $DisplayName.Replace("'", "''")
    $existing = @(az ad app list --filter "displayName eq '$escaped'" | ConvertFrom-Json) | Select-Object -First 1
    if ($existing) { return $existing }
    return az ad app create --display-name $DisplayName --sign-in-audience AzureADMyOrg | ConvertFrom-Json
}

function Get-OrCreateRole([object[]]$Existing, [string]$Value, [string]$DisplayName, [string]$Description) {
    $role = @($Existing | Where-Object { $_.value -eq $Value }) | Select-Object -First 1
    if ($role) { return $role }
    return [ordered]@{ id = [guid]::NewGuid(); allowedMemberTypes = @('User'); description = $Description; displayName = $DisplayName; isEnabled = $true; value = $Value }
}

function Set-GroupRole([string]$GroupId, [string]$ServicePrincipalId, [string]$RoleId) {
    if (-not $GroupId) { return }
    $assignments = @(Invoke-Graph GET "groups/$GroupId/appRoleAssignments" $null).value
    if ($assignments | Where-Object { $_.resourceId -eq $ServicePrincipalId -and $_.appRoleId -eq $RoleId }) { return }
    Invoke-Graph POST "groups/$GroupId/appRoleAssignments" @{ principalId = $GroupId; resourceId = $ServicePrincipalId; appRoleId = $RoleId } | Out-Null
}

$apiApp = Get-OrCreateApp $ApiDisplayName
$apiApp = Invoke-Graph GET "applications/$($apiApp.id)" $null
$roles = [System.Collections.Generic.List[object]]::new()
foreach ($role in @($apiApp.appRoles)) { [void]$roles.Add($role) }
$adaptiveRole = Get-OrCreateRole @($roles) 'DeploymentStatus.Adaptive.All' 'Adaptive: all customers' 'Read all DeploymentStatus customers and internal diagnostics.'
if (-not ($roles | Where-Object value -eq $adaptiveRole.value)) { [void]$roles.Add($adaptiveRole) }
$customerRoles = @{}
foreach ($customerId in @($access.customers.Keys | Sort-Object)) {
    $value = "DeploymentStatus.Customer.$customerId"
    $role = Get-OrCreateRole @($roles) $value "Customer: $customerId" "Read customer-safe DeploymentStatus data for $customerId."
    if (-not ($roles | Where-Object value -eq $role.value)) { [void]$roles.Add($role) }
    $customerRoles[$customerId] = $role
}
$scope = @($apiApp.api.oauth2PermissionScopes | Where-Object value -eq 'Deployment.Read') | Select-Object -First 1
if (-not $scope) { $scope = [ordered]@{ id = [guid]::NewGuid(); adminConsentDescription = 'Read authorized deployment status data.'; adminConsentDisplayName = 'Read deployment status'; isEnabled = $true; type = 'User'; userConsentDescription = 'Read deployment status assigned to your account.'; userConsentDisplayName = 'Read deployment status'; value = 'Deployment.Read' } }
$apiBody = @{ identifierUris = @("api://$($apiApp.appId)"); appRoles = @($roles); api = @{ oauth2PermissionScopes = @($scope); requestedAccessTokenVersion = 2 } }
Invoke-Graph PATCH "applications/$($apiApp.id)" $apiBody | Out-Null

$apiSp = @(az ad sp list --filter "appId eq '$($apiApp.appId)'" | ConvertFrom-Json) | Select-Object -First 1
if (-not $apiSp) { $apiSp = az ad sp create --id $apiApp.appId | ConvertFrom-Json }
$adaptiveGroupIds = @($access.adaptiveGroupObjectIds)
if (-not $adaptiveGroupIds -and $access.adaptiveGroupObjectId) { $adaptiveGroupIds = @([string]$access.adaptiveGroupObjectId) }
if (-not $adaptiveGroupIds) { Write-Warning 'No Adaptive access groups are configured. Run Initialize-DeploymentStatusAdaptiveGroups.ps1 first.' }
foreach ($groupId in $adaptiveGroupIds) { Set-GroupRole ([string]$groupId) $apiSp.id ([string]$adaptiveRole.id) }
foreach ($customerId in $customerRoles.Keys) {
    $groupId = [string]$access.customers[$customerId]
    if ($groupId) { Set-GroupRole $groupId $apiSp.id ([string]$customerRoles[$customerId].id) }
    else { Write-Warning "No Entra group is configured for customer '$customerId'." }
}

$spaApp = Get-OrCreateApp $SpaDisplayName
$spaSp = @(az ad sp list --filter "appId eq '$($spaApp.appId)'" | ConvertFrom-Json) | Select-Object -First 1
if (-not $spaSp) { $spaSp = az ad sp create --id $spaApp.appId | ConvertFrom-Json }
$redirectUris = [System.Collections.Generic.List[string]]::new()
[void]$redirectUris.Add('http://localhost:5173')
foreach ($staticWebAppUrl in $StaticWebAppUrls) {
    if (-not [string]::IsNullOrWhiteSpace($staticWebAppUrl)) { [void]$redirectUris.Add($staticWebAppUrl.TrimEnd('/')) }
}
$spaBody = @{
    spa = @{ redirectUris = @($redirectUris) }
    requiredResourceAccess = @(@{ resourceAppId = $apiApp.appId; resourceAccess = @(@{ id = $scope.id; type = 'Scope' }) })
}
Invoke-Graph PATCH "applications/$($spaApp.id)" $spaBody | Out-Null

Write-Host "DeploymentStatus API client ID: $($apiApp.appId)"
Write-Host "DeploymentStatus SPA client ID: $($spaApp.appId)"
if (-not $StaticWebAppUrls) { Write-Warning 'Run this script again with -StaticWebAppUrls after the Static Web App is provisioned.' }
Write-Host 'An Entra administrator must grant tenant-wide consent for the SPA Deployment.Read permission.'
