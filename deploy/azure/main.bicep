// =============================================================================
// Task Management — Azure deployment (free tier)
// -----------------------------------------------------------------------------
// Provisions the minimum set of Azure resources needed to host the full stack
// on the permanent free allowances:
//   - Azure SQL Database using the "Free offer" (100K vCore-seconds/mo serverless
//     GP_S_Gen5_2 + 32 GB storage). Auto-pauses after 1h idle; wakes on query.
//   - Two Container Apps (API + SPA) on Consumption plan, scale-to-zero. The
//     first 180K vCPU-seconds + 360K GiB-seconds + 2M requests per subscription
//     per month are free; scale-to-zero keeps us inside that envelope.
//   - Log Analytics workspace (required wiring for Container Apps diagnostics;
//     free up to 5 GB ingestion/mo).
//
// The stack is intentionally single-region and single-replica. It is a demo, not
// a production deployment — the existing compose file is what lands in prod.
// =============================================================================

targetScope = 'resourceGroup'

// -----------------------------------------------------------------------------
// Parameters
// -----------------------------------------------------------------------------

@description('Short, lowercase name used to prefix every resource (3-12 chars).')
@minLength(3)
@maxLength(12)
param projectName string = 'tasksmt'

@description('Azure region. Free SQL offer is only available in a subset; West Europe works.')
param location string = resourceGroup().location

@description('SQL Server admin login. Cannot be "admin", "sa", "root" etc.')
@minLength(4)
param sqlAdminLogin string

@description('SQL Server admin password. Must satisfy Azure SQL complexity rules.')
@secure()
@minLength(12)
param sqlAdminPassword string

@description('32+ char secret used to sign JWT access tokens.')
@secure()
@minLength(32)
param jwtSecret string

@description('API container image reference (published by CI to ghcr.io).')
param apiImage string = 'ghcr.io/OWNER/REPO/api:latest'

@description('SPA container image reference (published by CI to ghcr.io).')
param spaImage string = 'ghcr.io/OWNER/REPO/frontend:latest'

@description('Whether the API should run the demo-data seeder on startup.')
param seedDemoData bool = true

@description('Optional: a pre-existing Static Web App / CDN origin to whitelist in CORS. Leave empty to allow only the provisioned SPA.')
param extraCorsOrigin string = ''

// -----------------------------------------------------------------------------
// Deterministic resource names
// -----------------------------------------------------------------------------

var suffix = uniqueString(resourceGroup().id, projectName)
var sqlServerName = toLower('${projectName}-sql-${suffix}')
var sqlDatabaseName = 'taskmanagement'
var logAnalyticsName = '${projectName}-logs-${suffix}'
var containerEnvName = '${projectName}-env-${suffix}'
var apiAppName = '${projectName}-api'
var spaAppName = '${projectName}-spa'

// -----------------------------------------------------------------------------
// Log Analytics — required by Container Apps for diagnostics
// -----------------------------------------------------------------------------

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

// -----------------------------------------------------------------------------
// Azure SQL Server + free-offer Database
// -----------------------------------------------------------------------------

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Allow Azure services + the Container Apps outbound IPs through the SQL firewall.
// 0.0.0.0/0.0.0.0 in the Sql firewall rule syntax is the documented "allow all
// Azure-internal IPs" rule; does not expose SQL to the public internet.
resource sqlFirewallAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllAzureIPs'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  // Free offer: GP_Serverless_Gen5_2 + 32 GB storage + 100K vCore-seconds/mo
  // free, auto-pause after 1h. Flag useFreeLimit + AutoPause behaviour is what
  // makes Azure treat the database as "free" and keeps billing at zero as long
  // as usage stays under the monthly grant.
  sku: {
    name: 'GP_S_Gen5_2'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 2
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 34359738368 // 32 GB — matches the free-offer ceiling
    autoPauseDelay: 60
    minCapacity: json('0.5')
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: 'Local'
  }
}

// -----------------------------------------------------------------------------
// Container Apps environment (shared managed environment)
// -----------------------------------------------------------------------------

resource containerEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerEnvName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
  }
}

// -----------------------------------------------------------------------------
// API Container App
// -----------------------------------------------------------------------------

// Connection string assembled from the provisioned server + database so
// restarts of the Bicep deployment don't drift from the actual server name.
var sqlConnectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

resource apiApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiAppName
  location: location
  properties: {
    environmentId: containerEnv.id
    workloadProfileName: 'Consumption'
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
        traffic: [ { weight: 100, latestRevision: true } ]
      }
      secrets: [
        { name: 'db-connection-string', value: sqlConnectionString }
        { name: 'jwt-secret', value: jwtSecret }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: apiImage
          resources: {
            // 0.25 vCPU / 0.5 GiB — the minimum Consumption-plan increment.
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: concat(
            [
              { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
              { name: 'ASPNETCORE_URLS', value: 'http://+:8080' }
              { name: 'ConnectionStrings__Database', secretRef: 'db-connection-string' }
              { name: 'Jwt__SecretKey', secretRef: 'jwt-secret' }
              { name: 'Jwt__Issuer', value: 'TaskManagement.Api' }
              { name: 'Jwt__Audience', value: 'TaskManagement.Client' }
              { name: 'Database__RunMigrationsOnStartup', value: 'true' }
              { name: 'Seeding__DemoData', value: string(seedDemoData) }
              { name: 'Cors__Origins__0', value: 'https://${spaAppName}.${containerEnv.properties.defaultDomain}' }
            ],
            // Only declare the extra CORS origin when one was passed in — an
            // empty string slipping into the config array would fail WithOrigins.
            empty(extraCorsOrigin) ? [] : [
              { name: 'Cors__Origins__1', value: extraCorsOrigin }
            ])
          probes: [
            {
              type: 'Liveness'
              httpGet: { path: '/health', port: 8080 }
              initialDelaySeconds: 10
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: { path: '/health/ready', port: 8080 }
              initialDelaySeconds: 15
              periodSeconds: 30
              failureThreshold: 6
            }
          ]
        }
      ]
      scale: {
        // Scale-to-zero: this is what keeps us inside the Container Apps free
        // grant. First request after idle pays a ~3-5s cold start.
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

// -----------------------------------------------------------------------------
// SPA Container App
// -----------------------------------------------------------------------------

resource spaApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: spaAppName
  location: location
  properties: {
    environmentId: containerEnv.id
    workloadProfileName: 'Consumption'
    configuration: {
      ingress: {
        external: true
        targetPort: 80
        transport: 'auto'
        allowInsecure: false
        traffic: [ { weight: 100, latestRevision: true } ]
      }
    }
    template: {
      containers: [
        {
          name: 'spa'
          image: spaImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          // API_BASE_URL is baked into the SPA's runtime env.js at container
          // start by docker-entrypoint.sh. Pointing it at the API's public FQDN
          // means the browser talks directly to the API — no reverse proxy.
          env: [
            { name: 'API_BASE_URL', value: 'https://${apiApp.properties.configuration.ingress.fqdn}' }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

// -----------------------------------------------------------------------------
// Outputs — consumed by deploy.sh to print the URLs once provisioning finishes.
// -----------------------------------------------------------------------------

output apiUrl string = 'https://${apiApp.properties.configuration.ingress.fqdn}'
output spaUrl string = 'https://${spaApp.properties.configuration.ingress.fqdn}'
output sqlFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabaseName
