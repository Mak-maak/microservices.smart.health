# =============================================================================
# destroy-stack.ps1 - Clean up SmartHealth Azure infrastructure
# =============================================================================
# This script safely removes all Azure resources managed by the deployment stack
# =============================================================================

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment,
    
    [Parameter(Mandatory=$false)]
    [switch]$DeleteResourceGroup,
    
    [Parameter(Mandatory=$false)]
    [switch]$Force,
    
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf
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

Write-Header "Pre-flight Checks"

# Check if logged in
try {
    $account = az account show --output json | ConvertFrom-Json
    Write-Success "Logged in as: $($account.user.name)"
    Write-Info "Subscription: $($account.name) ($($account.id))"
} catch {
    Write-Error "Not logged in to Azure. Run 'az login' first."
    exit 1
}

# Check if resource group exists
$rgExists = az group exists --name $resourceGroupName --output tsv

if ($rgExists -eq "false") {
    Write-Warning "Resource group '$resourceGroupName' does not exist"
    Write-Info "Nothing to delete"
    exit 0
}

# =============================================================================
# Confirmation
# =============================================================================

if (-not $Force -and -not $WhatIf) {
    Write-Header "⚠️  DANGER ZONE ⚠️"
    Write-Warning "This will DELETE the following:"
    Write-Host "  • Environment: $Environment" -ForegroundColor Red
    Write-Host "  • Resource Group: $resourceGroupName" -ForegroundColor Red
    Write-Host "  • Stack: $stackName" -ForegroundColor Red
    
    # List resources that will be deleted
    try {
        Write-Host "`n  Resources to be deleted:" -ForegroundColor Yellow
        $resources = az resource list --resource-group $resourceGroupName --output json | ConvertFrom-Json
        $resources | ForEach-Object {
            Write-Host "    - $($_.name) ($($_.type))" -ForegroundColor Red
        }
    } catch {
        Write-Warning "Could not list resources"
    }
    
    Write-Host ""
    $confirmation = Read-Host "Type 'DELETE' to confirm deletion"
    
    if ($confirmation -ne "DELETE") {
        Write-Info "Deletion cancelled"
        exit 0
    }
}

# =============================================================================
# Delete APIM Stack (if exists)
# =============================================================================

Write-Header "Checking for APIM Stack"

try {
    $apimStack = az stack group show `
        --name $apimStackName `
        --resource-group $resourceGroupName `
        --output json 2>$null | ConvertFrom-Json
    
    if ($apimStack) {
        Write-Info "Found APIM stack, deleting..."
        
        if (-not $WhatIf) {
            az stack group delete `
                --name $apimStackName `
                --resource-group $resourceGroupName `
                --action-on-unmanage deleteAll `
                --yes | Out-Null
            
            Write-Success "APIM stack deleted"
        } else {
            Write-Info "[WHAT-IF] Would delete APIM stack"
        }
    } else {
        Write-Info "No APIM stack found"
    }
} catch {
    Write-Info "No APIM stack found"
}

# =============================================================================
# Delete Main Stack
# =============================================================================

Write-Header "Deleting Main Infrastructure Stack"

try {
    $stack = az stack group show `
        --name $stackName `
        --resource-group $resourceGroupName `
        --output json 2>$null | ConvertFrom-Json
    
    if ($stack) {
        Write-Info "Deleting stack '$stackName'..."
        Write-Warning "This will delete ALL resources managed by the stack"
        
        if (-not $WhatIf) {
            az stack group delete `
                --name $stackName `
                --resource-group $resourceGroupName `
                --action-on-unmanage deleteAll `
                --yes | Out-Null
            
            Write-Success "Infrastructure stack deleted"
        } else {
            Write-Info "[WHAT-IF] Would delete infrastructure stack"
        }
    } else {
        Write-Warning "Stack '$stackName' not found"
    }
} catch {
    Write-Warning "Stack '$stackName' not found or already deleted"
}

# =============================================================================
# Delete Resource Group (Optional)
# =============================================================================

if ($DeleteResourceGroup) {
    Write-Header "Deleting Resource Group"
    
    Write-Warning "Deleting resource group '$resourceGroupName' and any remaining resources"
    
    if (-not $WhatIf) {
        az group delete `
            --name $resourceGroupName `
            --yes `
            --no-wait
        
        Write-Success "Resource group deletion initiated (running in background)"
        Write-Info "Monitor status with: az group show --name $resourceGroupName"
    } else {
        Write-Info "[WHAT-IF] Would delete resource group"
    }
}

# =============================================================================
# Summary
# =============================================================================

Write-Header "Cleanup Summary"

if (-not $WhatIf) {
    Write-Success "Cleanup completed successfully"
    
    if ($DeleteResourceGroup) {
        Write-Info "Resource group deletion is running in the background"
        Write-Info "It may take several minutes to complete"
    } else {
        Write-Info "Resource group '$resourceGroupName' was preserved"
        Write-Info "To delete it manually, run:"
        Write-Host "  az group delete --name $resourceGroupName --yes" -ForegroundColor Gray
    }
} else {
    Write-Info "[WHAT-IF] No changes were made"
}

Write-Host ""
