targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment (e.g. prod, dev)')
param environmentName string

@description('Primary location for all resources')
param location string

@description('Name of the resource group')
param resourceGroupName string = 'PoNovaWeight'

// ── Resource Group ───────────────────────────────────────────────
resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
}

// ── Deploy resources into the resource group ─────────────────────
module resources 'resources.bicep' = {
  name: 'resources'
  scope: rg
  params: {
    environmentName: environmentName
    location: location
  }
}

// ── Outputs (azd uses SERVICE_<name>_NAME to map deployments) ────
output AZURE_STORAGE_ACCOUNT_NAME string = resources.outputs.AZURE_STORAGE_ACCOUNT_NAME
output SERVICE_APP_NAME string = resources.outputs.SERVICE_APP_NAME
output WEB_APP_URL string = resources.outputs.WEB_APP_URL
