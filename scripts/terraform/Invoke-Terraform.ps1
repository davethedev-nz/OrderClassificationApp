param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Plan', 'Apply')]
    [string]$Operation,

    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'prod')]
    [string]$Environment,

    [Parameter(Mandatory = $true)]
    [string]$WorkingDir,

    [Parameter(Mandatory = $true)]
    [string]$TerraformVersion,

    [Parameter(Mandatory = $true)]
    [string]$BackendResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]$BackendStorageAccountName,

    [Parameter(Mandatory = $true)]
    [string]$BackendContainerName,

    [Parameter(Mandatory = $true)]
    [string]$BackendKey,

    [string]$PlanFile
)

$ErrorActionPreference = 'Stop'

function Install-Terraform {
    $tempRoot = if ($env:AGENT_TEMPDIRECTORY) { $env:AGENT_TEMPDIRECTORY } else { [System.IO.Path]::GetTempPath() }
    $installDir = Join-Path $tempRoot 'terraform'
    $terraformExe = Join-Path $installDir 'terraform.exe'

    if (-not (Test-Path $terraformExe)) {
        New-Item -ItemType Directory -Force -Path $installDir | Out-Null
        $zipPath = Join-Path $installDir 'terraform.zip'
        $downloadUrl = "https://releases.hashicorp.com/terraform/$TerraformVersion/terraform_${TerraformVersion}_windows_amd64.zip"

        Write-Host "Downloading Terraform $TerraformVersion from $downloadUrl"
        Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath
        Expand-Archive -Path $zipPath -DestinationPath $installDir -Force
    }

    $env:PATH = "$installDir;$env:PATH"
}

function Set-TerraformAuthEnv {
    $env:ARM_CLIENT_ID = $env:servicePrincipalId
    $env:ARM_CLIENT_SECRET = $env:servicePrincipalKey
    $env:ARM_TENANT_ID = $env:tenantId
    $env:ARM_SUBSCRIPTION_ID = (az account show --query id -o tsv)
}

function Invoke-TerraformInit {
    terraform init `
        -backend-config="resource_group_name=$BackendResourceGroupName" `
        -backend-config="storage_account_name=$BackendStorageAccountName" `
        -backend-config="container_name=$BackendContainerName" `
        -backend-config="key=$BackendKey" `
        -backend-config="use_azuread_auth=true"
}

Install-Terraform
Set-TerraformAuthEnv

Push-Location $WorkingDir
try {
    terraform version
    Invoke-TerraformInit

    switch ($Operation) {
        'Plan' {
            terraform validate
            if (-not $PlanFile) {
                throw 'PlanFile is required when Operation is Plan.'
            }

            terraform plan -var="environment=$Environment" -out=$PlanFile
        }
        'Apply' {
            if (-not $PlanFile) {
                throw 'PlanFile is required when Operation is Apply.'
            }

            terraform apply -auto-approve $PlanFile
        }
    }
}
finally {
    Pop-Location
}

