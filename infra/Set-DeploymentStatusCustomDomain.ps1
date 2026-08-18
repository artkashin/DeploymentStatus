[CmdletBinding()]
param(
    [string]$SubscriptionId = 'bed0c1d9-82b9-44db-a62e-8c8e912e4825',
    [string]$ResourceGroup = 'rg-deployment-status',
    [string]$StaticWebAppName = 'swa-deployment-status',
    [string]$FunctionAppName = 'func-deployment-status-api',
    [string]$Hostname = 'deployments.adaptivenav.com',
    [string]$StaticWebAppHostname = 'orange-island-09ab4780f.7.azurestaticapps.net',
    [Parameter(Mandatory)][string]$KeyVaultName,
    [string]$EntraSetupScript = (Join-Path $PSScriptRoot 'Setup-Entra.ps1')
)

$ErrorActionPreference = 'Stop'
$expectedTarget = $StaticWebAppHostname.TrimEnd('.').ToLowerInvariant()
$cname = @(Resolve-DnsName -Name $Hostname -Type CNAME -ErrorAction Stop | Where-Object Type -eq 'CNAME' | Select-Object -First 1)
if (-not $cname -or $cname[0].NameHost.TrimEnd('.').ToLowerInvariant() -ne $expectedTarget) {
    throw "DNS is not ready. Configure CNAME $Hostname -> $StaticWebAppHostname and retry after it resolves publicly."
}

az staticwebapp hostname set --subscription $SubscriptionId --resource-group $ResourceGroup --name $StaticWebAppName --hostname $Hostname --validation-method cname-delegation
if ($LASTEXITCODE -ne 0) { throw "Azure Static Web Apps rejected custom hostname $Hostname." }

az functionapp cors add --subscription $SubscriptionId --resource-group $ResourceGroup --name $FunctionAppName --allowed-origins "https://$Hostname"
if ($LASTEXITCODE -ne 0) { throw "Unable to add https://$Hostname to Function App CORS." }

& $EntraSetupScript -KeyVaultName $KeyVaultName -StaticWebAppUrls @("https://$StaticWebAppHostname", "https://$Hostname")
if ($LASTEXITCODE -ne 0) { throw 'Unable to update Entra SPA redirect URIs and role assignments.' }

az staticwebapp hostname show --subscription $SubscriptionId --resource-group $ResourceGroup --name $StaticWebAppName --hostname $Hostname -o json
