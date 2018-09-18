param(
    [Parameter(Mandatory = $false)][string] $Configuration = "Release"
)

$OutputPath = ""
. ./dotnetfunctions.ps1

Write-Host "Building solution..." -ForegroundColor Green
& $dotnet build $solutionPath

Write-Host "Testing solution..." -ForegroundColor Green
DotNetTest "Quartermaster.Tests\Quartermaster.Tests.csproj"
DotNetTest "Watchman.AwsResources.Tests\Watchman.AwsResources.Tests.csproj"
DotNetTest "Watchman.Configuration.Tests\Watchman.Configuration.Tests.csproj"
DotNetTest "Watchman.Engine.Tests\Watchman.Engine.Tests.csproj"
