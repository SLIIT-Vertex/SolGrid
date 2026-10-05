<#
 Project: SolGrid
 File: deploy-iis.ps1
 Description: Publishes the SolGrid API and creates/updates the IIS site, app pool and
              app-pool environment variables. Run from an elevated PowerShell on the IIS host.
 Prerequisites: IIS enabled, .NET 10 Hosting Bundle installed, MongoDB reachable.
 Usage: .\scripts\deploy-iis.ps1 -MongoConnectionString "mongodb://localhost:27017" `
            -JwtSigningKey "<long-random-secret>" -AllowedOrigin "http://localhost:8082"
#>
param(
    [Parameter(Mandatory)] [string] $MongoConnectionString,
    [Parameter(Mandatory)] [string] $JwtSigningKey,
    [string] $AllowedOrigin = "http://localhost:5173",
    [string] $SiteName = "SolGridApi",
    [int] $Port = 8081,
    [string] $PublishPath = "C:\inetpub\solgrid-api"
)

$ErrorActionPreference = "Stop"
$appcmd = "$env:windir\system32\inetsrv\appcmd.exe"
if (-not (Test-Path $appcmd)) { throw "IIS is not installed. Enable Internet Information Services first." }

# Publish the API (Microsoft.NET.Sdk.Web emits web.config for the ASP.NET Core Module).
$root = Split-Path $PSScriptRoot -Parent
dotnet publish "$root\web-service\src\SolGrid.Api" -c Release -o $PublishPath
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# App pool must be "No Managed Code" for ASP.NET Core (appcmd avoids the IIS: drive / WebAdministration module).
& $appcmd list apppool /name:"$SiteName" | Out-Null
if ($LASTEXITCODE -ne 0) { & $appcmd add apppool /name:"$SiteName" | Out-Null }
& $appcmd set apppool /apppool.name:"$SiteName" /managedRuntimeVersion:"" | Out-Null

# Create the site once; later runs only republish files.
& $appcmd list site /name:"$SiteName" | Out-Null
if ($LASTEXITCODE -ne 0) {
    & $appcmd add site /name:"$SiteName" /physicalPath:"$PublishPath" /bindings:"http/*:${Port}:" | Out-Null
}
& $appcmd set app "$SiteName/" /applicationPool:"$SiteName" | Out-Null
icacls $PublishPath /grant "IIS AppPool\${SiteName}:(OI)(CI)RX" | Out-Null

# Secrets/config go in app-pool environment variables, not in source control.
$vars = [ordered]@{
    "ASPNETCORE_ENVIRONMENT"     = "Production"
    "MongoDb__ConnectionString"  = $MongoConnectionString
    "Jwt__SigningKey"            = $JwtSigningKey
    "Cors__AllowedOrigins__0"    = $AllowedOrigin
}
foreach ($name in $vars.Keys) {
    # Remove any existing entry first so reruns do not fail.
    & $appcmd set config -section:system.applicationHost/applicationPools "/-[name='$SiteName'].environmentVariables.[name='$name']" /commit:apphost 2>$null | Out-Null
    & $appcmd set config -section:system.applicationHost/applicationPools "/+[name='$SiteName'].environmentVariables.[name='$name',value='$($vars[$name])']" /commit:apphost | Out-Null
}

if (-not (Get-NetFirewallRule -DisplayName "SolGrid API" -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -DisplayName "SolGrid API" -Direction Inbound -Protocol TCP -LocalPort $Port -Action Allow | Out-Null
}

& $appcmd recycle apppool /apppool.name:"$SiteName" | Out-Null
Write-Host "Deployed. Check: http://localhost:$Port/health"
