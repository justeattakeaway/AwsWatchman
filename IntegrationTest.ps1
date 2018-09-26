param(
    [Parameter(Mandatory = $false)][string] $Configuration = "Release"
)

$OutputPath = ""
. ./dotnetfunctions.ps1

Write-Host "Building solution..." -ForegroundColor Green
& $dotnet build $solutionPath

Write-Host "Integration Testing solution..." -ForegroundColor Green
DotNetTest "Watchman.AwsResources.IntegrationTests\Watchman.AwsResources.IntegrationTests.csproj"
DotNetTest "Watchman.Engine.IntegrationTests\Watchman.Engine.IntegrationTests.csproj"

