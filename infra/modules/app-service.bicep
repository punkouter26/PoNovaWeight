// Nova Food Journal - App Service Module
// Deploys App Service Plan and Web App for Blazor WASM hosted application

@description('Web App name')
param name string

@description('App Service Plan name')
param planName string

@description('Azure region')
param location string

@description('App Service SKU')
@allowed(['F1', 'B1', 'B2'])
param sku string = 'B1'

@description('Resource tags')
param tags object = {}

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Application Insights instrumentation key')
param appInsightsInstrumentationKey string

@description('Storage account connection string')
@secure()
param storageConnectionString string

@description('Application passcode')
@secure()
param appPasscode string

@description('Azure OpenAI endpoint')
param openAiEndpoint string = ''

@description('Azure OpenAI API key')
@secure()
param openAiApiKey string = ''

@description('Azure OpenAI deployment name')
param openAiDeploymentName string = 'gpt-4o'

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  tags: tags
  sku: {
    name: sku
    tier: sku == 'F1' ? 'Free' : 'Basic'
    size: sku
    family: sku == 'F1' ? 'F' : 'B'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true // Required for Linux
  }
}

// Web App
resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/api/health'
      alwaysOn: sku != 'F1' // Not available on Free tier
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsights__InstrumentationKey'
          value: appInsightsInstrumentationKey
        }
        {
          name: 'TableStorage__ConnectionString'
          value: storageConnectionString
        }
        {
          name: 'Auth__Passcode'
          value: appPasscode
        }
        {
          name: 'Auth__SessionTimeoutMinutes'
          value: '480'
        }
        {
          name: 'AzureOpenAI__Endpoint'
          value: openAiEndpoint
        }
        {
          name: 'AzureOpenAI__ApiKey'
          value: openAiApiKey
        }
        {
          name: 'AzureOpenAI__DeploymentName'
          value: openAiDeploymentName
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

// Configure logging
resource webAppLogs 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: webApp
  name: 'logs'
  properties: {
    applicationLogs: {
      fileSystem: {
        level: 'Information'
      }
    }
    httpLogs: {
      fileSystem: {
        enabled: true
        retentionInDays: 7
        retentionInMb: 35
      }
    }
    detailedErrorMessages: {
      enabled: true
    }
    failedRequestsTracing: {
      enabled: true
    }
  }
}

// Outputs
output id string = webApp.id
output name string = webApp.name
output url string = 'https://${webApp.properties.defaultHostName}'
output planId string = appServicePlan.id
