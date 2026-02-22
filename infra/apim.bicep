// =============================================================================
// infra/apim.bicep – Azure API Management configuration
// JWT validation, Rate limiting, Routing to SmartHealth Appointments API
// =============================================================================
param prefix string
param location string
param tags object
param apiUrl string

@description('Azure AD / Entra ID tenant ID for JWT validation')
param tenantId string

@description('Azure AD audience (App Registration client ID)')
param jwtAudience string

var apimName = '${prefix}-apim'

resource apim 'Microsoft.ApiManagement/service@2023-05-01-preview' = {
  name: apimName
  location: location
  tags: tags
  sku: {
    name: 'Developer'  // Use 'Standard' or 'Premium' for production
    capacity: 1
  }
  properties: {
    publisherEmail: 'admin@smarthealth.example.com'
    publisherName: 'SmartHealth'
  }
}

// ---------------------------------------------------------------------------
// Named Values
// ---------------------------------------------------------------------------
resource backendUrl 'Microsoft.ApiManagement/service/namedValues@2023-05-01-preview' = {
  parent: apim
  name: 'appointments-backend-url'
  properties: {
    displayName: 'AppointmentsBackendUrl'
    value: apiUrl
    secret: false
  }
}

// ---------------------------------------------------------------------------
// Backend
// ---------------------------------------------------------------------------
resource backend 'Microsoft.ApiManagement/service/backends@2023-05-01-preview' = {
  parent: apim
  name: 'appointments-backend'
  properties: {
    url: apiUrl
    protocol: 'http'
    title: 'Appointments API Backend'
    tls: {
      validateCertificateChain: true
      validateCertificateName: true
    }
  }
}

// ---------------------------------------------------------------------------
// API definition
// ---------------------------------------------------------------------------
resource appointmentsApi 'Microsoft.ApiManagement/service/apis@2023-05-01-preview' = {
  parent: apim
  name: 'appointments-api'
  properties: {
    displayName: 'SmartHealth Appointments API'
    path: 'appointments'
    protocols: ['https']
    serviceUrl: apiUrl
    subscriptionRequired: true
    apiVersion: 'v1'
    apiVersionDescription: 'Version 1'
  }
}

// ---------------------------------------------------------------------------
// Policy: JWT validation + rate limiting (applied at API level)
// ---------------------------------------------------------------------------
resource apiPolicy 'Microsoft.ApiManagement/service/apis/policies@2023-05-01-preview' = {
  parent: appointmentsApi
  name: 'policy'
  properties: {
    format: 'xml'
    value: '''
<policies>
  <inbound>
    <base />

    <!-- JWT Validation (Entra ID / Azure AD) -->
    <validate-jwt header-name="Authorization"
                  failed-validation-httpcode="401"
                  failed-validation-error-message="Unauthorized">
      <openid-config url="https://login.microsoftonline.com/${tenantId}/v2.0/.well-known/openid-configuration" />
      <audiences>
        <audience>${jwtAudience}</audience>
      </audiences>
      <required-claims>
        <claim name="scp" match="any">
          <value>appointments.read</value>
          <value>appointments.write</value>
        </claim>
      </required-claims>
    </validate-jwt>

    <!-- Rate limiting: 200 calls per minute per subscription -->
    <rate-limit calls="200" renewal-period="60" />

    <!-- Quota: 10000 calls per day -->
    <quota calls="10000" renewal-period="86400" />

    <!-- Route to backend -->
    <set-backend-service backend-id="appointments-backend" />

    <!-- Correlation ID forwarding -->
    <set-header name="X-Correlation-Id" exists-action="override">
      <value>@(context.RequestId.ToString())</value>
    </set-header>
  </inbound>

  <backend>
    <base />
    <retry condition="@(context.Response.StatusCode == 503)" count="3" interval="1" />
  </backend>

  <outbound>
    <base />
    <!-- Remove internal headers -->
    <set-header name="X-Powered-By" exists-action="delete" />
    <set-header name="Server" exists-action="delete" />
  </outbound>

  <on-error>
    <base />
    <return-response>
      <set-status code="@(context.LastError.StatusCode)" />
      <set-body>@(context.LastError.Message)</set-body>
    </return-response>
  </on-error>
</policies>
'''
  }
}

output apimGatewayUrl string = apim.properties.gatewayUrl
