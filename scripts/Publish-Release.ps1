[CmdletBinding()]
param(
    [string]$Version = '0.1.0',
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$packageName = "YTMusicLite-$Version-$Runtime-self-contained"
$publishDirectory = [System.IO.Path]::GetFullPath((Join-Path $artifactsRoot $packageName))
$zipPath = [System.IO.Path]::GetFullPath((Join-Path $artifactsRoot "$packageName.zip"))
$checksumPath = "$zipPath.sha256"

if (-not $publishDirectory.StartsWith($artifactsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "The publish directory is outside the repository artifacts folder."
}

New-Item -ItemType Directory -Force -Path $artifactsRoot | Out-Null

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

if (Test-Path -LiteralPath $checksumPath) {
    Remove-Item -LiteralPath $checksumPath -Force
}

dotnet publish (Join-Path $repoRoot 'YTMusicLite.csproj') `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDirectory `
    -p:Version=$Version

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination $publishDirectory
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination $publishDirectory
Copy-Item -LiteralPath (Join-Path $repoRoot 'PRIVACY.md') -Destination $publishDirectory

Compress-Archive -Path (Join-Path $publishDirectory '*') -DestinationPath $zipPath -CompressionLevel Optimal

$hash = Get-FileHash -Algorithm SHA256 -LiteralPath $zipPath
"$($hash.Hash.ToLowerInvariant())  $([System.IO.Path]::GetFileName($zipPath))" |
    Set-Content -LiteralPath $checksumPath -Encoding ascii

Write-Host "Release package: $zipPath"
Write-Host "SHA-256: $checksumPath"
