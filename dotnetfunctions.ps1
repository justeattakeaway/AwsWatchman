# These make CI builds faster
$env:DOTNET_MULTILEVEL_LOOKUP = "0"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "true"
$env:NUGET_XMLDOC_MODE = "skip"

# Required since 2.1.300 to get console output from xunit for failed tests
$env:MSBUILDENSURESTDOUTFORTASKPROCESSES=1

$solutionPath = Split-Path $MyInvocation.MyCommand.Definition
$sdkFile      = Join-Path $solutionPath "global.json"
$dotnetVersion = (Get-Content $sdkFile | Out-String | ConvertFrom-Json).sdk.version

if ($env:TEAMCITY_VERSION -ne $null) {
    $BuildPackage = $true
    $OutputPath = Join-Path "$(Convert-Path "$PSScriptRoot")" "out"
    $VersionPrefix = $env:BUILD_NUMBER

    if ($env:TEAMCITY_IS_DEFAULT_BRANCH -eq "false") {
        $VersionSuffix = "beta"
    }
}
elseif ($OutputPath -eq "") {
    $OutputPath = Join-Path "$(Convert-Path "$PSScriptRoot")" "artifacts"
}

$installDotNetSdk = $false;

if (((Get-Command "dotnet.exe" -ErrorAction SilentlyContinue) -eq $null) -and ((Get-Command "dotnet" -ErrorAction SilentlyContinue) -eq $null)) {
    Write-Host "The .NET Core SDK is not installed."
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
        Write-Host "The required version of the .NET Core SDK is not installed. Expected $dotnetVersion."
        $installDotNetSdk = $true
    }
}

if ($installDotNetSdk -eq $true) {

    $env:DOTNET_INSTALL_DIR = Join-Path "$(Convert-Path "$PSScriptRoot")" ".dotnetcli"
    $env:MSBuildSDKsPath = Join-Path $env:DOTNET_INSTALL_DIR "sdk\$dotnetVersion\Sdks"

    if (($env:TEAMCITY_VERSION -ne $null) -or (!(Test-Path $env:DOTNET_INSTALL_DIR))) {
        mkdir $env:DOTNET_INSTALL_DIR -Force | Out-Null
        $installScript = Join-Path $env:DOTNET_INSTALL_DIR "install.ps1"
        Invoke-WebRequest "https://raw.githubusercontent.com/dotnet/cli/v$dotnetVersion/scripts/obtain/dotnet-install.ps1" -OutFile $installScript -UseBasicParsing
        & $installScript -Version "$dotnetVersion" -InstallDir "$env:DOTNET_INSTALL_DIR" -NoPath
    }

    $env:PATH = "$env:DOTNET_INSTALL_DIR;$env:PATH"
    $dotnet = Join-Path "$env:DOTNET_INSTALL_DIR" "dotnet.exe"
} else {
    $dotnet = "dotnet"
    Write-Host "The required version of the .NET Core SDK, $dotnetVersion is already installed."
}

function DotNetTest {
    param([string] $Project)

        & $dotnet test $Project --output $OutputPath --logger:"console;verbosity=normal" -- RunConfiguration.TestSessionTimeout=600000

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet test failed with exit code $LASTEXITCODE"
    }
}
