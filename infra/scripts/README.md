# 🚀 Azure Deployment Stack Scripts

Modern, declarative infrastructure deployment scripts using **Azure Deployment Stacks**.

## 📋 Overview

Azure Deployment Stacks provide:
- ✅ **Unified Lifecycle Management** - Deploy, update, and delete resources as a single unit
- ✅ **Drift Detection** - Automatically detect and prevent resource drift
- ✅ **Automatic Cleanup** - Remove unmanaged resources automatically
- ✅ **Deny Settings** - Prevent unauthorized modifications
- ✅ **Better Tracking** - Clear visibility of what resources belong together

## 📁 Files

| File | Description |
|------|-------------|
| `deploy-stack.ps1` | PowerShell deployment script (Windows) |
| `deploy-stack.sh` | Bash deployment script (Linux/macOS) |
| `destroy-stack.ps1` | PowerShell cleanup script |
| `validate-stack.ps1` | Validation and drift detection script |

## 🔧 Prerequisites

1. **Azure CLI** installed and configured
   ```bash
   # Check installation
   az version
   
   # Login
   az login
   
   # Set subscription
   az account set --subscription "Your-Subscription-Name"
   ```

2. **Bicep CLI** (usually installed with Azure CLI)
   ```bash
   az bicep version
   ```

3. **Required Permissions**
   - Contributor or Owner role on the subscription
   - Ability to create resource groups and deployment stacks

## 🚀 Quick Start

### Development Environment

**Windows (PowerShell):**
```powershell
.\infra\scripts\deploy-stack.ps1 `
    -Environment dev `
    -Location eastus `
    -SqlAdminPassword (ConvertTo-SecureString "YourStr0ng!Passw0rd" -AsPlainText -Force)
```

**Linux/macOS (Bash):**
```bash
chmod +x ./infra/scripts/deploy-stack.sh

./infra/scripts/deploy-stack.sh \
    --environment dev \
    --location eastus \
    --password "YourStr0ng!Passw0rd"
```

### Production Environment with APIM

**PowerShell:**
```powershell
.\infra\scripts\deploy-stack.ps1 `
    -Environment prod `
    -Location eastus `
    -SqlAdminPassword (ConvertTo-SecureString "YourStr0ng!Passw0rd" -AsPlainText -Force) `
    -DeployApiManagement `
    -DenyWriteAndDelete
```

**Bash:**
```bash
./infra/scripts/deploy-stack.sh \
    --environment prod \
    --location eastus \
    --password "YourStr0ng!Passw0rd" \
    --deploy-apim \
    --deny-settings
```

## 📖 Detailed Usage

### Deploy Script Options

#### PowerShell (`deploy-stack.ps1`)

| Parameter | Required | Description | Default |
|-----------|----------|-------------|---------|
| `-Environment` | Yes | Environment (dev, staging, prod) | - |
| `-Location` | Yes | Azure region | eastus |
| `-SqlAdminPassword` | Yes | SQL admin password (SecureString) | - |
| `-DeployApiManagement` | No | Deploy APIM | false |
| `-ApiUrl` | No* | API URL for APIM (*required if APIM) | - |
| `-TenantId` | No | Azure tenant ID | Auto-detected |
| `-JwtAudience` | No | JWT audience for APIM | - |
| `-ActionOnUnmanage` | No | Action on unmanaged resources (detach/delete) | detach |
| `-DenyWriteAndDelete` | No | Enable deny settings | false |
| `-WhatIf` | No | Dry run (no changes) | false |

**Examples:**

```powershell
# Basic deployment
.\deploy-stack.ps1 -Environment dev -Location eastus -SqlAdminPassword $securePassword

# With APIM and protection
.\deploy-stack.ps1 `
    -Environment prod `
    -Location eastus `
    -SqlAdminPassword $securePassword `
    -DeployApiManagement `
    -ApiUrl "https://smarthealth-api.azurecontainerapps.io" `
    -JwtAudience "api://smarthealth" `
    -DenyWriteAndDelete

# Dry run to preview changes
.\deploy-stack.ps1 -Environment staging -Location eastus -SqlAdminPassword $securePassword -WhatIf

# Clean up unmanaged resources
.\deploy-stack.ps1 `
    -Environment dev `
    -Location eastus `
    -SqlAdminPassword $securePassword `
    -ActionOnUnmanage delete
```

#### Bash (`deploy-stack.sh`)

| Parameter | Required | Description | Default |
|-----------|----------|-------------|---------|
| `-e, --environment` | Yes | Environment (dev, staging, prod) | - |
| `-l, --location` | Yes | Azure region | eastus |
| `-p, --password` | Yes | SQL admin password | - |
| `--deploy-apim` | No | Deploy APIM | false |
| `--api-url` | No* | API URL for APIM (*required if APIM) | - |
| `--tenant-id` | No | Azure tenant ID | Auto-detected |
| `--jwt-audience` | No | JWT audience for APIM | - |
| `--action-on-unmanage` | No | Action on unmanaged resources (detach/delete) | detach |
| `--deny-settings` | No | Enable deny settings | false |
| `--what-if` | No | Dry run (no changes) | false |
| `-h, --help` | No | Show help | - |

**Examples:**

```bash
# Make script executable (first time only)
chmod +x ./infra/scripts/deploy-stack.sh

# Basic deployment
./deploy-stack.sh -e dev -l eastus -p "YourStr0ng!Passw0rd"

# With APIM and protection
./deploy-stack.sh \
    -e prod \
    -l eastus \
    -p "YourStr0ng!Passw0rd" \
    --deploy-apim \
    --api-url "https://smarthealth-api.azurecontainerapps.io" \
    --jwt-audience "api://smarthealth" \
    --deny-settings

# Dry run
./deploy-stack.sh -e staging -l eastus -p "YourStr0ng!Passw0rd" --what-if
```

## 🗑️ Cleanup / Destroy

### Delete Stack and Resources

**PowerShell:**
```powershell
# Delete stack only (preserve resource group)
.\infra\scripts\destroy-stack.ps1 -Environment dev

# Delete stack and resource group
.\infra\scripts\destroy-stack.ps1 -Environment dev -DeleteResourceGroup

# Force deletion without confirmation
.\infra\scripts\destroy-stack.ps1 -Environment dev -Force -DeleteResourceGroup

# Dry run
.\infra\scripts\destroy-stack.ps1 -Environment dev -WhatIf
```

**Azure CLI:**
```bash
# Delete stack and all resources
az stack group delete \
    --name smarthealth-dev-stack \
    --resource-group smarthealth-dev-rg \
    --action-on-unmanage deleteAll \
    --yes

# Delete resource group (after stack deletion)
az group delete --name smarthealth-dev-rg --yes
```

## 🔍 Stack Management Commands

### View Stack Information

```bash
# Show stack details
az stack group show \
    --name smarthealth-dev-stack \
    --resource-group smarthealth-dev-rg

# List all resources in stack
az stack group show \
    --name smarthealth-dev-stack \
    --resource-group smarthealth-dev-rg \
    --query 'resources[].{Name:id, Type:resourceType}' \
    --output table

# Get stack outputs
az stack group show \
    --name smarthealth-dev-stack \
    --resource-group smarthealth-dev-rg \
    --query 'outputs'
```

### Export Stack Template

```bash
# Export current template
az stack group export \
    --name smarthealth-dev-stack \
    --resource-group smarthealth-dev-rg \
    --output json > exported-stack.json
```

### Update Stack

Just re-run the deployment script with updated parameters:

```powershell
.\deploy-stack.ps1 -Environment dev -Location eastus -SqlAdminPassword $newPassword
```

The stack will:
- Add new resources
- Update modified resources
- Handle removed resources based on `ActionOnUnmanage` setting

## 🔐 Security Best Practices

### 1. Password Management

**PowerShell - Use SecureString:**
```powershell
# Read password securely
$securePassword = Read-Host "Enter SQL password" -AsSecureString

# Or from file (encrypted)
$securePassword = Get-Content password.txt | ConvertTo-SecureString

# Use in deployment
.\deploy-stack.ps1 -Environment prod -Location eastus -SqlAdminPassword $securePassword
```

**Bash - Use Environment Variable:**
```bash
# Set password as environment variable
export SQL_ADMIN_PASSWORD="YourStr0ng!Passw0rd"

# Use in deployment
./deploy-stack.sh -e prod -l eastus -p "$SQL_ADMIN_PASSWORD"

# Clear variable after use
unset SQL_ADMIN_PASSWORD
```

### 2. Enable Deny Settings (Production)

Prevents unauthorized modifications:

```powershell
# PowerShell
.\deploy-stack.ps1 ... -DenyWriteAndDelete

# Bash
./deploy-stack.sh ... --deny-settings
```

### 3. Use Key Vault for Secrets

After deployment, store sensitive values in Key Vault:

```bash
# Store connection strings
az keyvault secret set \
    --vault-name smarthealth-prod-kv \
    --name SqlConnectionString \
    --value "your-connection-string"
```

## 📊 CI/CD Integration

### GitHub Actions Example

```yaml
name: Deploy Infrastructure

on:
  push:
    branches: [main]
    paths: ['infra/**']

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      
      - name: Deploy Stack
        run: |
          chmod +x ./infra/scripts/deploy-stack.sh
          ./infra/scripts/deploy-stack.sh \
            -e prod \
            -l eastus \
            -p "${{ secrets.SQL_ADMIN_PASSWORD }}" \
            --deploy-apim
```

### Azure DevOps Pipeline

```yaml
trigger:
  branches:
    include:
      - main
  paths:
    include:
      - infra/*

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: AzureCLI@2
    displayName: 'Deploy Infrastructure Stack'
    inputs:
      azureSubscription: 'Azure-Connection'
      scriptType: 'bash'
      scriptLocation: 'scriptPath'
      scriptPath: './infra/scripts/deploy-stack.sh'
      arguments: >
        -e $(environment)
        -l $(location)
        -p $(sqlAdminPassword)
```

## 🆚 Deployment Stack vs. Traditional Deployment

| Feature | Traditional `az deployment` | Deployment Stack |
|---------|----------------------------|------------------|
| Resource Lifecycle | Manual tracking | Automatic |
| Cleanup of Removed Resources | Manual | Automatic |
| Drift Detection | None | Built-in |
| Deny Settings | Manual locks | Built-in |
| Resource Grouping | Implicit | Explicit |
| Update Operations | Complex | Simple |
| Rollback | Manual | Easier |

## ❓ Troubleshooting

### Issue: "Stack already exists"

**Solution:** Update the existing stack by re-running the deployment

### Issue: "Cannot delete stack - deny settings enabled"

**Solution:** Update stack with deny settings disabled first:
```bash
az stack group create \
    --name smarthealth-prod-stack \
    --resource-group smarthealth-prod-rg \
    --template-file infra/main.bicep \
    --deny-settings-mode none \
    --yes
```

### Issue: "Resource group not found"

**Solution:** The script creates the RG automatically. Ensure Azure CLI is logged in.

### Issue: Deployment timeout

**Solution:** APIM deployment can take 30-40 minutes. Use `--no-wait` for background deployment:
```bash
az stack group create ... --no-wait
```

Monitor progress:
```bash
az stack group show --name smarthealth-prod-stack --resource-group smarthealth-prod-rg --query 'provisioningState'
```

## 📚 Additional Resources

- [Azure Deployment Stacks Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/deployment-stacks)
- [Bicep Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [Azure CLI Reference](https://learn.microsoft.com/cli/azure/)

## 🤝 Contributing

To add new scripts:
1. Follow the existing pattern
2. Add comprehensive error handling
3. Include `--what-if` support
4. Update this README

## 📝 License

See main repository LICENSE file.
