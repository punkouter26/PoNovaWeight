// Nova Food Journal - Application Insights Module
// Deploys Application Insights and Log Analytics for monitoring

@description('Application Insights name')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object = {}

@description('Daily data cap in GB')
param dailyDataCapGb int = 1

// Log Analytics Workspace (required for Application Insights)
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${name}-logs'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30 // Minimum retention
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      dailyQuotaGb: dailyDataCapGb
    }
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Bluefield'
    Request_Source: 'rest'
    WorkspaceResourceId: logAnalytics.id
    RetentionInDays: 30
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

@description('Enable availability (health check ping) test')
param enableAvailabilityTest bool = true

// Availability Test (health check ping)
if (enableAvailabilityTest) {
  resource availabilityTest 'Microsoft.Insights/webtests@2022-06-15' = {
    name: '${name}-health-ping'
    location: location
    tags: union(tags, {
      'hidden-link:${appInsights.id}': 'Resource'
    })
    kind: 'ping'
    properties: {
      SyntheticMonitorId: '${name}-health-ping'
      Name: 'Health Check Ping'
      Enabled: true
      Frequency: 300 // 5 minutes
      Timeout: 30
      Kind: 'standard'
      RetryEnabled: true
      Locations: [
        {
          Id: 'us-va-ash-azr' // East US
        }
      ]
      Request: {
        RequestUrl: 'https://placeholder-url/api/health' // Updated during deployment
        HttpVerb: 'GET'
        ParseDependentRequests: false
      }
      ValidationRules: {
        ExpectedHttpStatusCode: 200
        SSLCheck: true
        SSLCertRemainingLifetimeCheck: 7
      }
    }
  }
}

// Outputs
output id string = appInsights.id
output name string = appInsights.name
output connectionString string = appInsights.properties.ConnectionString
output instrumentationKey string = appInsights.properties.InstrumentationKey
output logAnalyticsId string = logAnalytics.id
output logAnalyticsName string = logAnalytics.name
