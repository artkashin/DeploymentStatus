[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ResourceGroup,
    [Parameter(Mandatory)][string]$FunctionAppName,
    [string]$KeyName = 'deploycd-reporter',
    [string]$DeployCdRepository
)

$ErrorActionPreference = 'Stop'
if (-not (Get-Command az -ErrorAction SilentlyContinue)) { throw 'Azure CLI is required.' }

az functionapp function keys set `
    --resource-group $ResourceGroup `
    --name $FunctionAppName `
    --function-name RegisterDeploymentEvent `
    --key-name $KeyName `
    --output none
$keys = az functionapp function keys list `
    --resource-group $ResourceGroup `
    --name $FunctionAppName `
    --function-name RegisterDeploymentEvent | ConvertFrom-Json
$key = $keys.PSObject.Properties[$KeyName].Value
if (-not $key) { throw 'Azure did not return the function key.' }

if ($DeployCdRepository) {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw 'GitHub CLI is required when DeployCdRepository is supplied.' }
    $key | gh secret set DEPLOYMENT_STATUS_API_KEY --repo $DeployCdRepository
    Write-Host "Updated DEPLOYMENT_STATUS_API_KEY in $DeployCdRepository."
}
else {
    Write-Warning 'The key was created but not printed. Supply -DeployCdRepository or store it from Azure securely.'
}
