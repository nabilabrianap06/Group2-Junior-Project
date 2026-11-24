# Builds the installable package: dist/OxyGenAI (folder) and dist/OxyGenAI-win-x64.zip.
# The package is self-contained: the target PC does not need the .NET SDK or runtime, only PostgreSQL.
#   powershell -ExecutionPolicy Bypass -File publish.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out  = Join-Path $root 'dist\OxyGenAI'
$zip  = Join-Path $root 'dist\OxyGenAI-win-x64.zip'

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }

dotnet publish (Join-Path $root 'src\AIEnvironmentalTracker.Web\AIEnvironmentalTracker.Web.csproj') `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:OxyGenAIPackage=true -p:DebugType=None -o $out
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

Copy-Item (Join-Path $root 'packaging\*') $out -Force
Copy-Item (Join-Path $root 'docs\USER_GUIDE.md') $out -Force
# Never ship a settings file that holds someone's password or key.
Remove-Item (Join-Path $out 'oxygenai.settings.json') -ErrorAction SilentlyContinue

Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip
Write-Host "Package ready: $zip"
