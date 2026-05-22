# Production deployment configuration
# Copy this file and customize for your environment
# Usage: . .\deploy-production.ps1; .\publish_all.ps1 -EnvironmentVariables $ProductionVars

$ProductionVars = @{
    # Environment
    ASPNETCORE_ENVIRONMENT = "Production"

    # Database Connection Strings
    # Replace with your actual connection strings
    ConnectionStrings__DefaultConnection = "Server=prod-sql-server;Database=MicroCMS;Integrated Security=true;TrustServerCertificate=true"
    ConnectionStrings__RedisCache = "prod-redis:6379,password=your-redis-password"

    # Logging Configuration
    Logging__LogLevel__Default = "Warning"
    Logging__LogLevel__Microsoft = "Warning"
    Logging__LogLevel__Microsoft__AspNetCore = "Warning"

    # Security & Authentication
    # AllowedHosts = "yourdomain.com;www.yourdomain.com"

    # If using Google Authentication
    # Authentication__Google__ClientId = "your-google-client-id"
    # Authentication__Google__ClientSecret = "your-google-client-secret"

    # If using Azure AD
    # AzureAd__Instance = "https://login.microsoftonline.com/"
    # AzureAd__TenantId = "your-tenant-id"
    # AzureAd__ClientId = "your-client-id"

    # Application-specific settings
    # Add any custom application settings here
    # Example: ApplicationInsights__InstrumentationKey = "your-key"
}

# Uncomment to deploy immediately
# .\publish_all.ps1 -EnvironmentVariables $ProductionVars
