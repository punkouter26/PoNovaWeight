// Azure App Service Module
// Deploys App Service Plan and Web App with managed identity

@description('App Service name')
param name string

@description('App Service Plan name')
param planName string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Storage connection string')
@secure()
param storageConnectionString string

@description('Google Client ID')
param googleClientId string = ''

@description('Google Client Secret')
@secure()
param googleClientSecret string = ''

@description('Azure OpenAI Endpoint')
param openAiEndpoint string = ''

@description('Azure OpenAI API Key')
@secure()
param openAiApiKey string = ''

@description('Azure OpenAI Deployment Name')
param openAiDeploymentName string = 'gpt-4o'

// App Service Plan (Standard tier for production)
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: planName
  location: location
  tags: tags
  sku: {
    name: 'S1'
    tier: 'Standard'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// Web App
resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ApplicationInsights__ConnectionString'
          value: appInsightsConnectionString
        }
        {
          name: 'ConnectionStrings__AzureStorage'
          value: storageConnectionString
        }
        {
          name: 'PoNovaWeight__AzureStorage__ConnectionString'
          value: storageConnectionString
        }
        {
          name: 'PoNovaWeight__Google__ClientId'
          value: !empty(googleClientId) ? googleClientId : ''
        }
        {
          name: 'PoNovaWeight__Google__ClientSecret'
          value: !empty(googleClientSecret) ? googleClientSecret : ''
        }
        {
          name: 'PoNovaWeight__AzureOpenAI__Endpoint'
          value: !empty(openAiEndpoint) ? openAiEndpoint : ''
        }
        {
          name: 'PoNovaWeight__AzureOpenAI__ApiKey'
          value: !empty(openAiApiKey) ? openAiApiKey : ''
        }
        {
          name: 'PoNovaWeight__AzureOpenAI__DeploymentName'
          value: openAiDeploymentName
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'true'
        }
      ]
    }
  }
}

// Enable HTTPS only
resource webAppHttps 'Microsoft.Web/sites/config@2023-01-01' = {
  parent: webApp
  name: 'web'
  properties: {
    httpsOnly: true
    minTlsVersion: '1.2'
  }
}

output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output webAppName string = webApp.name
output webAppPrincipalId string = webApp.identity.principalId
