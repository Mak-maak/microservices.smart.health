// =============================================================================
// modules/servicebus.bicep – Azure Service Bus Namespace + queues/topics
// =============================================================================
param prefix string
param location string
param tags object

var namespaceName = replace('${prefix}-sb', '-', '')

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  tags: tags
  sku: {
    name: 'Premium'
    tier: 'Premium'
    capacity: 1
  }
  properties: {
    premiumMessagingPartitions: 1
    zoneRedundant: true
  }
}

resource appointmentsQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'appointments'
  properties: {
    lockDuration: 'PT5M'
    maxDeliveryCount: 10
    deadLetteringOnMessageExpiration: true
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    enableBatchedOperations: true
  }
}

resource deadLetterRule 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'appointments/$DeadLetterQueue'
  properties: {}
}

// Send authorization rule
resource sendRule 'Microsoft.ServiceBus/namespaces/AuthorizationRules@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'SmartHealthAppSend'
  properties: {
    rights: ['Send', 'Listen', 'Manage']
  }
}

output connectionString string = listKeys(sendRule.id, sendRule.apiVersion).primaryConnectionString
