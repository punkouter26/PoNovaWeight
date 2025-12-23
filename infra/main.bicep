// Nova Food Journal - Main Bicep Orchestration
// Deploys all Azure resources for the PoNovaWeight application (Azure Container Apps)

targetScope = 'resourceGroup'

// Parameters
@description('Environment name (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environmentName string = 'prod'

@description('Azure region for resources')
param location string = resourceGroup().location

@description('Base name for resources')
param baseName string = 'ponovaweight'

@description('Budget limit in USD')
param budgetAmount int = 5

@description('Budget alert email')
param budgetAlertEmail string = 'punkouter26@gmail.com'

@description('Google Client ID')
param googleClientId string = ''

@description('Google Client Secret')
@secure()
param googleClientSecret string = ''

@description('Azure OpenAI endpoint URL')
param openAiEndpoint string = ''

@description('Azure OpenAI API key')
@secure()
param openAiApiKey string = ''

@description('Azure OpenAI deployment name')
param openAiDeploymentName string = 'gpt-4o'

@description('Budget start date (first of current month, format: YYYY-MM-01)')
param budgetStartDate string = '2025-08-01'

// Variables
var tags = {
  Environment: environmentName
  Application: 'PoNovaWeight'
  ManagedBy: 'Bicep'
}

// Storage Account
module storage 'modules/storage.bicep' = {
  name: 'storage-deployment'
  params: {
    name: '${baseName}stor'
    location: location
    tags: tags
  }
}

// Application Insights
module appInsights 'modules/app-insights.bicep' = {
  name: 'app-insights-deployment'
  params: {
    name: '${baseName}-ai'
    location: location
    tags: tags
  }
}

// Container Apps
module containerApp 'modules/container-app.bicep' = {
  name: 'container-app-deployment'
  params: {
    name: '${baseName}-api'
    envName: '${baseName}-env'
    acrName: '${baseName}acr'
    location: location
    tags: tags
    appInsightsConnectionString: appInsights.outputs.connectionString
    storageConnectionString: storage.outputs.connectionString
    googleClientId: googleClientId
    googleClientSecret: googleClientSecret
    openAiEndpoint: openAiEndpoint
    openAiApiKey: openAiApiKey
    openAiDeploymentName: openAiDeploymentName
  }
}

// Budget with alerts (disabled for now - date validation issues with existing budgets)
// module budget 'modules/budget.bicep' = {
//   name: 'budget-deployment'
//   params: {
//     name: '${baseName}-budget'
//     amount: budgetAmount
//     alertEmail: budgetAlertEmail
//     startDate: budgetStartDate
//   }
// }

// Outputs
output containerAppUrl string = containerApp.outputs.url
output containerAppFqdn string = containerApp.outputs.fqdn
output containerAppName string = containerApp.outputs.name
output acrLoginServer string = containerApp.outputs.acrLoginServer
output acrName string = containerApp.outputs.acrName
output storageAccountName string = storage.outputs.name
output appInsightsName string = appInsights.outputs.name
output resourceGroupName string = resourceGroup().name
