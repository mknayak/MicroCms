# Example deployment configurations for different environments
# Source this file or copy the configuration you need

# Example 1: Deploy with default Production environment
# .\publish_all.ps1

# Example 2: Deploy to Staging environment
# .\publish_all.ps1 -Environment "Staging"

# Example 3: Deploy with custom environment variables
# .\publish_all.ps1 -Environment "Production" -EnvironmentVariables @{
#     ASPNETCORE_ENVIRONMENT = "Production"
#     UseHttps = "true"
#     LogLevel = "Information"
# }

# Example 4: Deploy with database connection strings
# .\publish_all.ps1 -EnvironmentVariables @{
#     ASPNETCORE_ENVIRONMENT = "Production"
#     ConnectionStrings__DefaultConnection = "Server=myserver;Database=microcms;Trusted_Connection=true;"
#     ConnectionStrings__RedisCache = "localhost:6379"
# }

# Example 5: Deploy with all custom settings
# .\publish_all.ps1 `
#     -Environment "Production" `
#     -PublishConfig "Release" `
#     -Runtime "win-x64" `
#     -SelfContained $true `
#     -BaseOutputPath "C:\inetpub" `
#     -EnvironmentVariables @{
#         ASPNETCORE_ENVIRONMENT = "Production"
#         ConnectionStrings__DefaultConnection = "Server=myserver;Database=microcms;User Id=microcms_user;Password=SecurePassword123;"
#         Authentication__Google__ClientId = "your-client-id"
#         Authentication__Google__ClientSecret = "your-client-secret"
#         Logging__LogLevel__Default = "Warning"
#         Logging__LogLevel__Microsoft = "Warning"
#         AllowedHosts = "*"
#     }

# Example 6: Load environment variables from a file
# Create a file named 'deploy-vars.ps1' with your environment variables:
# $deployVars = @{
#     ASPNETCORE_ENVIRONMENT = "Production"
#     ConnectionStrings__DefaultConnection = "Server=..."
# }
# 
# Then run:
# . .\deploy-vars.ps1
# .\publish_all.ps1 -EnvironmentVariables $deployVars

# Example 7: Deploy to custom location (e.g., D:\websites)
# .\publish_all.ps1 -BaseOutputPath "D:\websites"

# ============================================================================
# Common Environment Variables for ASP.NET Core
# ============================================================================
# 
# ASPNETCORE_ENVIRONMENT           - Development, Staging, Production
# ASPNETCORE_URLS                  - URLs to listen on (e.g., "http://localhost:5000")
# ASPNETCORE_HTTPS_PORT            - HTTPS port
# ConnectionStrings__*             - Database connection strings (use __ for nested config)
# Logging__LogLevel__*             - Logging levels
# AllowedHosts                     - Allowed hosts for the application
# ASPNETCORE_FORWARDEDHEADERS_*    - For reverse proxy scenarios
#
# ============================================================================
# Tips for Connection String Configuration
# ============================================================================
#
# Use double underscores (__) to represent nested configuration in appsettings.json:
#
# appsettings.json:
#   "ConnectionStrings": {
#     "DefaultConnection": "..."
#   }
#
# Environment Variable:
#   ConnectionStrings__DefaultConnection = "..."
#
# ============================================================================
