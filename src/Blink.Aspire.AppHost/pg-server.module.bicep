@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param administratorLogin string

@secure()
param administratorLoginPassword string

param pg_server_kv_outputs_name string

resource pg_server 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: take('pgserver-${uniqueString(resourceGroup().id)}', 63)
  location: location
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    authConfig: {
      activeDirectoryAuth: 'Disabled'
      passwordAuth: 'Enabled'
    }
    availabilityZone: '1'
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    storage: {
      storageSizeGB: 32
    }
    version: '16'
  }
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  tags: {
    'aspire-resource-name': 'pg-server'
  }
}

resource postgreSqlFirewallRule_AllowAllAzureIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  name: 'AllowAllAzureIps'
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
  parent: pg_server
}

resource keycloak_db 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  name: 'keycloak'
  parent: pg_server
}

resource blink_db 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  name: 'blink-db'
  parent: pg_server
}

resource keyVault 'Microsoft.KeyVault/vaults@2024-11-01' existing = {
  name: pg_server_kv_outputs_name
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  name: 'connectionstrings--pg-server'
  properties: {
    value: 'Host=${pg_server.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword}'
  }
  parent: keyVault
}

resource keycloak_db_connectionString 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  name: 'connectionstrings--keycloak-db'
  properties: {
    value: 'Host=${pg_server.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword};Database=keycloak'
  }
  parent: keyVault
}

resource blink_db_connectionString 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  name: 'connectionstrings--blink-db'
  properties: {
    value: 'Host=${pg_server.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword};Database=blink-db'
  }
  parent: keyVault
}

output name string = pg_server.name

output hostName string = pg_server.properties.fullyQualifiedDomainName