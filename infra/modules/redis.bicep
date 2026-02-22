// =============================================================================
// modules/redis.bicep – Azure Cache for Redis
// =============================================================================
param prefix string
param location string
param tags object

var redisName = '${prefix}-redis'

resource redis 'Microsoft.Cache/redis@2023-08-01' = {
  name: redisName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'Premium'
      family: 'P'
      capacity: 1
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
      'rdb-backup-enabled': 'true'
      'rdb-backup-frequency': '60'
    }
  }
  zones: ['1', '2', '3']
}

output connectionString string = '${redis.properties.hostName}:6380,password=${listKeys(redis.id, redis.apiVersion).primaryKey},ssl=True,abortConnect=False'
