#!/bin/bash

dotnet test Quartermaster.Tests/Quartermaster.Tests.csproj
dotnet test Watchman.AwsResources.Tests/Watchman.AwsResources.Tests.csproj
dotnet test Watchman.Configuration.Tests/Watchman.Configuration.Tests.csproj
dotnet test Watchman.Engine.Tests/Watchman.Engine.Tests.csproj
