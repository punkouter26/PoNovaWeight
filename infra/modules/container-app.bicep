// Azure Container Apps Module
// Deploys Container App Environment and Container App

@description('Container App name')
param name string

@description('Container App Environment name')
param envName string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Container Registry name')
param acrName string

@description('Container image')
param containerImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('Target port')
param targetPort int = 8080

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

// Container Registry
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

// Log Analytics Workspace for Container Apps
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${name}-logs'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Container Apps Environment
resource containerAppEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: envName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// Container App
resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: targetPort
        transport: 'http'
        allowInsecure: false
      }
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acr.listCredentials().passwords[0].value
        }
        {
          name: 'storage-connection'
          value: storageConnectionString
        }
        {
          name: 'google-client-secret'
          value: !empty(googleClientSecret) ? googleClientSecret : 'placeholder'
        }
        {
          name: 'openai-api-key'
          value: !empty(openAiApiKey) ? openAiApiKey : 'placeholder'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: containerImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ConnectionStrings__tables'
              secretRef: 'storage-connection'
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              value: appInsightsConnectionString
            }
            {
              name: 'Google__ClientId'
              value: !empty(googleClientId) ? googleClientId : 'placeholder'
            }
            {
              name: 'Google__ClientSecret'
              secretRef: 'google-client-secret'
            }
            {
              name: 'AzureOpenAI__Endpoint'
              value: openAiEndpoint
            }
            {
              name: 'AzureOpenAI__ApiKey'
              secretRef: 'openai-api-key'
            }
            {
              name: 'AzureOpenAI__DeploymentName'
              value: openAiDeploymentName
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// Outputs
output fqdn string = containerApp.properties.configuration.ingress.fqdn
output url string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output name string = containerApp.name
output envName string = containerAppEnv.name
output acrLoginServer string = acr.properties.loginServer
output acrName string = acr.name
