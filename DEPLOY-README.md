# Windows IIS Deployment Scripts

This directory contains PowerShell scripts for deploying MicroCMS to Windows IIS.

## Files

- **`publish_all.ps1`** - Main deployment script
- **`configure-iis.ps1`** - Automated IIS configuration script
- **`deploy-examples.ps1`** - Usage examples and documentation
- **`deploy-production.ps1`** - Template for production environment variables
- **`DEPLOY-README.md`** - This file

## Quick Start

### Basic Deployment (Production)
```powershell
.\publish_all.ps1
```

This will:
- Publish all 4 MicroCMS projects to `C:\inetpub\micro_cms\*`
- Use Release configuration
- Target win-x64 runtime
- Set ASPNETCORE_ENVIRONMENT to "Production"

### Deployment with Environment Variables

#### Method 1: Inline Parameters
```powershell
.\publish_all.ps1 -Environment "Production" -EnvironmentVariables @{
    ConnectionStrings__DefaultConnection = "Server=myserver;Database=microcms;Integrated Security=true"
    Logging__LogLevel__Default = "Warning"
}
```

#### Method 2: Using Configuration File
```powershell
# 1. Copy and edit the template
Copy-Item deploy-production.ps1 deploy-production-myenv.ps1
# Edit deploy-production-myenv.ps1 with your settings

# 2. Load and deploy
. .\deploy-production-myenv.ps1
.\publish_all.ps1 -EnvironmentVariables $ProductionVars
```

#### Method 3: One-liner (Linux/Mac style)
For those familiar with bash-style inline environment variables:
```bash
# In bash/sh (your original approach):
ASPNETCORE_ENVIRONMENT=Production dotnet publish ...

# PowerShell equivalent:
$env:ASPNETCORE_ENVIRONMENT="Production"; dotnet publish ...
```

However, for IIS deployments, it's better to set them in `web.config` (which our script does automatically).

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Environment` | string | "Production" | ASP.NET Core environment (Development, Staging, Production) |
| `EnvironmentVariables` | hashtable | @{} | Additional environment variables to set in web.config |
| `PublishConfig` | string | "Release" | Build configuration (Debug, Release) |
| `Runtime` | string | "win-x64" | Target runtime identifier |
| `SelfContained` | bool | true | Whether to create a self-contained deployment |
| `BaseOutputPath` | string | "C:\inetpub" | Base output directory |

## Environment Variables

Environment variables are automatically injected into the `web.config` file after publishing. This is the standard way to configure ASP.NET Core applications in IIS.

### Common Variables

```powershell
@{
    # Environment
    ASPNETCORE_ENVIRONMENT = "Production"

    # Database
    ConnectionStrings__DefaultConnection = "Server=...;Database=...;"

    # Logging
    Logging__LogLevel__Default = "Warning"
    Logging__LogLevel__Microsoft = "Warning"

    # Security
    AllowedHosts = "yourdomain.com"

    # Custom App Settings
    # Use double underscores (__) for nested JSON paths
    MyApp__Setting__NestedValue = "value"
}
```

### How It Works

1. The script publishes your application using `dotnet publish`
2. After publishing, it locates the generated `web.config` file
3. It injects environment variables into the `<aspNetCore><environmentVariables>` section
4. IIS reads these variables and passes them to your application

**Example web.config section:**
```xml
<aspNetCore processPath="dotnet" arguments=".\MicroCMS.WebHost.dll">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
    <environmentVariable name="ConnectionStrings__DefaultConnection" value="Server=..." />
  </environmentVariables>
</aspNetCore>
```

## Output Structure

```
C:\inetpub\micro_cms\
├── cms_webhost\         (MicroCMS.WebHost)
├── cms_admin\           (MicroCMS.Admin.WebHost)
├── delivery_webhost\    (MicroCMS.Delivery.WebHost)
└── site_host\           (MicroCMS.SiteHost)
```

## Post-Deployment Steps

After running the script, you need to configure IIS. You can do this manually or use the automated script.

### Automated IIS Configuration (Recommended)

Use the `configure-iis.ps1` script to automatically set up IIS:

```powershell
# Run as Administrator
# Option 1: Create separate websites (each on its own port)
.\configure-iis.ps1

# Option 2: Create as sub-applications under Default Web Site
.\configure-iis.ps1 -UseSubApplications

# Option 3: Custom configuration
.\configure-iis.ps1 -WebHostPort 8080 -AdminPort 8081 -DeliveryPort 8082 -SiteHostPort 8083
```

This script will:
- Create and configure 4 application pools for .NET 8
- Set proper permissions for IIS_IUSRS
- Create websites or applications in IIS
- Start all application pools and sites

### Manual IIS Configuration

If you prefer to configure IIS manually:

### 1. Create Application Pools
```powershell
# Run as Administrator
Import-Module WebAdministration

New-WebAppPool -Name "MicroCMS_WebHost" -Force
New-WebAppPool -Name "MicroCMS_Admin" -Force
New-WebAppPool -Name "MicroCMS_Delivery" -Force
New-WebAppPool -Name "MicroCMS_SiteHost" -Force

# Set .NET CLR Version to "No Managed Code" (for .NET 8)
Set-ItemProperty IIS:\AppPools\MicroCMS_WebHost -Name managedRuntimeVersion -Value ""
Set-ItemProperty IIS:\AppPools\MicroCMS_Admin -Name managedRuntimeVersion -Value ""
Set-ItemProperty IIS:\AppPools\MicroCMS_Delivery -Name managedRuntimeVersion -Value ""
Set-ItemProperty IIS:\AppPools\MicroCMS_SiteHost -Name managedRuntimeVersion -Value ""
```

### 2. Create IIS Websites/Applications
```powershell
# Example: Create website for WebHost
New-Website -Name "MicroCMS.WebHost" `
    -PhysicalPath "C:\inetpub\micro_cms\cms_webhost" `
    -ApplicationPool "MicroCMS_WebHost" `
    -Port 80

# Or create as applications under Default Web Site
New-WebApplication -Name "cms" `
    -Site "Default Web Site" `
    -PhysicalPath "C:\inetpub\micro_cms\cms_webhost" `
    -ApplicationPool "MicroCMS_WebHost"
```

### 3. Set Permissions
```powershell
# Grant IIS_IUSRS read/execute permissions
$acl = Get-Acl "C:\inetpub\micro_cms"
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "IIS_IUSRS", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
)
$acl.SetAccessRule($rule)
Set-Acl "C:\inetpub\micro_cms" $acl
```

### 4. Install ASP.NET Core Hosting Bundle
If not already installed, download and install:
- [ASP.NET Core Runtime & Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0)

After installation, restart IIS:
```powershell
iisreset
```

## Examples

See `deploy-examples.ps1` for comprehensive usage examples.

## Troubleshooting

### PowerShell Execution Policy Error
**Error:** "cannot be loaded because running scripts is disabled on this system"

**Solution (choose one):**
```powershell
# Option 1: Unblock the scripts (recommended, no admin needed)
Get-ChildItem -Path . -Filter *.ps1 | Unblock-File

# Option 2: Run with bypass (one-time)
PowerShell -ExecutionPolicy Bypass -File .\deploy.ps1

# Option 3: Set policy for current user (permanent)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Option 4: Set policy system-wide (requires Administrator)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope LocalMachine
```

**Why this happens:** Windows downloads scripts with a security flag that blocks execution. The Unblock-File command removes this flag.

### Application doesn't start
1. Check Event Viewer → Windows Logs → Application
2. Enable stdout logging in web.config:
   ```xml
   <aspNetCore stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout">
   ```
3. Verify Application Pool is running
4. Check file permissions (IIS_IUSRS needs access)

### Environment variables not working
- Verify web.config has the `<environmentVariables>` section
- Check that IIS has been restarted after changes
- Ensure Application Pool is set to "No Managed Code"

### Connection string errors
- Remember to use double underscores `__` for nested config
- Escape special characters in connection strings
- Verify SQL Server allows connections from the IIS server

## Security Notes

- **Never commit `deploy-production.ps1` with real credentials to source control**
- Use Azure Key Vault, AWS Secrets Manager, or Windows Credential Manager for production secrets
- Consider using managed identities for Azure SQL connections
- The script automatically masks sensitive values in console output

## Comparison with Shell Script

**Original `publish_all.sh` (macOS/Linux):**
```bash
ASPNETCORE_ENVIRONMENT=Production dotnet publish ...
```

**PowerShell equivalent:**
```powershell
.\publish_all.ps1 -Environment "Production"
```

The PowerShell script improves upon the original by:
- Automatically configuring `web.config` for IIS
- Supporting multiple environment variables
- Providing colored output and progress tracking
- Including error handling and validation
- Adding post-deployment guidance

## License

Part of MicroCMS project
