// Nova Food Journal - Storage Account Module
// Deploys Azure Storage Account for Table Storage (daily logs)

@description('Storage account name (must be globally unique, 3-24 chars, lowercase alphanumeric)')
@minLength(3)
@maxLength(24)
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object = {}

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: name
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS' // Locally redundant - cost effective for MVP
  }
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true // Required for Table Storage SDK
    networkAcls: {
      defaultAction: 'Allow' // Will be restricted in production
      bypass: 'AzureServices'
    }
    encryption: {
      services: {
        table: {
          enabled: true
          keyType: 'Account'
        }
        blob: {
          enabled: true
          keyType: 'Account'
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

// Table Service
resource tableService 'Microsoft.Storage/storageAccounts/tableServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
  properties: {}
}

// Daily Logs Table
resource dailyLogsTable 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = {
  parent: tableService
  name: 'DailyLogs'
}

// Outputs
output id string = storageAccount.id
output name string = storageAccount.name
output connectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
output tableEndpoint string = storageAccount.properties.primaryEndpoints.table
