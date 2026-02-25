# =============================================================================
# deploy-stack.ps1 - Deploy SmartHealth infrastructure using Azure Deployment Stacks
# =============================================================================
# Azure Deployment Stacks provide:
# - Unified resource lifecycle management
# - Automatic cleanup of unmanaged resources
# - Drift detection and prevention
# - Better resource grouping and tracking
# =============================================================================

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment,
    
    [Parameter(Mandatory=$true)]
    [string]$Location = "eastus",
    
    [Parameter(Mandatory=$true)]
    [SecureString]$SqlAdminPassword,
    
    [Parameter(Mandatory=$false)]
    [switch]$DeployApiManagement,
    
    [Parameter(Mandatory=$false)]
    [string]$ApiUrl,
    
    [Parameter(Mandatory=$false)]
    [string]$TenantId,
    
    [Parameter(Mandatory=$false)]
    [string]$JwtAudience,
    
    [Parameter(Mandatory=$false)]
    [ValidateSet('detach', 'delete')]
    [string]$ActionOnUnmanage = 'detach',
    
    [Parameter(Mandatory=$false)]
    [switch]$DenyWriteAndDelete,
    
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf
)

# =============================================================================
# Configuration
# =============================================================================
$ErrorActionPreference = "Stop"
$resourceGroupName = "smarthealth-$Environment-rg"
$stackName = "smarthealth-$Environment-stack"
$mainBicepPath = "$PSScriptRoot/../main.bicep"
$apimBicepPath = "$PSScriptRoot/../apim.bicep"

# Tags for all resources
$tags = @{
    "application" = "SmartHealth.Appointments"
    "environment" = $Environment
    "managedBy" = "azure-deployment-stack"
    "deployedBy" = $env:USERNAME
    "deployedAt" = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
}

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

# Check if Azure CLI is installed
try {
    $azVersion = az version --output json | ConvertFrom-Json
    Write-Success "Azure CLI version: $($azVersion.'azure-cli')"
} catch {
    Write-Error "Azure CLI is not installed. Please install from https://aka.ms/installazurecliwindows"
    exit 1
}

# Check if logged in
try {
    $account = az account show --output json | ConvertFrom-Json
    Write-Success "Logged in as: $($account.user.name)"
    Write-Info "Subscription: $($account.name) ($($account.id))"
} catch {
    Write-Error "Not logged in to Azure. Run 'az login' first."
    exit 1
}

# Check if Bicep files exist
if (-not (Test-Path $mainBicepPath)) {
    Write-Error "main.bicep not found at: $mainBicepPath"
    exit 1
}
Write-Success "Found main.bicep"

# Validate APIM parameters if deployment requested
if ($DeployApiManagement) {
    if ([string]::IsNullOrWhiteSpace($ApiUrl)) {
        Write-Error "ApiUrl is required when DeployApiManagement is specified"
        exit 1
    }
    if (-not (Test-Path $apimBicepPath)) {
        Write-Error "apim.bicep not found at: $apimBicepPath"
        exit 1
    }
    Write-Success "APIM deployment parameters validated"
}

# =============================================================================
# Create Resource Group
# =============================================================================

Write-Header "Creating Resource Group"

$rgExists = az group exists --name $resourceGroupName --output tsv

if ($rgExists -eq "true") {
    Write-Info "Resource group '$resourceGroupName' already exists"
} else {
    Write-Info "Creating resource group '$resourceGroupName' in '$Location'"
    
    if (-not $WhatIf) {
        $tagsJson = ($tags.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join " "
        
        az group create `
            --name $resourceGroupName `
            --location $Location `
            --tags $tagsJson | Out-Null
        
        Write-Success "Resource group created"
    } else {
        Write-Info "[WHAT-IF] Would create resource group"
    }
}

# =============================================================================
# Deploy Infrastructure Stack
# =============================================================================

Write-Header "Deploying Infrastructure Stack"

Write-Info "Stack Name: $stackName"
Write-Info "Environment: $Environment"
Write-Info "Location: $Location"
Write-Info "Action on Unmanage: $ActionOnUnmanage"
Write-Info "Deny Settings: $(if ($DenyWriteAndDelete) { 'denyWriteAndDelete' } else { 'none' })"

# Convert SecureString to plain text for Azure CLI
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdminPassword)
$plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
[System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)

# Build the deployment command
$deployCommand = @(
    "az", "stack", "group", "create",
    "--name", $stackName,
    "--resource-group", $resourceGroupName,
    "--template-file", $mainBicepPath,
    "--parameters", "environment=$Environment",
    "--parameters", "sqlAdminPassword=$plainPassword",
    "--action-on-unmanage", "resources=$ActionOnUnmanage",
    "--deny-settings-mode", $(if ($DenyWriteAndDelete) { "denyWriteAndDelete" } else { "none" }),
    "--yes"
)

if ($WhatIf) {
    Write-Info "[WHAT-IF] Would execute deployment stack command"
    Write-Host ($deployCommand -join " ") -ForegroundColor DarkGray
} else {
    Write-Info "Deploying infrastructure... (this may take 10-15 minutes)"
    
    try {
        & $deployCommand[0] $deployCommand[1..($deployCommand.Length - 1)]
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Infrastructure stack deployed successfully"
        } else {
            throw "Deployment failed with exit code: $LASTEXITCODE"
        }
    } catch {
        Write-Error "Deployment failed: $_"
        exit 1
    }
}

# Clear the password from memory
$plainPassword = $null
[System.GC]::Collect()

# =============================================================================
# Get Stack Outputs
# =============================================================================

Write-Header "Stack Outputs"

if (-not $WhatIf) {
    try {
        $stack = az stack group show `
            --name $stackName `
            --resource-group $resourceGroupName `
            --output json | ConvertFrom-Json
        
        if ($stack.outputs) {
            Write-Success "Deployment outputs:"
            $stack.outputs.PSObject.Properties | ForEach-Object {
                Write-Host "  $($_.Name): " -NoNewline -ForegroundColor Yellow
                Write-Host $_.Value.value -ForegroundColor White
            }
            
            # Store API URL for APIM deployment
            if ($stack.outputs.apiUrl) {
                $script:ApiUrl = $stack.outputs.apiUrl.value
            }
        }
    } catch {
        Write-Warning "Could not retrieve stack outputs: $_"
    }
}

# =============================================================================
# Deploy API Management (Optional)
# =============================================================================

if ($DeployApiManagement) {
    Write-Header "Deploying API Management"
    
    # Use the API URL from stack outputs if not provided
    if ([string]::IsNullOrWhiteSpace($ApiUrl) -and $script:ApiUrl) {
        $ApiUrl = $script:ApiUrl
        Write-Info "Using API URL from stack outputs: $ApiUrl"
    }
    
    # Get tenant ID if not provided
    if ([string]::IsNullOrWhiteSpace($TenantId)) {
        $TenantId = (az account show --query tenantId --output tsv)
        Write-Info "Using current tenant ID: $TenantId"
    }
    
    $apimStackName = "$stackName-apim"
    
    $apimCommand = @(
        "az", "stack", "group", "create",
        "--name", $apimStackName,
        "--resource-group", $resourceGroupName,
        "--template-file", $apimBicepPath,
        "--parameters", "prefix=smarthealth-$Environment",
        "--parameters", "location=$Location",
        "--parameters", "apiUrl=$ApiUrl"
    )
    
    if (-not [string]::IsNullOrWhiteSpace($TenantId)) {
        $apimCommand += @("--parameters", "tenantId=$TenantId")
    }
    
    if (-not [string]::IsNullOrWhiteSpace($JwtAudience)) {
        $apimCommand += @("--parameters", "jwtAudience=$JwtAudience")
    }
    
    $apimCommand += @(
        "--action-on-unmanage", "resources=$ActionOnUnmanage",
        "--deny-settings-mode", $(if ($DenyWriteAndDelete) { "denyWriteAndDelete" } else { "none" }),
        "--yes"
    )
    
    if ($WhatIf) {
        Write-Info "[WHAT-IF] Would deploy APIM stack"
        Write-Host ($apimCommand -join " ") -ForegroundColor DarkGray
    } else {
        Write-Info "Deploying APIM... (this may take 30-40 minutes)"
        
        try {
            & $apimCommand[0] $apimCommand[1..($apimCommand.Length - 1)]
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "APIM stack deployed successfully"
            } else {
                throw "APIM deployment failed with exit code: $LASTEXITCODE"
            }
        } catch {
            Write-Error "APIM deployment failed: $_"
            Write-Warning "Main infrastructure is deployed. You can retry APIM deployment separately."
        }
    }
}

# =============================================================================
# Summary
# =============================================================================

Write-Header "Deployment Summary"

Write-Success "Environment: $Environment"
Write-Success "Resource Group: $resourceGroupName"
Write-Success "Stack Name: $stackName"

if (-not $WhatIf) {
    Write-Host "`nTo view stack resources:" -ForegroundColor Yellow
    Write-Host "  az stack group show --name $stackName --resource-group $resourceGroupName" -ForegroundColor Gray
    
    Write-Host "`nTo delete the stack and all resources:" -ForegroundColor Yellow
    Write-Host "  az stack group delete --name $stackName --resource-group $resourceGroupName --action-on-unmanage deleteAll --yes" -ForegroundColor Gray
    
    Write-Host "`nTo export stack template:" -ForegroundColor Yellow
    Write-Host "  az stack group export --name $stackName --resource-group $resourceGroupName" -ForegroundColor Gray
}

Write-Host ""
Write-Success "Deployment completed successfully! 🚀"
Write-Host ""
