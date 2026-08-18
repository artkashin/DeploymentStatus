[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$KeyVaultName,
    [string]$SecretName = 'deployment-status-access',
    [string]$Path = (Join-Path $PSScriptRoot 'customer-access.example.json')
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $Path)) { throw "Access mapping file '$Path' was not found." }
$mapping = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -AsHashtable
if (-not $mapping.ContainsKey('adaptiveGroupObjectIds') -or -not $mapping.ContainsKey('customers')) {
    throw 'The access mapping must contain adaptiveGroupObjectIds and customers.'
}
$value = $mapping | ConvertTo-Json -Depth 12 -Compress
az keyvault secret set --vault-name $KeyVaultName --name $SecretName --value $value --output none
if ($LASTEXITCODE -ne 0) { throw "Unable to write Key Vault secret '$SecretName'." }
Write-Host "Stored DeploymentStatus access mapping in Key Vault '$KeyVaultName' as '$SecretName'."
