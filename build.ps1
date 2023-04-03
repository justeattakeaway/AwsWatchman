#! /usr/bin/pwsh

#Requires -PSEdition Core
#Requires -Version 7

param(
    [Parameter(Mandatory = $false)][string] $Configuration = "Release",
    [Parameter(Mandatory = $false)][string] $VersionSuffix = "",
    [Parameter(Mandatory = $false)][string] $OutputPath = "",
    [Parameter(Mandatory = $false)][switch] $SkipTests
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$solutionPath = $PSScriptRoot
$sdkFile = Join-Path $solutionPath "global.json"

$dotnetVersion = (Get-Content $sdkFile | Out-String | ConvertFrom-Json).sdk.version

if ($OutputPath -eq "") {
    $OutputPath = Join-Path "$(Convert-Path "$PSScriptRoot")" "artifacts"
}

$installDotNetSdk = $false;

if (($null -eq (Get-Command "dotnet" -ErrorAction SilentlyContinue)) -and ($null -eq (Get-Command "dotnet.exe" -ErrorAction SilentlyContinue))) {
    Write-Host "The .NET SDK is not installed."
    $installDotNetSdk = $true
}
else {
    Try {
        $installedDotNetVersion = (dotnet --version 2>&1 | Out-String).Trim()
    }
    Catch {
        $installedDotNetVersion = "?"
    }

    if ($installedDotNetVersion -ne $dotnetVersion) {
        Write-Host "The required version of the .NET SDK is not installed. Expected $dotnetVersion."
        $installDotNetSdk = $true
    }
}

if ($installDotNetSdk -eq $true) {

    $env:DOTNET_INSTALL_DIR = Join-Path "$(Convert-Path "$PSScriptRoot")" ".dotnetcli"
    $sdkPath = Join-Path $env:DOTNET_INSTALL_DIR "sdk\$dotnetVersion"

    if (!(Test-Path $sdkPath)) {
        if (!(Test-Path $env:DOTNET_INSTALL_DIR)) {
            mkdir $env:DOTNET_INSTALL_DIR | Out-Null
        }
        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor "Tls12"

        if (($PSVersionTable.PSVersion.Major -ge 6) -And !$IsWindows) {
            $installScript = Join-Path $env:DOTNET_INSTALL_DIR "install.sh"
            Invoke-WebRequest "https://dot.net/v1/dotnet-install.sh" -OutFile $installScript -UseBasicParsing
            chmod +x $installScript
            & $installScript --version "$dotnetVersion" --install-dir "$env:DOTNET_INSTALL_DIR" --no-path
        }
        else {
            $installScript = Join-Path $env:DOTNET_INSTALL_DIR "install.ps1"
            Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript -UseBasicParsing
            & $installScript -Version "$dotnetVersion" -InstallDir "$env:DOTNET_INSTALL_DIR" -NoPath
        }
    }
}
else {
    $env:DOTNET_INSTALL_DIR = Split-Path -Path (Get-Command dotnet).Path
}

$dotnet = Join-Path "$env:DOTNET_INSTALL_DIR" "dotnet"

if ($installDotNetSdk -eq $true) {
    $env:PATH = "$env:DOTNET_INSTALL_DIR;$env:PATH"
}

function DotNetPublish {
    param([string]$Project)

    $publishPath = (Join-Path $OutputPath "publish")

    $additionalArgs = @()

    if (![string]::IsNullOrEmpty($VersionSuffix)) {
        $additionalArgs += "--version-suffix"
        $additionalArgs += $VersionSuffix
    }

    & $dotnet publish $Project --output $publishPath --configuration $Configuration $additionalArgs

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
}

function DotNetTest {
    param([string]$Project)

    $additionalArgs = @()

    if (![string]::IsNullOrEmpty($env:GITHUB_SHA)) {
        $additionalArgs += "--logger"
        $additionalArgs += "GitHubActions;report-warnings=false"
    }

    & $dotnet test $Project --output $OutputPath --configuration $Configuration $additionalArgs

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet test failed with exit code $LASTEXITCODE"
    }
}

$publishProjects = @(
    (Join-Path $solutionPath "Watchman\Watchman.csproj"),
    (Join-Path $solutionPath "Quartermaster\Quartermaster.csproj")
)

Write-Host "Publishing solution..." -ForegroundColor Green
ForEach ($project in $publishProjects) {
    DotNetPublish $project
}

$unitTestProjects = @(
    (Join-Path $solutionPath "Quartermaster.Tests\Quartermaster.Tests.csproj"),
    (Join-Path $solutionPath "Watchman.AwsResources.Tests\Watchman.AwsResources.Tests.csproj"),
    (Join-Path $solutionPath "Watchman.Configuration.Tests\Watchman.Configuration.Tests.csproj"),
    (Join-Path $solutionPath "Watchman.Engine.Tests\Watchman.Engine.Tests.csproj"),
    (Join-Path $solutionPath "Watchman.Tests\Watchman.Tests.csproj")
)

Write-Host "Running unit tests..." -ForegroundColor Green
ForEach ($testProject in $unitTestProjects) {
    DotNetTest $testProject
}

$integrationTestProjects = @(
    (Join-Path $solutionPath "Watchman.AwsResources.IntegrationTests\Watchman.AwsResources.IntegrationTests.csproj"),
    (Join-Path $solutionPath "Watchman.Engine.IntegrationTests\Watchman.Engine.IntegrationTests.csproj")
)

Write-Host "Running integration tests..." -ForegroundColor Green
ForEach ($testProject in $integrationTestProjects) {
    DotNetTest $testProject
}
