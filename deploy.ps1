# Complete MicroCMS Deployment Script
# This script handles both publishing and IIS configuration
# Administrator rights are only required for IIS configuration

param(
    [string]$Environment = "Production",
    [hashtable]$EnvironmentVariables = @{},
    [string]$BaseOutputPath = "C:\inetpub",
    [switch]$SkipPublish = $false,
    [switch]$SkipIISConfig = $false,
    [switch]$UseSubApplications = $false,
    [int]$WebHostPort = 5000,
    [int]$AdminPort = 5001,
    [int]$DeliveryPort = 5002,
    [int]$SiteHostPort = 5003
)

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

# Validate admin requirement
if (-not $SkipIISConfig -and -not $isAdmin) {
    Write-Host "`nAdministrator Rights Required" -ForegroundColor Yellow
    Write-Host "============================================================`n" -ForegroundColor Yellow
    Write-Host "IIS configuration requires Administrator privileges." -ForegroundColor White
    Write-Host "`nYou have two options:`n" -ForegroundColor White
    Write-Host "Option 1: Run PowerShell as Administrator" -ForegroundColor Cyan
    Write-Host "  - Right-click PowerShell" -ForegroundColor Gray
    Write-Host "  - Select 'Run as Administrator'" -ForegroundColor Gray
    Write-Host "  - Run: .\deploy.ps1`n" -ForegroundColor Gray

    Write-Host "Option 2: Skip IIS configuration - publish files only" -ForegroundColor Cyan
    Write-Host "  - Run: .\deploy.ps1 -SkipIISConfig" -ForegroundColor Gray
    Write-Host "  - Then configure IIS manually or run configure-iis.ps1 as admin later`n" -ForegroundColor Gray

    Write-Host "Would you like to continue with publish only - skip IIS? [Y/N]: " -ForegroundColor Yellow -NoNewline
    $response = Read-Host

    if ($response -eq 'Y' -or $response -eq 'y') {
        Write-Host "`nContinuing with publish only - IIS configuration skipped`n" -ForegroundColor Green
        $SkipIISConfig = $true
    } else {
        Write-Host "`nDeployment cancelled. Please run PowerShell as Administrator.`n" -ForegroundColor Red
        exit 1
    }
}

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

Write-ColorOutput "`n============================================================" "Cyan"
Write-ColorOutput "     MicroCMS Complete Deployment Script" "Cyan"
Write-ColorOutput "============================================================`n" "Cyan"

$scriptPath = $PSScriptRoot
$deploySuccess = $true

# Step 1: Publish Applications
if (-not $SkipPublish) {
    Write-ColorOutput "=== Phase 1: Publishing Applications ===`n" "Cyan"

    $publishScript = Join-Path $scriptPath "publish_all.ps1"

    if (-not (Test-Path $publishScript)) {
        Write-ColorOutput "Error: publish_all.ps1 not found at $publishScript" "Red"
        exit 1
    }

    $publishParams = @{
        Environment = $Environment
        BaseOutputPath = $BaseOutputPath
    }

    if ($EnvironmentVariables.Count -gt 0) {
        $publishParams.EnvironmentVariables = $EnvironmentVariables
    }

    try {
        & $publishScript @publishParams

        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput "`nPublishing failed with exit code: $LASTEXITCODE" "Red"
            $deploySuccess = $false
        }
    }
    catch {
        Write-ColorOutput "`nPublishing failed: $_" "Red"
        $deploySuccess = $false
    }

    if (-not $deploySuccess) {
        Write-ColorOutput "`nDeployment aborted due to publishing errors." "Red"
        exit 1
    }

    Write-ColorOutput "`nPublishing completed successfully`n" "Green"
} else {
    Write-ColorOutput "Skipping publish phase - using existing published files`n" "Yellow"
}

# Step 2: Configure IIS
if (-not $SkipIISConfig) {
    Write-ColorOutput "=== Phase 2: Configuring IIS ===`n" "Cyan"

    $iisScript = Join-Path $scriptPath "configure-iis.ps1"

    if (-not (Test-Path $iisScript)) {
        Write-ColorOutput "Error: configure-iis.ps1 not found at $iisScript" "Red"
        exit 1
    }

    $iisParams = @{
        BasePhysicalPath = Join-Path $BaseOutputPath "micro_cms"
        WebHostPort = $WebHostPort
        AdminPort = $AdminPort
        DeliveryPort = $DeliveryPort
        SiteHostPort = $SiteHostPort
    }

    if ($UseSubApplications) {
        $iisParams.UseSubApplications = $true
    }

    try {
        & $iisScript @iisParams

        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput "`nIIS configuration failed with exit code: $LASTEXITCODE" "Red"
            $deploySuccess = $false
        }
    }
    catch {
        Write-ColorOutput "`nIIS configuration failed: $_" "Red"
        $deploySuccess = $false
    }

    if (-not $deploySuccess) {
        Write-ColorOutput "`nDeployment completed with IIS configuration errors." "Yellow"
        exit 1
    }

    Write-ColorOutput "`nIIS configuration completed successfully`n" "Green"
} else {
    Write-ColorOutput "Skipping IIS configuration phase`n" "Yellow"
}

# Final Summary
Write-ColorOutput "============================================================" "Green"
Write-ColorOutput "           Deployment Completed Successfully" "Green"
Write-ColorOutput "============================================================`n" "Green"

if ($UseSubApplications) {
    Write-ColorOutput "Applications are available at:" "Cyan"
    Write-ColorOutput "  - CMS WebHost:   http://localhost/cms" "White"
    Write-ColorOutput "  - Admin:         http://localhost/admin" "White"
    Write-ColorOutput "  - Delivery:      http://localhost/delivery" "White"
    Write-ColorOutput "  - Site Host:     http://localhost/site" "White"
} else {
    Write-ColorOutput "Applications are available at:" "Cyan"
    Write-ColorOutput "  - CMS WebHost:   http://localhost:$WebHostPort" "White"
    Write-ColorOutput "  - Admin:         http://localhost:$AdminPort" "White"
    Write-ColorOutput "  - Delivery:      http://localhost:$DeliveryPort" "White"
    Write-ColorOutput "  - Site Host:     http://localhost:$SiteHostPort" "White"
}

Write-ColorOutput "`nEnvironment: $Environment" "Gray"
Write-ColorOutput "Location: $BaseOutputPath\micro_cms\*`n" "Gray"

Write-ColorOutput "Important Notes:" "Yellow"
Write-ColorOutput "  - Verify each application loads correctly in a browser" "Gray"
Write-ColorOutput "  - Check Event Viewer if any application fails to start" "Gray"
Write-ColorOutput "  - Update connection strings and secrets as needed" "Gray"
Write-ColorOutput "  - Configure SSL/TLS for production environments" "Gray"
Write-ColorOutput "  - Set up monitoring and logging" "Gray"

exit 0
