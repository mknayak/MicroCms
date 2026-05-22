# IIS Configuration Script for MicroCMS
# This script automates IIS setup after deployment
# Must be run as Administrator

#Requires -RunAsAdministrator

param(
    [string]$BasePhysicalPath = "C:\inetpub\micro_cms",
    [int]$WebHostPort = 5000,
    [int]$AdminPort = 5001,
    [int]$DeliveryPort = 5002,
    [int]$SiteHostPort = 5003,
    [switch]$UseSubApplications = $false,
    [string]$ParentSite = "Default Web Site"
)

Import-Module WebAdministration

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Ensure-ApplicationPool {
    param(
        [string]$Name
    )

    if (Test-Path "IIS:\AppPools\$Name") {
        Write-ColorOutput "  Application pool '$Name' already exists" "Yellow"
    } else {
        New-WebAppPool -Name $Name -Force | Out-Null
        Write-ColorOutput "  Created application pool: $Name" "Green"
    }

    # Configure for .NET 8 (No Managed Code)
    Set-ItemProperty "IIS:\AppPools\$Name" -Name managedRuntimeVersion -Value ""

    # Set to Integrated Pipeline
    Set-ItemProperty "IIS:\AppPools\$Name" -Name managedPipelineMode -Value "Integrated"

    # Set Identity (ApplicationPoolIdentity is recommended)
    Set-ItemProperty "IIS:\AppPools\$Name" -Name processModel.identityType -Value "ApplicationPoolIdentity"

    Write-ColorOutput "  Configured '$Name' for .NET 8" "Gray"
}

function Set-DirectoryPermissions {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        Write-ColorOutput "  Warning: Path not found: $Path" "Yellow"
        return
    }

    $acl = Get-Acl $Path
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "IIS_IUSRS", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
    )
    $acl.SetAccessRule($rule)
    Set-Acl $Path $acl

    Write-ColorOutput "  Set permissions for IIS_IUSRS on $Path" "Gray"
}

Write-ColorOutput "`n=== MicroCMS IIS Configuration ===" "Cyan"
Write-ColorOutput "Base Path: $BasePhysicalPath" "Cyan"
Write-ColorOutput "Mode: $(if ($UseSubApplications) { 'Sub-Applications' } else { 'Separate Websites' })`n" "Cyan"

# Define applications
$applications = @(
    @{
        Name = "MicroCMS.WebHost"
        AppPoolName = "MicroCMS_WebHost"
        PhysicalPath = Join-Path $BasePhysicalPath "cms_webhost"
        Port = $WebHostPort
        AppPath = "/cms"
    },
    @{
        Name = "MicroCMS.Admin"
        AppPoolName = "MicroCMS_Admin"
        PhysicalPath = Join-Path $BasePhysicalPath "cms_admin"
        Port = $AdminPort
        AppPath = "/admin"
    },
    @{
        Name = "MicroCMS.Delivery"
        AppPoolName = "MicroCMS_Delivery"
        PhysicalPath = Join-Path $BasePhysicalPath "delivery_webhost"
        Port = $DeliveryPort
        AppPath = "/delivery"
    },
    @{
        Name = "MicroCMS.SiteHost"
        AppPoolName = "MicroCMS_SiteHost"
        PhysicalPath = Join-Path $BasePhysicalPath "site_host"
        Port = $SiteHostPort
        AppPath = "/site"
    }
)

# Step 1: Create Application Pools
Write-ColorOutput "Step 1: Creating Application Pools..." "Cyan"
foreach ($app in $applications) {
    Ensure-ApplicationPool -Name $app.AppPoolName
}

# Step 2: Set Permissions
Write-ColorOutput "`nStep 2: Setting File Permissions..." "Cyan"
foreach ($app in $applications) {
    Set-DirectoryPermissions -Path $app.PhysicalPath
}

# Step 3: Create Websites or Applications
Write-ColorOutput "`nStep 3: Creating IIS Sites..." "Cyan"

if ($UseSubApplications) {
    # Create as sub-applications under existing website
    foreach ($app in $applications) {
        $appName = $app.AppPath.TrimStart('/')
        $fullPath = "$ParentSite$($app.AppPath)"

        if (Test-Path "IIS:\Sites\$ParentSite$($app.AppPath)") {
            Write-ColorOutput "  Application '$appName' already exists under $ParentSite" "Yellow"
            Remove-WebApplication -Name $appName -Site $ParentSite
        }

        New-WebApplication -Name $appName `
            -Site $ParentSite `
            -PhysicalPath $app.PhysicalPath `
            -ApplicationPool $app.AppPoolName `
            -Force | Out-Null

        Write-ColorOutput "  Created: $ParentSite$($app.AppPath) -> $($app.PhysicalPath)" "Green"
    }
} else {
    # Create as separate websites
    foreach ($app in $applications) {
        if (Test-Path "IIS:\Sites\$($app.Name)") {
            Write-ColorOutput "  Website '$($app.Name)' already exists" "Yellow"
            Remove-Website -Name $app.Name
        }

        New-Website -Name $app.Name `
            -PhysicalPath $app.PhysicalPath `
            -ApplicationPool $app.AppPoolName `
            -Port $app.Port `
            -Force | Out-Null

        Write-ColorOutput "  Created: $($app.Name) on port $($app.Port) -> $($app.PhysicalPath)" "Green"
    }
}

# Step 4: Start Application Pools and Sites
Write-ColorOutput "`nStep 4: Starting Application Pools and Sites..." "Cyan"
foreach ($app in $applications) {
    Start-WebAppPool -Name $app.AppPoolName -ErrorAction SilentlyContinue
    Write-ColorOutput "  Started application pool: $($app.AppPoolName)" "Gray"
}

if (-not $UseSubApplications) {
    foreach ($app in $applications) {
        Start-Website -Name $app.Name -ErrorAction SilentlyContinue
        Write-ColorOutput "  Started website: $($app.Name)" "Gray"
    }
}

# Summary
Write-ColorOutput "`n=== Configuration Complete ===" "Green"

if ($UseSubApplications) {
    Write-ColorOutput "`nApplications created under '$ParentSite':" "Cyan"
    foreach ($app in $applications) {
        $url = "http://localhost$($app.AppPath)"
        Write-ColorOutput "  $($app.Name): $url" "White"
    }
} else {
    Write-ColorOutput "`nWebsites created:" "Cyan"
    foreach ($app in $applications) {
        $url = "http://localhost:$($app.Port)"
        Write-ColorOutput "  $($app.Name): $url" "White"
    }
}

Write-ColorOutput "`nNext steps:" "Yellow"
Write-ColorOutput "  1. Verify each application is running correctly" "Gray"
Write-ColorOutput "  2. Configure SSL certificates if needed" "Gray"
Write-ColorOutput "  3. Update DNS/host bindings for production" "Gray"
Write-ColorOutput "  4. Configure firewall rules for the ports" "Gray"
Write-ColorOutput "  5. Test the applications in a browser" "Gray"

Write-ColorOutput "`nUseful commands:" "Yellow"
Write-ColorOutput "  iisreset - Restart IIS" "Gray"
Write-ColorOutput "  Get-Website - List all websites" "Gray"
Write-ColorOutput "  Get-WebApplication - List all applications" "Gray"
Write-ColorOutput "  Get-WebAppPoolState <name> - Check app pool status" "Gray"
