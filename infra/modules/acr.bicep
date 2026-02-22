// =============================================================================
// modules/acr.bicep – Azure Container Registry
// =============================================================================
param prefix string
param location string
param tags object

var acrName = replace('${prefix}acr', '-', '')

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  tags: tags
  sku: {
    name: 'Premium'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
    zoneRedundancy: 'Enabled'
    policies: {
      retentionPolicy: {
        days: 30
        status: 'enabled'
      }
      trustPolicy: {
        type: 'Notary'
        status: 'enabled'
      }
    }
  }
}

output loginServer string = acr.properties.loginServer
output acrName string = acr.name
