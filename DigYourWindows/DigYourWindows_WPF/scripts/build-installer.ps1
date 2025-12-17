param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$AppName = "DigYourWindows",
    [string]$AppPublisher = "LessUp"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "DigYourWindows.UI\DigYourWindows.UI.csproj"
$issPath = Join-Path $repoRoot "installer\DigYourWindows.iss"

if (-not (Test-Path $projectPath))
{
    throw "未找到入口项目: $projectPath"
}

if (-not (Test-Path $issPath))
{
    throw "未找到 Inno Setup 脚本: $issPath"
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$outRoot = Join-Path $repoRoot "bld\installer\DigYourWindows_$timestamp"
$publishDir = Join-Path $outRoot "publish_$Runtime"
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

Write-Host "== DigYourWindows Installer Build =="
Write-Host "Project: $projectPath"
Write-Host "Configuration: $Configuration"
Write-Host "Runtime: $Runtime"
Write-Host "PublishDir: $publishDir"
Write-Host "OutRoot: $outRoot"

Write-Host "\n[1/3] Publish (self-contained)..."
dotnet publish $projectPath -c $Configuration -r $Runtime --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败" }

$exeName = "DigYourWindows.UI.exe"
$exePath = Join-Path $publishDir $exeName
if (-not (Test-Path $exePath))
{
    $exeCandidate = Get-ChildItem -Path $publishDir -Filter "*.exe" -File | Select-Object -First 1
    if ($null -ne $exeCandidate)
    {
        $exeName = $exeCandidate.Name
        $exePath = $exeCandidate.FullName
    }
}

if (-not (Test-Path $exePath))
{
    throw "未找到发布后的 exe: $exePath"
}

$version = (Get-Item $exePath).VersionInfo.ProductVersion
if ([string]::IsNullOrWhiteSpace($version))
{
    $version = (Get-Item $exePath).VersionInfo.FileVersion
}

if ([string]::IsNullOrWhiteSpace($version))
{
    $version = "0.0.0"
}

$version = $version.Trim()
$version = $version.Split('+')[0]
$version = $version.Split(' ')[0]
if ([string]::IsNullOrWhiteSpace($version))
{
    $version = "0.0.0"
}

$versionForFile = ($version -replace "[^0-9A-Za-z\.\-_]", "_")

Write-Host "\nDetected EXE: $exeName"
Write-Host "Detected Version: $version"

Write-Host "\n[2/3] Locate ISCC.exe..."
$iscc = $null
$cmd = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
if ($null -ne $cmd)
{
    $iscc = $cmd.Source
}

if ([string]::IsNullOrWhiteSpace($iscc))
{
    $candidates = @()

    if (-not [string]::IsNullOrWhiteSpace($env:LOCALAPPDATA))
    {
        $candidates += (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe")
    }

    if (-not [string]::IsNullOrWhiteSpace(${env:ProgramFiles(x86)}))
    {
        $candidates += (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe")
    }

    if (-not [string]::IsNullOrWhiteSpace($env:ProgramFiles))
    {
        $candidates += (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    }

    foreach ($c in $candidates)
    {
        if (Test-Path $c)
        {
            $iscc = $c
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($iscc))
{
    $regPaths = @(
        'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
        'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
        'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
    )

    foreach ($rp in $regPaths)
    {
        $entry = Get-ItemProperty $rp -ErrorAction SilentlyContinue |
            Where-Object { $_.DisplayName -like 'Inno Setup*' -and -not [string]::IsNullOrWhiteSpace($_.InstallLocation) } |
            Select-Object -First 1

        if ($null -ne $entry)
        {
            $candidate = Join-Path $entry.InstallLocation 'ISCC.exe'
            if (Test-Path $candidate)
            {
                $iscc = $candidate
                break
            }
        }
    }
}

if ([string]::IsNullOrWhiteSpace($iscc) -or -not (Test-Path $iscc))
{
    throw "未找到 ISCC.exe。请安装 Inno Setup 6，并确保 ISCC.exe 可在 PATH 中找到或位于默认安装目录。"
}

Write-Host "ISCC: $iscc"

Write-Host "\n[3/3] Build installer..."
$outputBaseFilename = "${AppName}_Setup_${Runtime}_$versionForFile"
$args = @(
    $issPath,
    "/DAppName=$AppName",
    "/DAppPublisher=$AppPublisher",
    "/DAppVersion=$version",
    "/DAppExeName=$exeName",
    "/DPublishDir=$publishDir",
    "/DOutputDir=$outRoot",
    "/DOutputBaseFilename=$outputBaseFilename"
)

& $iscc @args
if ($LASTEXITCODE -ne 0) { throw "ISCC 编译失败" }

Write-Host "\n完成。"
Write-Host "Installer OutDir: $outRoot"
Write-Host "BaseName: $outputBaseFilename"
