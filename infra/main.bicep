targetScope = 'resourceGroup'

@description('Azure region for the production resources.')
param location string = resourceGroup().location
@description('Globally unique Function App name.')
param functionAppName string = 'func-deployment-status-api'
@description('Existing Flex Consumption plan hosting the Function App during cutover.')
param functionPlanName string = 'ASP-rgdeploymentstatus-ac50'
@description('Globally unique Static Web App name.')
param staticWebAppName string = 'swa-deployment-status'
@description('Microsoft Entra tenant that issues dashboard tokens.')
param entraTenantId string
@description('Application client ID of the DeploymentStatus API registration.')
param entraApiClientId string
@description('Additional allowed local CORS origins.')
param localOrigins array = [
  'http://localhost:5173'
  'http://127.0.0.1:5173'
]
@description('Additional production dashboard origins, retained alongside the Static Web App default hostname.')
param dashboardOrigins array = [
  'https://deployments.adaptivenav.com'
]

var token = uniqueString(subscription().id, resourceGroup().id)
var storageName = 'stds${token}'
var insightsName = 'appi-deployment-status-${token}'
var deploymentContainerName = 'function-releases'
var blobContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
var queueContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
var tableContributorRole = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
}
resource deploymentContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: deploymentContainerName
  properties: { publicAccess: 'None' }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: insightsName
  location: location
  kind: 'web'
  properties: { Application_Type: 'web' }
}

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: location
  sku: { name: 'Standard', tier: 'Standard' }
  properties: { allowConfigFileUpdates: true }
}

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: functionPlanName
  location: location
  kind: 'functionapp'
  sku: { tier: 'FlexConsumption', name: 'FC1' }
  properties: { reserved: true }
}

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    publicNetworkAccess: 'Enabled'
    siteConfig: {
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      cors: {
        allowedOrigins: union([ 'https://${staticWebApp.properties.defaultHostname}' ], dashboardOrigins, localOrigins)
        supportCredentials: false
      }
      appSettings: [
        { name: 'AzureWebJobsStorage__accountName', value: storage.name }
        { name: 'Storage__ServiceUri', value: storage.properties.primaryEndpoints.table }
        { name: 'Storage__UseInMemory', value: 'false' }
        { name: 'Authorization__AllowDevelopmentHeaders', value: 'false' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: insights.properties.ConnectionString }
      ]
    }
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storage.properties.primaryEndpoints.blob}${deploymentContainerName}'
          authentication: { type: 'SystemAssignedIdentity' }
        }
      }
      scaleAndConcurrency: { maximumInstanceCount: 20, instanceMemoryMB: 2048 }
      runtime: { name: 'dotnet-isolated', version: '10.0' }
    }
  }
}

resource auth 'Microsoft.Web/sites/config@2024-04-01' = {
  parent: functionApp
  name: 'authsettingsV2'
  properties: {
    platform: { enabled: true, runtimeVersion: '~1' }
    globalValidation: {
      requireAuthentication: true
      unauthenticatedClientAction: 'Return401'
      excludedPaths: [ '/api/v1/deployment-events', '/api/v1/artifact-sources' ]
    }
    httpSettings: { requireHttps: true }
    identityProviders: {
      azureActiveDirectory: {
        enabled: true
        registration: {
          clientId: entraApiClientId
          openIdIssuer: '${environment().authentication.loginEndpoint}${entraTenantId}/v2.0'
        }
        validation: { allowedAudiences: [ entraApiClientId, 'api://${entraApiClientId}' ] }
      }
    }
  }
}

resource blobRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, functionApp.id, blobContributorRole)
  scope: storage
  properties: { roleDefinitionId: blobContributorRole, principalId: functionApp.identity.principalId, principalType: 'ServicePrincipal' }
}
resource queueRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, functionApp.id, queueContributorRole)
  scope: storage
  properties: { roleDefinitionId: queueContributorRole, principalId: functionApp.identity.principalId, principalType: 'ServicePrincipal' }
}
resource tableRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, functionApp.id, tableContributorRole)
  scope: storage
  properties: { roleDefinitionId: tableContributorRole, principalId: functionApp.identity.principalId, principalType: 'ServicePrincipal' }
}

output functionAppName string = functionApp.name
output functionApiUrl string = 'https://${functionApp.properties.defaultHostName}'
output staticWebAppUrl string = 'https://${staticWebApp.properties.defaultHostname}'
output storageAccountName string = storage.name
