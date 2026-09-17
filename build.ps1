<#
.SYNOPSIS
    Builds the FilePathOnFooter VSIX and drops the installer into .\dist.

.DESCRIPTION
    Locates MSBuild with vswhere, restores NuGet packages, builds the extension, and copies
    the resulting installer to .\dist\FilePathOnFooter.vsix.

    The installer targets Visual Studio 17.x and 18.x (Community, Professional, Enterprise).

.PARAMETER Configuration
    Debug or Release (default).

.PARAMETER Install
    After a successful build, launch VSIXInstaller.exe on the artifact.

.EXAMPLE
    .\build.ps1
    .\build.ps1 -Install
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [switch] $Install
)

$ErrorActionPreference = 'Stop'

$root    = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\FilePathOnFooter\FilePathOnFooter.csproj'
$binDir  = Join-Path $root "src\FilePathOnFooter\bin\$Configuration"
$distDir = Join-Path $root 'dist'

# --- Locate MSBuild -------------------------------------------------------
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) {
    throw "vswhere.exe not found at '$vswhere'. A Visual Studio 2022 or newer installation is required to build."
}

$msbuild = & $vswhere -latest -prerelease -products * -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) {
    throw 'MSBuild.exe not found. Install the .NET desktop development workload, or use the Developer Command Prompt.'
}

$vsInstallPath = & $vswhere -latest -prerelease -products * -property installationPath | Select-Object -First 1
$vsVersion     = & $vswhere -latest -prerelease -products * -property installationVersion | Select-Object -First 1

Write-Host "MSBuild       : $msbuild"
Write-Host "Visual Studio : $vsVersion  ($vsInstallPath)"
Write-Host "Configuration : $Configuration"
Write-Host ''

# --- Restore --------------------------------------------------------------
Write-Host '--- Restoring NuGet packages ---' -ForegroundColor Cyan
& $msbuild $project -t:Restore -v:minimal -nologo
if ($LASTEXITCODE -ne 0) { throw "NuGet restore failed (exit $LASTEXITCODE)." }

# --- Build ----------------------------------------------------------------
Write-Host ''
Write-Host '--- Building ---' -ForegroundColor Cyan
& $msbuild $project -t:Rebuild -p:Configuration=$Configuration -v:minimal -nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)." }

$vsix = Get-ChildItem -Path $binDir -Filter *.vsix | Select-Object -First 1
if (-not $vsix) { throw "Build produced no .vsix in '$binDir'." }

if (-not (Test-Path $distDir)) { New-Item -ItemType Directory -Path $distDir | Out-Null }
Copy-Item $vsix.FullName -Destination $distDir -Force
$artifact = Join-Path $distDir $vsix.Name

Write-Host ''
Write-Host '--- Installer ---' -ForegroundColor Green
Write-Host ("  {0} ({1:N0} bytes)" -f $artifact, $vsix.Length)

# --- Optional install -----------------------------------------------------
if ($Install) {
    $installer = Join-Path $vsInstallPath 'Common7\IDE\VSIXInstaller.exe'
    if (-not (Test-Path $installer)) { throw "VSIXInstaller.exe not found at '$installer'." }

    Write-Host ''
    Write-Host "--- Installing $artifact ---" -ForegroundColor Cyan
    Write-Host 'Close all Visual Studio windows first; the installer will prompt.'
    & $installer $artifact
}
