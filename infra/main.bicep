// =============================================================================
// main.bicep – Orchestrates all SmartHealth Azure resources
// =============================================================================
@description('Deployment environment (dev, staging, prod)')
param environment string = 'dev'

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('SQL Server administrator password')
@secure()
param sqlAdminPassword string

var prefix = 'smarthealth-${environment}'
var tags = {
  application: 'SmartHealth.Appointments'
  environment: environment
  managedBy: 'bicep'
}

// ---------------------------------------------------------------------------
// Azure SQL
// ---------------------------------------------------------------------------
module sql 'modules/sql.bicep' = {
  name: 'sql-deployment'
  params: {
    prefix: prefix
    location: location
    tags: tags
    adminPassword: sqlAdminPassword
  }
}

// ---------------------------------------------------------------------------
// Azure Service Bus
// ---------------------------------------------------------------------------
module serviceBus 'modules/servicebus.bicep' = {
  name: 'servicebus-deployment'
  params: {
    prefix: prefix
    location: location
    tags: tags
  }
}

// ---------------------------------------------------------------------------
// Azure Cache for Redis
// ---------------------------------------------------------------------------
module redis 'modules/redis.bicep' = {
  name: 'redis-deployment'
  params: {
    prefix: prefix
    location: location
    tags: tags
  }
}

// ---------------------------------------------------------------------------
// Application Insights + Log Analytics
// ---------------------------------------------------------------------------
module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring-deployment'
  params: {
    prefix: prefix
    location: location
    tags: tags
  }
}

// ---------------------------------------------------------------------------
// Azure Container Registry
// ---------------------------------------------------------------------------
module acr 'modules/acr.bicep' = {
  name: 'acr-deployment'
  params: {
    prefix: prefix
    location: location
    tags: tags
  }
}

// ---------------------------------------------------------------------------
// Azure Container Apps (replaces AKS for cost-optimised deployments)
// ---------------------------------------------------------------------------
module containerApps 'modules/containerapps.bicep' = {
  name: 'containerapps-deployment'
  params: {
    prefix: prefix
    location: location
    tags: tags
    acrLoginServer: acr.outputs.loginServer
    sqlConnectionString: sql.outputs.connectionString
    redisConnectionString: redis.outputs.connectionString
    serviceBusConnectionString: serviceBus.outputs.connectionString
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
  }
}

// ---------------------------------------------------------------------------
// Key Vault
// ---------------------------------------------------------------------------
module keyVault 'modules/keyvault.bicep' = {
  name: 'keyvault-deployment'
  params: {
    prefix: prefix
    location: location
    tags: tags
  }
}

output apiUrl string = containerApps.outputs.apiUrl
