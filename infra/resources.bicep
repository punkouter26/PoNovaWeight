@description('Name of the environment')
param environmentName string

@description('Primary location for all resources')
param location string = resourceGroup().location

// ── Storage Account (Table Storage) ──────────────────────────────
var storageAccountName = toLower(take(replace('st${environmentName}', '-', ''), 24))

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

// ── Existing App Service Plan in PoShared ────────────────────────
var appServicePlanId = resourceId('PoShared', 'Microsoft.Web/serverfarms', 'asp-poshared-linux')

// ── Web App ──────────────────────────────────────────────────────
var webAppName = 'ponovaweight-app'

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  tags: {
    'azd-service-name': 'app'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
    }
  }
}

// ── Outputs ──────────────────────────────────────────────────────
output AZURE_STORAGE_ACCOUNT_NAME string = storageAccount.name
output SERVICE_APP_NAME string = webApp.name
output WEB_APP_URL string = 'https://${webApp.properties.defaultHostName}'
