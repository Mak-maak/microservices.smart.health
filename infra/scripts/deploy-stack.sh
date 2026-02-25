#!/bin/bash
# =============================================================================
# deploy-stack.sh - Deploy SmartHealth infrastructure using Azure Deployment Stacks
# =============================================================================
# Azure Deployment Stacks provide:
# - Unified resource lifecycle management
# - Automatic cleanup of unmanaged resources
# - Drift detection and prevention
# - Better resource grouping and tracking
# =============================================================================

set -e  # Exit on error

# =============================================================================
# Color Functions
# =============================================================================
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

print_header() {
    echo -e "\n${CYAN}========================================${NC}"
    echo -e "${CYAN} $1${NC}"
    echo -e "${CYAN}========================================${NC}\n"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

# =============================================================================
# Usage
# =============================================================================
usage() {
    cat << EOF
Usage: $0 [OPTIONS]

Deploy SmartHealth infrastructure using Azure Deployment Stacks

Required Options:
    -e, --environment ENV       Environment (dev, staging, prod)
    -l, --location LOCATION     Azure region (default: eastus)
    -p, --password PASSWORD     SQL Server admin password

Optional:
    --deploy-apim              Deploy API Management
    --api-url URL              API URL (required if --deploy-apim)
    --tenant-id ID             Azure tenant ID (auto-detected if not provided)
    --jwt-audience AUDIENCE    JWT audience for APIM
    --action-on-unmanage ACTION Action on unmanaged resources (detach|delete, default: detach)
    --deny-settings            Enable deny write and delete settings
    --what-if                  Show what would be deployed without deploying
    -h, --help                 Show this help message

Examples:
    # Deploy development environment
    $0 -e dev -l eastus -p 'MyStr0ng!Passw0rd'

    # Deploy with APIM
    $0 -e prod -l eastus -p 'MyStr0ng!Passw0rd' --deploy-apim --api-url https://api.smarthealth.com

    # Dry run
    $0 -e staging -l eastus -p 'MyStr0ng!Passw0rd' --what-if

EOF
    exit 1
}

# =============================================================================
# Parse Arguments
# =============================================================================
ENVIRONMENT=""
LOCATION="eastus"
SQL_PASSWORD=""
DEPLOY_APIM=false
API_URL=""
TENANT_ID=""
JWT_AUDIENCE=""
ACTION_ON_UNMANAGE="detach"
DENY_SETTINGS="none"
WHAT_IF=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -l|--location)
            LOCATION="$2"
            shift 2
            ;;
        -p|--password)
            SQL_PASSWORD="$2"
            shift 2
            ;;
        --deploy-apim)
            DEPLOY_APIM=true
            shift
            ;;
        --api-url)
            API_URL="$2"
            shift 2
            ;;
        --tenant-id)
            TENANT_ID="$2"
            shift 2
            ;;
        --jwt-audience)
            JWT_AUDIENCE="$2"
            shift 2
            ;;
        --action-on-unmanage)
            ACTION_ON_UNMANAGE="$2"
            shift 2
            ;;
        --deny-settings)
            DENY_SETTINGS="denyWriteAndDelete"
            shift
            ;;
        --what-if)
            WHAT_IF=true
            shift
            ;;
        -h|--help)
            usage
            ;;
        *)
            print_error "Unknown option: $1"
            usage
            ;;
    esac
done

# Validate required arguments
if [[ -z "$ENVIRONMENT" ]] || [[ -z "$SQL_PASSWORD" ]]; then
    print_error "Missing required arguments"
    usage
fi

if [[ ! "$ENVIRONMENT" =~ ^(dev|staging|prod)$ ]]; then
    print_error "Environment must be one of: dev, staging, prod"
    exit 1
fi

# =============================================================================
# Configuration
# =============================================================================
RESOURCE_GROUP="smarthealth-${ENVIRONMENT}-rg"
STACK_NAME="smarthealth-${ENVIRONMENT}-stack"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MAIN_BICEP_PATH="${SCRIPT_DIR}/../main.bicep"
APIM_BICEP_PATH="${SCRIPT_DIR}/../apim.bicep"

# =============================================================================
# Pre-flight Checks
# =============================================================================
print_header "Pre-flight Checks"

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    print_error "Azure CLI is not installed. Please install from https://aka.ms/installazurecli"
    exit 1
fi

AZ_VERSION=$(az version --query '"azure-cli"' -o tsv)
print_success "Azure CLI version: $AZ_VERSION"

# Check if logged in
if ! az account show &> /dev/null; then
    print_error "Not logged in to Azure. Run 'az login' first."
    exit 1
fi

ACCOUNT_NAME=$(az account show --query user.name -o tsv)
SUBSCRIPTION_NAME=$(az account show --query name -o tsv)
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
print_success "Logged in as: $ACCOUNT_NAME"
print_info "Subscription: $SUBSCRIPTION_NAME ($SUBSCRIPTION_ID)"

# Check if Bicep files exist
if [[ ! -f "$MAIN_BICEP_PATH" ]]; then
    print_error "main.bicep not found at: $MAIN_BICEP_PATH"
    exit 1
fi
print_success "Found main.bicep"

# Validate APIM parameters
if [[ "$DEPLOY_APIM" == true ]]; then
    if [[ -z "$API_URL" ]]; then
        print_error "API URL is required when deploying APIM"
        exit 1
    fi
    if [[ ! -f "$APIM_BICEP_PATH" ]]; then
        print_error "apim.bicep not found at: $APIM_BICEP_PATH"
        exit 1
    fi
    print_success "APIM deployment parameters validated"
fi

# =============================================================================
# Create Resource Group
# =============================================================================
print_header "Creating Resource Group"

if az group exists --name "$RESOURCE_GROUP" | grep -q "true"; then
    print_info "Resource group '$RESOURCE_GROUP' already exists"
else
    print_info "Creating resource group '$RESOURCE_GROUP' in '$LOCATION'"
    
    if [[ "$WHAT_IF" == false ]]; then
        az group create \
            --name "$RESOURCE_GROUP" \
            --location "$LOCATION" \
            --tags \
                application="SmartHealth.Appointments" \
                environment="$ENVIRONMENT" \
                managedBy="azure-deployment-stack" \
                deployedBy="$USER" \
                deployedAt="$(date '+%Y-%m-%d %H:%M:%S')" \
            --output none
        
        print_success "Resource group created"
    else
        print_info "[WHAT-IF] Would create resource group"
    fi
fi

# =============================================================================
# Deploy Infrastructure Stack
# =============================================================================
print_header "Deploying Infrastructure Stack"

print_info "Stack Name: $STACK_NAME"
print_info "Environment: $ENVIRONMENT"
print_info "Location: $LOCATION"
print_info "Action on Unmanage: $ACTION_ON_UNMANAGE"
print_info "Deny Settings: $DENY_SETTINGS"

if [[ "$WHAT_IF" == false ]]; then
    print_info "Deploying infrastructure... (this may take 10-15 minutes)"
    
    if az stack group create \
        --name "$STACK_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --template-file "$MAIN_BICEP_PATH" \
        --parameters environment="$ENVIRONMENT" \
        --parameters sqlAdminPassword="$SQL_PASSWORD" \
        --action-on-unmanage resources="$ACTION_ON_UNMANAGE" \
        --deny-settings-mode "$DENY_SETTINGS" \
        --yes \
        --output json > /tmp/stack-output.json; then
        
        print_success "Infrastructure stack deployed successfully"
    else
        print_error "Deployment failed"
        exit 1
    fi
else
    print_info "[WHAT-IF] Would deploy infrastructure stack"
fi

# =============================================================================
# Get Stack Outputs
# =============================================================================
print_header "Stack Outputs"

if [[ "$WHAT_IF" == false ]]; then
    if STACK_OUTPUT=$(az stack group show \
        --name "$STACK_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --output json 2>/dev/null); then
        
        print_success "Deployment outputs:"
        echo "$STACK_OUTPUT" | jq -r '.outputs | to_entries[] | "  \(.key): \(.value.value)"' || true
        
        # Extract API URL for APIM deployment
        if [[ -z "$API_URL" ]]; then
            API_URL=$(echo "$STACK_OUTPUT" | jq -r '.outputs.apiUrl.value // empty')
            if [[ -n "$API_URL" ]]; then
                print_info "Extracted API URL from outputs: $API_URL"
            fi
        fi
    else
        print_warning "Could not retrieve stack outputs"
    fi
fi

# =============================================================================
# Deploy API Management (Optional)
# =============================================================================
if [[ "$DEPLOY_APIM" == true ]]; then
    print_header "Deploying API Management"
    
    # Get tenant ID if not provided
    if [[ -z "$TENANT_ID" ]]; then
        TENANT_ID=$(az account show --query tenantId -o tsv)
        print_info "Using current tenant ID: $TENANT_ID"
    fi
    
    APIM_STACK_NAME="${STACK_NAME}-apim"
    
    APIM_PARAMS=(
        --name "$APIM_STACK_NAME"
        --resource-group "$RESOURCE_GROUP"
        --template-file "$APIM_BICEP_PATH"
        --parameters prefix="smarthealth-${ENVIRONMENT}"
        --parameters location="$LOCATION"
        --parameters apiUrl="$API_URL"
    )
    
    if [[ -n "$TENANT_ID" ]]; then
        APIM_PARAMS+=(--parameters tenantId="$TENANT_ID")
    fi
    
    if [[ -n "$JWT_AUDIENCE" ]]; then
        APIM_PARAMS+=(--parameters jwtAudience="$JWT_AUDIENCE")
    fi
    
    APIM_PARAMS+=(
        --action-on-unmanage resources="$ACTION_ON_UNMANAGE"
        --deny-settings-mode "$DENY_SETTINGS"
        --yes
    )
    
    if [[ "$WHAT_IF" == false ]]; then
        print_info "Deploying APIM... (this may take 30-40 minutes)"
        
        if az stack group create "${APIM_PARAMS[@]}" --output none; then
            print_success "APIM stack deployed successfully"
        else
            print_error "APIM deployment failed"
            print_warning "Main infrastructure is deployed. You can retry APIM deployment separately."
        fi
    else
        print_info "[WHAT-IF] Would deploy APIM stack"
    fi
fi

# =============================================================================
# Summary
# =============================================================================
print_header "Deployment Summary"

print_success "Environment: $ENVIRONMENT"
print_success "Resource Group: $RESOURCE_GROUP"
print_success "Stack Name: $STACK_NAME"

if [[ "$WHAT_IF" == false ]]; then
    echo -e "\n${YELLOW}To view stack resources:${NC}"
    echo -e "${NC}  az stack group show --name $STACK_NAME --resource-group $RESOURCE_GROUP${NC}"
    
    echo -e "\n${YELLOW}To delete the stack and all resources:${NC}"
    echo -e "${NC}  az stack group delete --name $STACK_NAME --resource-group $RESOURCE_GROUP --action-on-unmanage deleteAll --yes${NC}"
    
    echo -e "\n${YELLOW}To export stack template:${NC}"
    echo -e "${NC}  az stack group export --name $STACK_NAME --resource-group $RESOURCE_GROUP${NC}"
fi

echo ""
print_success "Deployment completed successfully! 🚀"
echo ""
