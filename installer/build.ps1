<#
.SYNOPSIS
  Publishes the tray app and the ebmt tool with Native AOT and builds the Inno Setup installer into artifacts\.
.EXAMPLE
  .\installer\build.ps1 -Version 0.1.0
#>
param(
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$publishDir = Join-Path $root "artifacts\publish"
$cliDir = Join-Path $publishDir "cli"
$appDir = Join-Path $publishDir "app"
$outputDir = Join-Path $root "artifacts"

# Native AOT links with MSVC and finds it through vswhere, which ships outside PATH.
$vsInstaller = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer"
if ((Test-Path $vsInstaller) -and ($env:PATH -notlike "*$vsInstaller*")) {
    $env:PATH += ";$vsInstaller"
}

$iscc = @(
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 7\ISCC.exe"),
    (Join-Path $env:ProgramFiles "Inno Setup 7\ISCC.exe"),
    (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 7\ISCC.exe")
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    throw "Inno Setup 7 is not installed. Get it from https://jrsoftware.org/isdl.php"
}

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

dotnet publish (Join-Path $root "src\ExplorerBlurMicaTray.Cli\ExplorerBlurMicaTray.Cli.csproj") `
    -c Release -o $cliDir -p:Version=$Version -p:DebugType=none
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for ebmt" }

dotnet publish (Join-Path $root "src\ExplorerBlurMicaTray.App\ExplorerBlurMicaTray.App.csproj") `
    -c Release -p:Platform=x64 -o $appDir -p:Version=$Version -p:DebugType=none
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for the tray app" }

& $iscc "/DAppVersion=$Version" "/DCliDir=$cliDir" "/DAppDir=$appDir" "/DOutputDir=$outputDir" `
    (Join-Path $PSScriptRoot "ExplorerBlurMicaTray.iss")
if ($LASTEXITCODE -ne 0) { throw "ISCC failed" }

Write-Host "Built $(Join-Path $outputDir "ExplorerBlurMica-Tray-Setup-$Version.exe")"
