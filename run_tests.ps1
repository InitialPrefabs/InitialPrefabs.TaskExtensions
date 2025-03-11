param (
    [Parameter(Mandatory=$true)][bool]$dotnet_test = $true
)

if ($dotnet_test) {
    dotnet build -c Debug
    coverlet .\InitialPrefabs.TaskFlow.Tests\bin\Debug\net8.0\InitialPrefabs.TaskFlow.Tests.dll --target "dotnet" --targetargs "test InitialPrefabs.TaskFlow.Tests/InitialPrefabs.TaskFlow.Tests.csproj --no-build" --format lcov --output coverage.info
    reportgenerator -reports:coverage.info -targetdir:CoverageReport
    start CoverageReport\index.html
} else {
    # TODO: Deprecate this and remove nunit console runner
    dotnet run --project .\InitialPrefabs.TaskFlow.Tests\InitialPrefabs.TaskFlow.Tests.csproj
}
