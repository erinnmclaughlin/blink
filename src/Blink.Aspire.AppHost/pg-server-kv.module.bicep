@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

resource pg_server_kv 'Microsoft.KeyVault/vaults@2024-11-01' = {
  name: take('pgserverkv-${uniqueString(resourceGroup().id)}', 24)
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
  }
  tags: {
    'aspire-resource-name': 'pg-server-kv'
  }
}

output vaultUri string = pg_server_kv.properties.vaultUri

output name string = pg_server_kv.name