<#
.SYNOPSIS
    Builds EXE installers for x64 and ARM64 using dotnet publish and Inno Setup.
.DESCRIPTION
    Publishes the project for both architectures, then runs Inno Setup to create
    EXE installers suitable for WinGet submission.
#>

param(
    [string]$Configuration = "Release",
    [string]$Version = "0.0.1"
)

$ErrorActionPreference = "Stop"

$projectName = (Get-ChildItem -Path "EventViewer" -Filter "*.csproj" | Select-Object -First 1).BaseName
if (-not $projectName) {
    Write-Error "No .csproj file found in the EventViewer directory."
    exit 1
}

$architectures = @("x64", "arm64")

foreach ($arch in $architectures) {
    Write-Host "`n=== Building $arch ===" -ForegroundColor Cyan

    # Publish
    Write-Host "Publishing for $arch..."
    dotnet publish "EventViewer\EventViewer.csproj" -c $Configuration -r "win-$arch" -o "publish" --self-contained=false -p:WindowsPackageType=None
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed for $arch"
        exit 1
    }

    # Create installer
    Write-Host "Creating installer for $arch..."
    $issFile = "setup-template.iss"
    if (-not (Test-Path $issFile)) {
        Write-Error "Inno Setup script not found: $issFile"
        exit 1
    }

    $archFlag = if ($arch -eq "arm64") { "arm64" } else { "x64" }
    & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" `
        /DMyAppVersion="$Version" `
        /DArchitecturesAllowed="$archFlag" `
        $issFile

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Inno Setup failed for $arch"
        exit 1
    }

    # Clean publish directory for next architecture
    Remove-Item -Recurse -Force "publish" -ErrorAction SilentlyContinue

    Write-Host "=== $arch complete ===" -ForegroundColor Green
}

Write-Host "`nInstallers created in the 'Installer' directory:" -ForegroundColor Cyan
Get-ChildItem -Path "Installer" -Filter "*.exe" | ForEach-Object { Write-Host "  $_" }
