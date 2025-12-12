// Nova Food Journal - Main Bicep Orchestration
// Deploys all Azure resources for the PoNovaWeight application

targetScope = 'resourceGroup'

// Parameters
@description('Environment name (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environmentName string = 'dev'

@description('Azure region for resources')
param location string = resourceGroup().location

@description('Base name for resources')
param baseName string = 'ponova'

@description('App Service SKU')
@allowed(['F1', 'B1', 'B2'])
param appServiceSku string = 'B1'

@description('Budget limit in USD')
param budgetAmount int = 5

@description('Budget alert email')
param budgetAlertEmail string = 'punkouter26@gmail.com'

@description('Application passcode (stored in Key Vault in production)')
@secure()
param appPasscode string = '1234'

@description('Azure OpenAI endpoint URL')
param openAiEndpoint string = ''

@description('Azure OpenAI API key')
@secure()
param openAiApiKey string = ''

@description('Azure OpenAI deployment name')
param openAiDeploymentName string = 'gpt-4o'

// Variables
var resourcePrefix = '${baseName}-${environmentName}'
var tags = {
  Environment: environmentName
  Application: 'PoNovaWeight'
  ManagedBy: 'Bicep'
}

// Storage Account
module storage 'modules/storage.bicep' = {
  name: 'storage-deployment'
  params: {
    name: replace('${resourcePrefix}stor', '-', '')
    location: location
    tags: tags
  }
}

// Application Insights
module appInsights 'modules/app-insights.bicep' = {
  name: 'app-insights-deployment'
  params: {
    name: '${resourcePrefix}-ai'
    location: location
    tags: tags
  }
}

// App Service Plan and Web App
module appService 'modules/app-service.bicep' = {
  name: 'app-service-deployment'
  params: {
    name: '${resourcePrefix}-app'
    planName: '${resourcePrefix}-plan'
    location: location
    sku: appServiceSku
    tags: tags
    appInsightsConnectionString: appInsights.outputs.connectionString
    appInsightsInstrumentationKey: appInsights.outputs.instrumentationKey
    storageConnectionString: storage.outputs.connectionString
    appPasscode: appPasscode
    openAiEndpoint: openAiEndpoint
    openAiApiKey: openAiApiKey
    openAiDeploymentName: openAiDeploymentName
  }
}

// Budget with alerts
module budget 'modules/budget.bicep' = {
  name: 'budget-deployment'
  params: {
    name: '${resourcePrefix}-budget'
    amount: budgetAmount
    alertEmail: budgetAlertEmail
    startDate: '${substring(utcNow(), 0, 7)}-01' // First of current month
  }
}

// Outputs
output appServiceUrl string = appService.outputs.url
output appServiceName string = appService.outputs.name
output storageAccountName string = storage.outputs.name
output appInsightsName string = appInsights.outputs.name
output resourceGroupName string = resourceGroup().name
