# Infrastructure Pipeline Integration

**Date:** June 19, 2026  
**Branch:** `feature/infra-pipeline`

## Overview

This document explains the integration of Terraform infrastructure build and deployment into the Azure DevOps pipeline. The infrastructure code now builds, plans, and deploys alongside the application code in a coordinated, environment-aware manner.

## Problem Statement

Previously, the Azure pipeline only built and deployed the .NET API application. The infrastructure (Azure resources like Service Bus, Key Vault, Application Insights, etc.) was managed separately, creating:
- Manual deployment processes
- Risk of environment inconsistencies
- Difficulty tracking infrastructure changes
- No version control integration for infrastructure deployments

## Solution

We've integrated Terraform deployment into the Azure DevOps pipeline with a **3-stage process**:

1. **Build Stage** - Build and test application code
2. **Terraform Plan Stage** - Validate and plan infrastructure changes for both dev and prod
3. **Deploy Stages** - Deploy infrastructure first, then application code per environment

---

## New Pipeline Architecture

### Stage Dependency Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ Build Stage                                                     │
│ - Build .NET SDK                                                │
│ - Restore & Compile Code                                        │
│ - Run Tests                                                      │
│ - Publish & Archive API                                         │
└─────────────────────┬───────────────────────────────────────────┘
                      │
        ┌─────────────┴─────────────┐
        │                           │
        ▼                           │
┌─────────────────────────────┐    │
│ Terraform_Plan Stage        │    │
│ - Install Terraform 1.9.0   │    │
│ - Init (Azure backend)      │    │
│ - Validate Configuration    │    │
│ - Plan dev environment      │    │
│ - Plan prod environment     │    │
│ - Publish plan artifacts    │    │
└─────────────────────┬───────┘    │
                      │            │
        ┌─────────────┴─────────────┤
        │                           │
        ▼                           │
┌─────────────────────────────┐    │
│ Deploy_Dev Stage            │    │
├─────────────────────────────┤    │
│ DeployInfraDev (Job)        │    │
│ - Apply terraform plan-dev  │    │
└──────┬──────────────────────┘    │
       │                           │
       ▼                           │
│ DeployAppDev (Job)          │    │
│ - Deploy API to dev         │    │
└─────────────────────────────┘    │
       │                           │
       ▼                           │
┌─────────────────────────────┐    │
│ Deploy_Prod Stage           │    │
│ (only on main branch)        │    │
├─────────────────────────────┤    │
│ DeployInfraProd (Job)       │    │
│ - Apply terraform plan-prod │    │
└──────┬──────────────────────┘    │
       │                           │
       ▼                           │
│ DeployAppProd (Job)         │    │
│ - Deploy API to prod        │    │
└─────────────────────────────┘    │
       │                           │
       └───────────────────────────┘
```

---

## Key Changes to azure-pipelines.yml

### 1. New Pipeline Variables

```yaml
TerraformVersion: 1.9.0              # Terraform version requirement
TerraformArtifact: terraform-plans   # Artifact name for tfplans
TerraformWorkingDir: $(Build.SourcesDirectory)/infra/terraform
```

These configure:
- The Terraform version to use
- Where plan files are stored
- The path to Terraform configuration files

---

### 2. Terraform_Plan Stage (NEW)

**Purpose:** Validate infrastructure and plan changes for review before deployment

```yaml
- stage: Terraform_Plan
  displayName: Terraform plan and validate
  dependsOn: Build
  jobs:
    - job: Terraform_Validate_Plan
      steps:
        # Install Terraform
        - task: TerraformInstaller@0
        
        # Connect to Azure backend for state management
        - task: TerraformTaskV4@4
          command: init
          inputs:
            backendServiceArm: $(AzureServiceConnection)
            backendAzureRmResourceGroupName: rg-terraform
            backendAzureRmStorageAccountName: terraform
            backendAzureRmContainerName: tfstate
        
        # Validate syntax
        - task: TerraformTaskV4@4
          command: validate
        
        # Generate plans for dev environment
        - task: TerraformTaskV4@4
          command: plan
          commandOptions: -var="environment=dev" -out=$(Build.ArtifactStagingDirectory)/tfplan-dev
        
        # Generate plans for prod environment
        - task: TerraformTaskV4@4
          command: plan
          commandOptions: -var="environment=prod" -out=$(Build.ArtifactStagingDirectory)/tfplan-prod
        
        # Publish plans as artifacts for review
        - task: PublishPipelineArtifact@1
```

**What it does:**
- ✅ Validates Terraform configuration syntax
- ✅ Generates infrastructure change plans for dev and prod
- ✅ Publishes plans as pipeline artifacts (downloadable for review)
- ✅ Catches configuration errors early
- ✅ Enables audit trail of planned changes

---

### 3. Deploy_Dev Stage (MODIFIED)

**Key Change:** Now includes infrastructure deployment first, then app deployment

```yaml
- stage: Deploy_Dev
  dependsOn:
    - Build
    - Terraform_Plan        # ← Now depends on Terraform_Plan
  jobs:
    # NEW: Infrastructure deployment job
    - deployment: DeployInfraDev
      steps:
        - download: current
          artifact: $(TerraformArtifact)
        
        - task: TerraformInstaller@0
        - task: TerraformTaskV4@4
          command: init
        
        - task: TerraformTaskV4@4
          command: apply
          commandOptions: $(Pipeline.Workspace)/$(TerraformArtifact)/tfplan-dev
    
    # EXISTING: Application deployment job
    - deployment: DeployAppDev
      dependsOn: DeployInfraDev  # ← Wait for infra to complete
      steps:
        - task: AzureWebApp@1
          # Deploy API...
```

**Execution Order:**
1. **DeployInfraDev** - Creates/updates Azure resources (Service Bus, Key Vault, App Service, etc.)
2. **DeployAppDev** - Once infrastructure is ready, deploy the API

---

### 4. Deploy_Prod Stage (MODIFIED)

**Key Change:** Infrastructure deployment before app deployment, but only from main branch

```yaml
- stage: Deploy_Prod
  dependsOn:
    - Deploy_Dev
    - Terraform_Plan
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
  jobs:
    - deployment: DeployInfraProd
      # Applies prod Terraform plan
    
    - deployment: DeployAppProd
      dependsOn: DeployInfraProd
      # Deploys API after infra ready
```

**Key Points:**
- 🔒 Prod stage only runs on `main` branch (production safety)
- 🔄 Infrastructure and app deployments are coordinated
- 📊 Uses prod Terraform plan (different environment variables)

---

## Infrastructure as Code Details

### Terraform Backend Configuration

The pipeline uses **Azure Storage** for Terraform state management:

```yaml
backendServiceArm: sc-orderclassification           # Azure service connection
backendAzureRmResourceGroupName: rg-terraform       # Resource group for state
backendAzureRmStorageAccountName: terraform         # Storage account
backendAzureRmContainerName: tfstate                # Container
backendAzureRmKey: orderclassification.tfstate      # State file key
```

**What this means:**
- Terraform state is stored in Azure (not locally)
- Multiple people can safely run pipeline simultaneously
- State changes are auditable in Azure
- Prevents accidental state conflicts

---

## Terraform Configuration Overview

### Components Managed by Terraform

Your `infra/terraform/main.tf` manages:

1. **Resource Group** - Container for all resources per environment
2. **Service Bus** - Message queue for order events
3. **Key Vault** - Secure storage for secrets (connection strings)
4. **Application Insights** - Application monitoring & logging
5. **Service Plan** - Compute hosting plan (Linux, B1 SKU)
6. **Linux Web App** - The .NET API application
7. **Access Policies** - Security permissions between resources

### Environment-Specific Configurations

The Terraform plan takes an `environment` variable that controls:

| Setting | Dev | Prod |
|---------|-----|------|
| Environment | `dev` | `prod` |
| Location | `australiaeast` | `australiaeast` |
| VM SKU | B1 | B1 |
| Tags | owner: interview-cram | owner: interview-cram |

---

## Pipeline Execution Flow

### Scenario 1: Pull Request on `develop` Branch

```
1. Build stage runs
   ✓ Code compiles, tests pass
   ✓ API artifact published

2. Terraform_Plan stage runs
   ✓ Terraform plan-dev generated
   ✓ Terraform plan-prod generated
   ✓ Plan artifacts published

3. Deploy stages SKIP
   (No deployment on PR)

4. Result: Plans available for review, no deployment
```

### Scenario 2: Push to `develop` Branch

```
1. Build stage ✓
2. Terraform_Plan stage ✓
3. Deploy_Dev stage runs
   - DeployInfraDev: Applies terraform plan-dev
   - DeployAppDev: Deploys API to dev
4. Deploy_Prod stage SKIPS (not on main)
```

### Scenario 3: Push to `main` Branch (Production Release)

```
1. Build stage ✓
2. Terraform_Plan stage ✓
3. Deploy_Dev stage runs
   - DeployInfraDev: Updates dev infrastructure
   - DeployAppDev: Updates dev API
4. Deploy_Prod stage runs
   - DeployInfraProd: Updates prod infrastructure
   - DeployAppProd: Updates prod API
```

---

## Azure Prerequisites

To use this pipeline, ensure these exist in your Azure subscription:

### 1. Terraform State Backend

```bash
# Create resource group
az group create --name rg-terraform --location australiaeast

# Create storage account
az storage account create \
  --name terraform \
  --resource-group rg-terraform \
  --location australiaeast

# Create container
az storage container create \
  --name tfstate \
  --account-name terraform
```

### 2. Azure DevOps Service Connection

The pipeline uses `sc-orderclassification` service connection which should:
- Have permissions to create/modify resources
- Be authorized for the target subscription
- Have the necessary roles (Contributor minimum)

**Setup in Azure DevOps:**
- Project Settings → Service Connections
- Create "Azure Resource Manager" connection
- Name it: `sc-orderclassification`
- Select appropriate subscription and resource group

---

## Security Considerations

### 🔐 Secrets Management

- **No secrets in code** - Connection strings stored in Key Vault
- **No secrets in pipeline** - Use Azure service connections
- **State encryption** - Terraform state encrypted at rest in Azure
- **Access control** - Azure RBAC controls who can deploy

### 🔒 Deployment Gates

- **Dev deployment** - Automatic when code is pushed
- **Prod deployment** - Only from `main` branch (protection against accidental prod changes)
- **Environment approvals** - Can be added in pipeline conditions if needed

### 📋 Audit Trail

Every infrastructure change is:
- Logged in Terraform state file (versioned)
- Tracked in Azure DevOps pipeline run history
- Visible in Azure resource activity logs

---

## Troubleshooting

### Problem: Terraform init fails

**Cause:** Service connection lacks permissions to storage account

**Solution:**
```bash
az role assignment create \
  --assignee <service-principal-id> \
  --role "Storage Blob Data Owner" \
  --scope /subscriptions/<subscription-id>/resourceGroups/rg-terraform/providers/Microsoft.Storage/storageAccounts/terraform
```

### Problem: Plan artifact not found during apply

**Cause:** Plan generation failed in Terraform_Plan stage

**Solution:**
- Check Terraform_Plan stage logs
- Verify Terraform syntax: `terraform validate`
- Check Azure credentials in service connection

### Problem: Resource already exists error

**Cause:** Resource created outside pipeline or stale state

**Solution:**
- Check if resource exists in Azure Portal
- If it should be deleted: `terraform destroy`
- If it exists and is correct: Update Terraform state

---

## Best Practices Now in Place

✅ **Infrastructure and Code Together**
- Both versioned and deployed together
- Consistency across deployments

✅ **Plan Before Apply**
- Review what will change before deployment
- Terraform plans are artifacts for audit

✅ **Environment Isolation**
- Separate plans/deployments for dev vs prod
- Environment-specific variables

✅ **Automated Deployments**
- No manual steps required
- Repeatable, consistent, auditable

✅ **State Management**
- Centralized state in Azure Storage
- Safe for team collaboration

---

## Future Enhancements

Potential improvements to consider:

1. **Manual Approval Gates** - Require approval before prod deployment
2. **Cost Estimation** - Include Terraform cost estimates in plan output
3. **Drift Detection** - Periodic checks for infrastructure changes outside pipeline
4. **Automated Testing** - Add Terraform test framework (Terratest)
5. **Tagging Strategy** - Enforce consistent tagging via Terraform
6. **Backup Strategy** - Automated backups of Key Vault secrets

---

## Summary

The infrastructure pipeline integration provides:

| Aspect | Benefit |
|--------|---------|
| **Automation** | No manual resource creation |
| **Consistency** | Same process every deployment |
| **Auditability** | Full history of changes |
| **Safety** | Plans reviewed before apply |
| **Scalability** | Easy to add new environments |
| **Reliability** | Repeatable infrastructure code |

Your OrderClassification API now has a complete CI/CD pipeline that manages both application and infrastructure deployments in a coordinated, auditable manner! 🚀

---

## Related Documentation

- [Terraform Documentation](https://www.terraform.io/docs)
- [Azure DevOps Terraform Tasks](https://learn.microsoft.com/en-us/azure/devops/pipelines/tasks/deploy/terraform)
- [Azure Provider for Terraform](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs)
- Your project's `infra/terraform/` directory for implementation details

