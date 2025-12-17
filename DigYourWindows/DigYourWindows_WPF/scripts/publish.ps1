param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "DigYourWindows.UI\DigYourWindows.UI.csproj"

if (-not (Test-Path $projectPath))
{
    throw "未找到入口项目: $projectPath"
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$outRoot = Join-Path $repoRoot "bld\publish\DigYourWindows_$timestamp"

$fddOut = Join-Path $outRoot "fdd_$Runtime"
$scdOut = Join-Path $outRoot "scd_$Runtime"

New-Item -ItemType Directory -Force -Path $fddOut | Out-Null
New-Item -ItemType Directory -Force -Path $scdOut | Out-Null

Write-Host "== DigYourWindows Publish =="
Write-Host "Project: $projectPath"
Write-Host "Configuration: $Configuration"
Write-Host "Runtime: $Runtime"
Write-Host "Output: $outRoot"

Write-Host "\n[1/4] Framework-dependent publish..."
dotnet publish $projectPath -c $Configuration -r $Runtime --self-contained false -o $fddOut
if ($LASTEXITCODE -ne 0) { throw "dotnet publish (FDD) 失败" }

Write-Host "\n[2/4] Self-contained publish..."
dotnet publish $projectPath -c $Configuration -r $Runtime --self-contained true -o $scdOut
if ($LASTEXITCODE -ne 0) { throw "dotnet publish (SCD) 失败" }

Write-Host "\n[3/4] Compress FDD..."
$fddZip = Join-Path $outRoot "DigYourWindows_${Runtime}_fdd_${timestamp}.zip"
Compress-Archive -Path (Join-Path $fddOut "*") -DestinationPath $fddZip -Force

Write-Host "\n[4/4] Compress SCD..."
$scdZip = Join-Path $outRoot "DigYourWindows_${Runtime}_scd_${timestamp}.zip"
Compress-Archive -Path (Join-Path $scdOut "*") -DestinationPath $scdZip -Force

Write-Host "\n完成。"
Write-Host "FDD: $fddOut"
Write-Host "FDD ZIP: $fddZip"
Write-Host "SCD: $scdOut"
Write-Host "SCD ZIP: $scdZip"
