param(
    [Parameter(Mandatory = $false)][string] $Configuration = "Release",
    [Parameter(Mandatory = $false)][switch] $SkipTests
)

$ErrorActionPreference = "Stop"

$solutionPath = Split-Path $MyInvocation.MyCommand.Definition
$solutionFile = Join-Path $solutionPath "AwsWatchman.sln"

$dotnetVersion = "2.1.401"

$installDotNetSdk = $false;

if (((Get-Command "dotnet.exe" -ErrorAction SilentlyContinue) -eq $null) -and ((Get-Command "dotnet" -ErrorAction SilentlyContinue) -eq $null)) {
    Write-Host "The .NET Core SDK is not installed."
    $installDotNetSdk = $true
}
else {
    $installedDotNetVersion = (dotnet --version | Out-String).Trim()
    if ($installedDotNetVersion -ne $dotnetVersion) {
        Write-Host "The required version of the .NET Core SDK is not installed. Expected $dotnetVersion but $installedDotNetVersion was found."
        $installDotNetSdk = $true
    }
}

if ($installDotNetSdk -eq $true) {
    $env:DOTNET_INSTALL_DIR = Join-Path "$(Convert-Path "$PSScriptRoot")" ".dotnetcli"

    if (!(Test-Path $env:DOTNET_INSTALL_DIR)) {
        mkdir $env:DOTNET_INSTALL_DIR | Out-Null
        $installScript = Join-Path $env:DOTNET_INSTALL_DIR "install.ps1"
        Invoke-WebRequest "https://raw.githubusercontent.com/dotnet/cli/release/2.0.0/scripts/obtain/dotnet-install.ps1" -OutFile $installScript
        & $installScript -Version "$dotnetVersion" -InstallDir "$env:DOTNET_INSTALL_DIR" -NoPath
    }

    $env:PATH = "$env:DOTNET_INSTALL_DIR;$env:PATH"
    $dotnet = Join-Path "$env:DOTNET_INSTALL_DIR" "dotnet.exe"
}
else {
    $dotnet = "dotnet"
}

function DotNetRestore {
    param([string]$Project)
    & $dotnet restore $Project --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE"
    }
}


function DotNetBuild {
    param([string]$Project)
    & $dotnet build $Project --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE"
    }
}


Write-Host "Restoring NuGet packages for solution..." -ForegroundColor Green
DotNetRestore $solutionFile

Write-Host "Building solution..." -ForegroundColor Green
DotNetBuild $solutionFile


if ($SkipTests -eq $false) {
    Write-Host "Running tests..." -ForegroundColor Green
    .\unit_test.bat
}
