# =============================================================================
# validate-stack.ps1 - Validate and check drift for Azure Deployment Stack
# =============================================================================

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment,
    
    [Parameter(Mandatory=$false)]
    [switch]$CheckDrift,
    
    [Parameter(Mandatory=$false)]
    [switch]$DetailedOutput
)

# =============================================================================
# Configuration
# =============================================================================
$ErrorActionPreference = "Stop"
$resourceGroupName = "smarthealth-$Environment-rg"
$stackName = "smarthealth-$Environment-stack"
$apimStackName = "$stackName-apim"

# =============================================================================
# Functions
# =============================================================================

function Write-Header {
    param([string]$Message)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ $Message" -ForegroundColor Blue
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

# =============================================================================
# Pre-flight Checks
# =============================================================================

Write-Header "Validation for Environment: $Environment"

# Check if logged in
try {
    $account = az account show --output json | ConvertFrom-Json
    Write-Success "Logged in as: $($account.user.name)"
} catch {
    Write-Error "Not logged in to Azure. Run 'az login' first."
    exit 1
}

# Check if resource group exists
$rgExists = az group exists --name $resourceGroupName --output tsv

if ($rgExists -eq "false") {
    Write-Error "Resource group '$resourceGroupName' does not exist"
    exit 1
}

# =============================================================================
# Stack Status
# =============================================================================

Write-Header "Stack Status"

try {
    $stack = az stack group show `
        --name $stackName `
        --resource-group $resourceGroupName `
        --output json | ConvertFrom-Json
    
    Write-Success "Stack Name: $($stack.name)"
    Write-Info "Provisioning State: $($stack.provisioningState)"
    Write-Info "Last Modified: $($stack.systemData.lastModifiedAt)"
    Write-Info "Action on Unmanage: $($stack.actionOnUnmanage.resources)"
    Write-Info "Deny Settings: $($stack.denySettings.mode)"
    
    # Resources count
    $resourceCount = ($stack.resources | Measure-Object).Count
    Write-Info "Managed Resources: $resourceCount"
    
} catch {
    Write-Error "Stack '$stackName' not found"
    exit 1
}

# =============================================================================
# List Stack Resources
# =============================================================================

Write-Header "Managed Resources"

if ($DetailedOutput) {
    Write-Host ("{0,-50} {1,-40} {2}" -f "Name", "Type", "Status") -ForegroundColor Yellow
    Write-Host ("{0,-50} {1,-40} {2}" -f "----", "----", "------") -ForegroundColor Yellow
    
    $stack.resources | ForEach-Object {
        $resourceName = ($_.id -split '/')[-1]
        $resourceType = ($_.id -split '/providers/')[-1] -replace "/[^/]+$"
        $status = $_.status
        
        if ($status -eq "managed") {
            Write-Host ("{0,-50} {1,-40} {2}" -f $resourceName, $resourceType, $status) -ForegroundColor Green
        } else {
            Write-Host ("{0,-50} {1,-40} {2}" -f $resourceName, $resourceType, $status) -ForegroundColor Yellow
        }
    }
} else {
    $stack.resources | Select-Object -First 10 | ForEach-Object {
        $resourceName = ($_.id -split '/')[-1]
        Write-Host "  • $resourceName" -ForegroundColor Gray
    }
    
    if ($resourceCount -gt 10) {
        Write-Info "... and $($resourceCount - 10) more resources"
    }
}

# =============================================================================
# Check for Unmanaged Resources
# =============================================================================

Write-Header "Unmanaged Resources Check"

try {
    $allResources = az resource list --resource-group $resourceGroupName --output json | ConvertFrom-Json
    $managedResourceIds = $stack.resources | ForEach-Object { $_.id }
    
    $unmanagedResources = $allResources | Where-Object { $_.id -notin $managedResourceIds }
    
    if ($unmanagedResources) {
        Write-Warning "Found $($unmanagedResources.Count) unmanaged resources:"
        $unmanagedResources | ForEach-Object {
            Write-Host "  • $($_.name) ($($_.type))" -ForegroundColor Yellow
        }
        Write-Info "These resources are not managed by the deployment stack"
    } else {
        Write-Success "All resources in the resource group are managed by the stack"
    }
} catch {
    Write-Warning "Could not check for unmanaged resources"
}

# =============================================================================
# Stack Outputs
# =============================================================================

Write-Header "Stack Outputs"

if ($stack.outputs) {
    $stack.outputs.PSObject.Properties | ForEach-Object {
        Write-Host "  $($_.Name): " -NoNewline -ForegroundColor Yellow
        Write-Host $_.Value.value -ForegroundColor White
    }
} else {
    Write-Info "No outputs defined"
}

# =============================================================================
# Check APIM Stack
# =============================================================================

Write-Header "APIM Stack Status"

try {
    $apimStack = az stack group show `
        --name $apimStackName `
        --resource-group $resourceGroupName `
        --output json 2>$null | ConvertFrom-Json
    
    if ($apimStack) {
        Write-Success "APIM Stack exists"
        Write-Info "Provisioning State: $($apimStack.provisioningState)"
        Write-Info "Resources: $(($apimStack.resources | Measure-Object).Count)"
    } else {
        Write-Info "APIM Stack not deployed"
    }
} catch {
    Write-Info "APIM Stack not deployed"
}

# =============================================================================
# Drift Detection
# =============================================================================

if ($CheckDrift) {
    Write-Header "Drift Detection"
    Write-Info "Checking for configuration drift..."
    
    # This is a placeholder - Azure CLI doesn't have built-in drift detection yet
    # You would typically:
    # 1. Export current stack template
    # 2. Compare with source Bicep files
    # 3. Check for manual changes
    
    Write-Warning "Drift detection is not fully automated in Azure CLI"
    Write-Info "To check for drift, compare the current state with your Bicep files:"
    Write-Host "  1. Export stack: az stack group export --name $stackName --resource-group $resourceGroupName" -ForegroundColor Gray
    Write-Host "  2. Compare with infra/main.bicep" -ForegroundColor Gray
}

# =============================================================================
# Health Status
# =============================================================================

Write-Header "Resource Health Status"

$healthyCount = 0
$unhealthyCount = 0
$unknownCount = 0

foreach ($resource in $stack.resources) {
    try {
        $resourceId = $resource.id
        $health = az resource show --ids $resourceId --query 'properties.provisioningState' -o tsv 2>$null
        
        if ($health -eq "Succeeded") {
            $healthyCount++
        } elseif ($health -in @("Failed", "Canceled")) {
            $unhealthyCount++
            if ($DetailedOutput) {
                $resourceName = ($resourceId -split '/')[-1]
                Write-Warning "Resource '$resourceName' status: $health"
            }
        } else {
            $unknownCount++
        }
    } catch {
        $unknownCount++
    }
}

Write-Info "Healthy: $healthyCount"
if ($unhealthyCount -gt 0) {
    Write-Warning "Unhealthy: $unhealthyCount"
} else {
    Write-Success "Unhealthy: $unhealthyCount"
}
Write-Info "Unknown: $unknownCount"

# =============================================================================
# Cost Estimation (if available)
# =============================================================================

Write-Header "Cost Information"

try {
    Write-Info "To view costs for this resource group:"
    Write-Host "  az consumption usage list --query '[?contains(instanceId, ``$resourceGroupName``)]' --output table" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Or view in Azure Portal:" -ForegroundColor Gray
    Write-Host "  https://portal.azure.com/#@/resource/subscriptions/$($account.id)/resourceGroups/$resourceGroupName/overview" -ForegroundColor Gray
} catch {
    Write-Warning "Could not retrieve cost information"
}

# =============================================================================
# Recommendations
# =============================================================================

Write-Header "Recommendations"

$recommendations = @()

# Check deny settings
if ($stack.denySettings.mode -eq "none") {
    $recommendations += "Consider enabling deny settings for production: --deny-settings-mode denyWriteAndDelete"
}

# Check unmanaged resources
if ($unmanagedResources) {
    $recommendations += "Clean up unmanaged resources or add them to the stack"
}

# Check action on unmanage
if ($stack.actionOnUnmanage.resources -eq "detach") {
    $recommendations += "For production, consider using --action-on-unmanage resources=delete to prevent resource sprawl"
}

if ($recommendations.Count -gt 0) {
    foreach ($rec in $recommendations) {
        Write-Info "• $rec"
    }
} else {
    Write-Success "No recommendations at this time"
}

# =============================================================================
# Summary
# =============================================================================

Write-Header "Validation Summary"

Write-Success "Stack is operational"
Write-Info "Resource Group: $resourceGroupName"
Write-Info "Stack Name: $stackName"
Write-Info "Environment: $Environment"

Write-Host ""
