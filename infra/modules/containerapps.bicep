// =============================================================================
// modules/containerapps.bicep – Azure Container Apps Environment + App
// =============================================================================
param prefix string
param location string
param tags object
param acrLoginServer string
param sqlConnectionString string
param redisConnectionString string
param serviceBusConnectionString string
param appInsightsConnectionString string

var envName = '${prefix}-cae'
var appName = '${prefix}-api'

resource managedEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: envName
  location: location
  tags: tags
  properties: {
    zoneRedundant: true
  }
}

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: managedEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
      }
      registries: [
        {
          server: acrLoginServer
          identity: 'system'
        }
      ]
      secrets: [
        {
          name: 'sql-conn'
          value: sqlConnectionString
        }
        {
          name: 'redis-conn'
          value: redisConnectionString
        }
        {
          name: 'sb-conn'
          value: serviceBusConnectionString
        }
        {
          name: 'ai-conn'
          value: appInsightsConnectionString
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: '${acrLoginServer}/smarthealth-appointments:latest'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'ConnectionStrings__SqlServer'
              secretRef: 'sql-conn'
            }
            {
              name: 'ConnectionStrings__Redis'
              secretRef: 'redis-conn'
            }
            {
              name: 'ConnectionStrings__AzureServiceBus'
              secretRef: 'sb-conn'
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              secretRef: 'ai-conn'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/liveness'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 15
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/readiness'
                port: 8080
              }
              initialDelaySeconds: 5
              periodSeconds: 10
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
        rules: [
          {
            name: 'http-scale'
            http: {
              metadata: {
                concurrentRequests: '100'
              }
            }
          }
        ]
      }
    }
  }
}

output apiUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
