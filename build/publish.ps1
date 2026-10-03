<#
.SYNOPSIS
    Builds the release: a self-contained, single-file DisplayToolkit.exe, zipped with the license.

.DESCRIPTION
    Output goes to artifacts/: the zip (DisplayToolkit-<version>-win-x64.zip) and the unzipped files in
    artifacts/publish. The version comes from Directory.Build.props unless -Version is given (the release
    workflow passes the tag).

.EXAMPLE
    ./build/publish.ps1
#>
param(
    [string] $Version
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$artifacts = Join-Path $root 'artifacts'
$publish = Join-Path $artifacts 'publish'

if (-not $Version) {
    $Version = (Select-Xml -Path (Join-Path $root 'Directory.Build.props') -XPath '//Version').Node.InnerText
}

if (Test-Path $publish) {
    Remove-Item $publish -Recurse -Force
}

dotnet publish (Join-Path $root 'src/DisplayToolkit.App/DisplayToolkit.App.csproj') `
    --configuration Release `
    --output $publish `
    -p:Version=$Version
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed ($LASTEXITCODE)"
}

Copy-Item (Join-Path $root 'LICENSE') (Join-Path $publish 'LICENSE.txt')

$zip = Join-Path $artifacts "DisplayToolkit-$Version-win-x64.zip"
Compress-Archive -Path (Join-Path $publish 'DisplayToolkit.exe'), (Join-Path $publish 'LICENSE.txt') -DestinationPath $zip -Force

$size = '{0:N1} MB' -f ((Get-Item $zip).Length / 1MB)
Write-Host "Built $zip ($size)"
