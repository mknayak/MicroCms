# Windows IIS Deployment Scripts - Summary

## 📦 What's Been Created

You now have a complete, production-ready deployment solution for MicroCMS on Windows IIS, equivalent to your shell script but with enhanced features for IIS environments.

### Scripts Created (5 files)

| Script | Purpose | Lines | Size |
|--------|---------|-------|------|
| **`deploy.ps1`** | 🎯 **Main deployment script** - One command to do everything | 150 | 6 KB |
| **`publish_all.ps1`** | 📦 Publish all projects with environment variable support | 226 | 7 KB |
| **`configure-iis.ps1`** | ⚙️ Automated IIS configuration | 196 | 6 KB |
| **`deploy-production.ps1`** | 📝 Template for production environment variables | 37 | 1 KB |
| **`deploy-examples.ps1`** | 📚 Usage examples and documentation | 81 | 3 KB |

### Documentation Created (3 files)

| Document | Purpose |
|----------|---------|
| **`QUICKSTART.md`** | 🚀 Get started in 5 minutes |
| **`DEPLOY-README.md`** | 📖 Comprehensive documentation |

## 🎯 Quick Start

### Simplest Deployment (One Command)
```powershell
# Run as Administrator
.\deploy.ps1
```

### With Environment Variables (Like Your Shell Script)
**Your original shell script approach:**
```bash
ASPNETCORE_ENVIRONMENT=Production dotnet publish ...
```

**PowerShell equivalent:**
```powershell
.\deploy.ps1 -Environment "Production" -EnvironmentVariables @{
    ConnectionStrings__DefaultConnection = "Server=myserver;Database=microcms;..."
    Logging__LogLevel__Default = "Warning"
}
```

## 🔄 Migration from Shell Script

### Before (macOS/Linux - publish_all.sh)
```bash
(cd ./src/MicroCMS.WebHost  &&  dotnet publish -c Release -r osx-arm64 --self-contained -o ~/websites/micro_cms/cms_webhost ) &
(cd ./src/MicroCMS.Admin.WebHost  &&  dotnet publish -c Release -r osx-arm64 --self-contained -o ~/websites/micro_cms/cms_admin ) &
(cd ./src/MicroCMS.Delivery.WebHost  &&  dotnet publish -c Release -r osx-arm64 --self-contained -o ~/websites/micro_cms/delivery_webhost ) & 
(cd ./src/MicroCMS.SiteHost  &&  dotnet publish -c Release -r osx-arm64 --self-contained -o ~/websites/micro_cms/site_host ) &
```

### After (Windows - deploy.ps1)
```powershell
.\deploy.ps1
```

### Key Improvements

✅ **Automatic parallel publishing** (like `&` in bash)  
✅ **IIS configuration included** (no manual setup needed)  
✅ **Environment variables in web.config** (IIS standard)  
✅ **Color-coded output** (easy to track progress)  
✅ **Error handling** (better debugging)  
✅ **Sensitive data masking** (security)  
✅ **Windows-specific** (`win-x64` runtime, `C:\inetpub`)  
✅ **Production-ready** (includes post-deployment guidance)

## 📊 Comparison Table

| Feature | Shell Script | PowerShell Scripts |
|---------|-------------|-------------------|
| Parallel Publishing | ✅ (using `&`) | ✅ (using jobs) |
| Windows Runtime | ❌ | ✅ |
| IIS Configuration | ❌ Manual | ✅ Automated |
| Environment Variables | ⚠️ Process-level | ✅ web.config injection |
| Error Handling | ⚠️ Basic | ✅ Comprehensive |
| Progress Tracking | ❌ | ✅ |
| Color Output | ⚠️ Limited | ✅ Full |
| Documentation | ❌ | ✅ Extensive |

## 🎨 Environment Variables: Shell vs PowerShell

### Shell Script Style (Inline)
```bash
ASPNETCORE_ENVIRONMENT=Production \
DATABASE_URL="Server=..." \
API_KEY="secret" \
dotnet publish ...
```

### PowerShell Equivalent (Process Environment)
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:DATABASE_URL = "Server=..."
$env:API_KEY = "secret"
dotnet publish ...
```

### PowerShell for IIS (Recommended)
```powershell
.\deploy.ps1 -EnvironmentVariables @{
    ASPNETCORE_ENVIRONMENT = "Production"
    DATABASE_URL = "Server=..."
    API_KEY = "secret"
}
```

The IIS method injects variables into `web.config`:
```xml
<aspNetCore processPath="dotnet" arguments=".\app.dll">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
    <environmentVariable name="DATABASE_URL" value="Server=..." />
  </environmentVariables>
</aspNetCore>
```

## 📝 Common Usage Examples

### Example 1: Basic Deployment
```powershell
.\deploy.ps1
```

### Example 2: Staging Environment
```powershell
.\deploy.ps1 -Environment "Staging"
```

### Example 3: With Connection Strings
```powershell
.\deploy.ps1 -EnvironmentVariables @{
    ConnectionStrings__DefaultConnection = "Server=prod-sql;Database=MicroCMS;Integrated Security=true"
}
```

### Example 4: Sub-Applications (Under Default Web Site)
```powershell
.\deploy.ps1 -UseSubApplications
```
Results in:
- http://localhost/cms
- http://localhost/admin
- http://localhost/delivery
- http://localhost/site

### Example 5: Custom Ports
```powershell
.\deploy.ps1 -WebHostPort 8080 -AdminPort 8081
```

### Example 6: Re-configure IIS Only (After Code Changes)
```powershell
.\publish_all.ps1  # Just publish
# No need to reconfigure IIS if already set up
```

### Example 7: Production with Secrets File
```powershell
# 1. Create config file (do this once)
Copy-Item deploy-production.ps1 production-secrets.ps1
# Edit production-secrets.ps1

# 2. Add to .gitignore
Add-Content .gitignore "`nproduction-secrets.ps1"

# 3. Deploy
. .\production-secrets.ps1
.\deploy.ps1 -EnvironmentVariables $ProductionVars
```

## 🔍 What Each Script Does

### deploy.ps1 (Main Script)
- ✅ Orchestrates the entire deployment
- ✅ Calls publish_all.ps1 to build and publish
- ✅ Calls configure-iis.ps1 to set up IIS
- ✅ Provides unified progress reporting
- ✅ Handles errors gracefully

### publish_all.ps1 (Publishing)
- ✅ Publishes all 4 MicroCMS projects in parallel
- ✅ Targets win-x64 runtime
- ✅ Creates self-contained deployments
- ✅ Injects environment variables into web.config
- ✅ Masks sensitive values in output

### configure-iis.ps1 (IIS Setup)
- ✅ Creates 4 application pools (configured for .NET 8)
- ✅ Sets file permissions (IIS_IUSRS)
- ✅ Creates websites or sub-applications
- ✅ Starts all application pools and sites
- ✅ Provides helpful post-configuration tips

## 🎓 Understanding Output Structure

```
C:\inetpub\micro_cms\
├── cms_webhost\          (MicroCMS.WebHost)
│   ├── MicroCMS.WebHost.dll
│   ├── MicroCMS.WebHost.exe
│   ├── web.config        ← Environment variables here
│   ├── appsettings.json
│   ├── wwwroot\
│   └── ... (all dependencies)
│
├── cms_admin\            (MicroCMS.Admin.WebHost)
│   ├── MicroCMS.Admin.WebHost.dll
│   ├── web.config
│   └── ...
│
├── delivery_webhost\     (MicroCMS.Delivery.WebHost)
│   └── ...
│
└── site_host\            (MicroCMS.SiteHost)
    └── ...
```

## 🛠️ Customization Options

All scripts support parameters. You can:

- Change target directory: `-BaseOutputPath "D:\websites"`
- Change runtime: `-Runtime "win-x86"`
- Use different ports: `-WebHostPort 8080`
- Skip steps: `-SkipPublish` or `-SkipIISConfig`
- Create sub-apps: `-UseSubApplications`

## 🔐 Security Best Practices

1. **Never commit secrets:**
   ```powershell
   # Add to .gitignore
   *-secrets.ps1
   production-secrets.ps1
   deploy-production.ps1  # if it contains real secrets
   ```

2. **Use Windows Credential Manager or Azure Key Vault for production secrets**

3. **The scripts automatically mask sensitive values in console output**

4. **For production, use:**
   - SQL Server with Windows Authentication (no passwords in connection strings)
   - Azure Managed Identities
   - Windows Certificate Store for SSL/TLS

## 📚 Documentation

- **`QUICKSTART.md`** - Start here! 5-minute setup guide
- **`DEPLOY-README.md`** - Detailed documentation with all options
- **`deploy-examples.ps1`** - Copy-paste examples
- **`deploy-production.ps1`** - Template for your production config

## 🚀 Next Steps

1. **Test the deployment:**
   ```powershell
   .\deploy.ps1
   ```

2. **Check the applications:**
   - Open http://localhost:5000 (CMS WebHost)
   - Open http://localhost:5001 (Admin)
   - Open http://localhost:5002 (Delivery)
   - Open http://localhost:5003 (Site Host)

3. **Configure production settings:**
   ```powershell
   Copy-Item deploy-production.ps1 my-production.ps1
   # Edit my-production.ps1
   . .\my-production.ps1
   .\deploy.ps1 -EnvironmentVariables $ProductionVars
   ```

4. **Set up SSL/TLS** (for production)

5. **Configure monitoring and logging**

## 💡 Tips

- Run as Administrator for IIS configuration
- Use `-SkipIISConfig` if IIS is already configured
- Use `-SkipPublish` to just reconfigure IIS
- Check Event Viewer if apps don't start
- Enable stdout logging in web.config for debugging

## 🆘 Troubleshooting

Quick fixes for common issues:

```powershell
# Restart IIS
iisreset

# Check app pool status
Get-WebAppPoolState -Name "MicroCMS_WebHost"

# Restart a specific app pool
Restart-WebAppPool -Name "MicroCMS_WebHost"

# View recent errors
Get-EventLog -LogName Application -Source "IIS*" -Newest 10

# Check what's using a port
Get-NetTCPConnection -LocalPort 5000

# Re-deploy from scratch
.\deploy.ps1
```

## 📞 Support

For more help:
- Read `QUICKSTART.md` for common scenarios
- Check `DEPLOY-README.md` for detailed documentation
- Review `deploy-examples.ps1` for usage examples

---

**Summary:** You now have enterprise-grade PowerShell deployment scripts that match and exceed the functionality of your shell script, with full Windows/IIS support and environment variable management! 🎉
