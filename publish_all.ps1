# PowerShell deployment script for MicroCMS to Windows IIS
# This script publishes all MicroCMS web hosts to c:\inetpub\
#
# Usage:
#   .\publish_all.ps1
#   .\publish_all.ps1 -Environment "Production"
#   .\publish_all.ps1 -Environment "Staging" -EnvironmentVariables @{ConnectionStrings__Default="Server=..."; UseHttps="true"}
#   .\publish_all.ps1 -ShowProgress  # Shows live progress updates

param(
    [string]$Environment = "Production",
    [hashtable]$EnvironmentVariables = @{},
    [string]$PublishConfig = "Release",
    [string]$Runtime = "win-x64",
    [bool]$SelfContained = $true,
    [string]$BaseOutputPath = "C:\inetpub",
    [switch]$ShowProgress = $false
)

# Configuration
$publishConfig = $PublishConfig
$runtime = $Runtime
$selfContained = $SelfContained
$baseOutputPath = $BaseOutputPath

# Add ASPNETCORE_ENVIRONMENT to environment variables if not already present
if (-not $EnvironmentVariables.ContainsKey("ASPNETCORE_ENVIRONMENT")) {
    $EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = $Environment
}

# Color output for better visibility
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Function to create or update web.config with environment variables
function Set-WebConfigEnvironmentVariables {
    param(
        [string]$OutputPath,
        [hashtable]$EnvVars
    )

    $webConfigPath = Join-Path $OutputPath "web.config"

    if (-not (Test-Path $webConfigPath)) {
        Write-ColorOutput "  Warning: web.config not found at $webConfigPath" "Yellow"
        return
    }

    try {
        [xml]$webConfig = Get-Content $webConfigPath

        # Find or create the aspNetCore node
        $aspNetCoreNode = $webConfig.configuration.location.'system.webServer'.aspNetCore

        if (-not $aspNetCoreNode) {
            Write-ColorOutput "  Warning: aspNetCore node not found in web.config" "Yellow"
            return
        }

        # Find or create environmentVariables node
        $envVarsNode = $aspNetCoreNode.environmentVariables

        if (-not $envVarsNode) {
            $envVarsNode = $webConfig.CreateElement("environmentVariables")
            [void]$aspNetCoreNode.AppendChild($envVarsNode)
        } else {
            # Clear existing environment variables
            $envVarsNode.RemoveAll()
        }

        # Add environment variables
        foreach ($key in $EnvVars.Keys) {
            $envVar = $webConfig.CreateElement("environmentVariable")
            $envVar.SetAttribute("name", $key)
            $envVar.SetAttribute("value", $EnvVars[$key])
            [void]$envVarsNode.AppendChild($envVar)
        }

        $webConfig.Save($webConfigPath)
        Write-ColorOutput "  Updated web.config with $($EnvVars.Count) environment variable(s)" "Green"
    }
    catch {
        Write-ColorOutput "  Error updating web.config: $_" "Red"
    }
}

# Create base directory if it doesn't exist
if (-not (Test-Path $baseOutputPath)) {
    Write-ColorOutput "Creating base directory: $baseOutputPath" "Yellow"
    New-Item -ItemType Directory -Path $baseOutputPath -Force | Out-Null
}

# Define projects to publish
$projects = @(
    @{
        Name = "MicroCMS.WebHost"
        Path = ".\src\MicroCMS.WebHost"
        OutputPath = "$baseOutputPath\micro_cms\cms_webhost"
    },
    @{
        Name = "MicroCMS.Admin.WebHost"
        Path = ".\src\MicroCMS.Admin.WebHost"
        OutputPath = "$baseOutputPath\micro_cms\cms_admin"
    },
    @{
        Name = "MicroCMS.Delivery.WebHost"
        Path = ".\src\MicroCMS.Delivery.WebHost"
        OutputPath = "$baseOutputPath\micro_cms\delivery_webhost"
    },
    @{
        Name = "MicroCMS.SiteHost"
        Path = ".\src\MicroCMS.SiteHost"
        OutputPath = "$baseOutputPath\micro_cms\site_host"
    }
)

Write-ColorOutput "`n=== Starting MicroCMS Deployment ===" "Cyan"
Write-ColorOutput "Environment: $Environment" "Cyan"
Write-ColorOutput "Configuration: $publishConfig" "Cyan"
Write-ColorOutput "Runtime: $runtime" "Cyan"
Write-ColorOutput "Self-contained: $selfContained" "Cyan"
Write-ColorOutput "Target: $baseOutputPath" "Cyan"

if ($EnvironmentVariables.Count -gt 0) {
    Write-ColorOutput "`nEnvironment Variables to be set:" "Cyan"
    foreach ($key in $EnvironmentVariables.Keys) {
        $value = $EnvironmentVariables[$key]
        # Mask sensitive values (connection strings, passwords, etc.)
        if ($key -match "password|secret|key|token|connectionstring" -and $value.Length -gt 10) {
            $maskedValue = $value.Substring(0, 10) + "..." + $value.Substring($value.Length - 5)
            Write-ColorOutput "  $key = $maskedValue" "Gray"
        } else {
            Write-ColorOutput "  $key = $value" "Gray"
        }
    }
}

Write-Host ""

# Publish all projects in parallel using jobs
$jobs = @()
$solutionRoot = Get-Location

foreach ($project in $projects) {
    Write-ColorOutput "Starting publish for: $($project.Name)" "Green"

    $scriptBlock = {
        param($solutionRoot, $projectPath, $outputPath, $config, $runtime, $selfContained)

        $publishArgs = @(
            "publish"
            (Join-Path $solutionRoot $projectPath)
            "-c", $config
            "-r", $runtime
            "-o", $outputPath
        )

        if ($selfContained) {
            $publishArgs += "--self-contained"
        }

        # Capture both output and errors
        $output = & dotnet $publishArgs 2>&1 | Out-String

        return @{
            ProjectPath = $projectPath
            OutputPath = $outputPath
            ExitCode = $LASTEXITCODE
            Output = $output
        }
    }

    $job = Start-Job -ScriptBlock $scriptBlock -ArgumentList $solutionRoot, $project.Path, $project.OutputPath, $publishConfig, $runtime, $selfContained
    $jobs += @{
        Job = $job
        Project = $project
    }
}

# Wait for all jobs and display results
Write-ColorOutput "`nWaiting for all publish operations to complete...`n" "Yellow"

# Show progress if requested
if ($ShowProgress) {
    Write-ColorOutput "Live progress monitoring enabled. Checking job status every 2 seconds...`n" "Cyan"

    while ($jobs | Where-Object { $_.Job.State -eq 'Running' }) {
        foreach ($jobInfo in $jobs) {
            if ($jobInfo.Job.State -eq 'Running') {
                Write-Host "  [RUNNING] $($jobInfo.Project.Name)..." -ForegroundColor Yellow
            }
        }
        Start-Sleep -Seconds 2
        Write-Host "" # Clear line
    }
}

$allSucceeded = $true

foreach ($jobInfo in $jobs) {
    $job = $jobInfo.Job
    $project = $jobInfo.Project

    # Wait for this job to complete
    $result = Receive-Job -Job $job -Wait
    Remove-Job -Job $job

    if ($result.ExitCode -eq 0) {
        Write-ColorOutput "[SUCCESS] $($project.Name) -> $($project.OutputPath)" "Green"

        # Show last few lines of output for successful builds (optional)
        if ($result.Output) {
            $outputLines = $result.Output -split "`n" | Where-Object { $_ -match "^\s*\S" } | Select-Object -Last 3
            foreach ($line in $outputLines) {
                Write-ColorOutput "  $line" "Gray"
            }
        }

        # Update web.config with environment variables if any are specified
        if ($EnvironmentVariables.Count -gt 0) {
            Set-WebConfigEnvironmentVariables -OutputPath $project.OutputPath -EnvVars $EnvironmentVariables
        }
    } else {
        Write-ColorOutput "[FAILED] $($project.Name) (Exit Code: $($result.ExitCode))" "Red"

        # Show the full error output
        if ($result.Output) {
            Write-ColorOutput "`nError output for $($project.Name):" "Yellow"
            Write-ColorOutput "------------------------------------------------------------" "Gray"
            Write-Host $result.Output -ForegroundColor Red
            Write-ColorOutput "------------------------------------------------------------`n" "Gray"
        }

        $allSucceeded = $false
    }
}

# Summary
Write-ColorOutput "`n=== Deployment Summary ===" "Cyan"
if ($allSucceeded) {
    Write-ColorOutput "All projects published successfully!" "Green"
    Write-ColorOutput "`nDeployment locations:" "White"
    foreach ($project in $projects) {
        Write-ColorOutput "  - $($project.Name): $($project.OutputPath)" "Gray"
    }

    Write-ColorOutput "`nNext steps:" "Yellow"
    Write-ColorOutput "  1. Configure IIS application pools for each site" "Gray"
    Write-ColorOutput "  2. Create IIS websites/applications pointing to the deployment folders" "Gray"
    Write-ColorOutput "  3. Update appsettings.json files with production settings" "Gray"
    Write-ColorOutput "  4. Ensure proper file permissions for IIS user (IIS_IUSRS)" "Gray"
    Write-ColorOutput "  5. Install ASP.NET Core hosting bundle if not already installed" "Gray"

    exit 0
} else {
    Write-ColorOutput "Some projects failed to publish. Check the errors above." "Red"
    exit 1
}
