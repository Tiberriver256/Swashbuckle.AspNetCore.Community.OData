var target = Argument("Target", "Default");
var configuration =
    HasArgument("Configuration") ? Argument<string>("Configuration") :
    EnvironmentVariable("Configuration") is not null ? EnvironmentVariable("Configuration") :
    "Release";

var artefactsDirectory = Directory("./Artefacts");

Task("Clean")
    .Description("Cleans the artefacts, bin and obj directories.")
    .Does(() =>
    {
        CleanDirectory(artefactsDirectory);
        DeleteDirectories(GetDirectories("**/bin"), new DeleteDirectorySettings() { Force = true, Recursive = true });
        DeleteDirectories(GetDirectories("**/obj"), new DeleteDirectorySettings() { Force = true, Recursive = true });
    });

Task("Restore")
    .Description("Restores NuGet packages.")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        DotNetCoreRestore();
    });

Task("Build")
    .Description("Builds the solution.")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        DotNetCoreBuild(
            ".",
            new DotNetCoreBuildSettings()
            {
                Configuration = configuration,
                NoRestore = true,
            });
    });

Task("Test")
    .Description("Runs unit tests.")
    .DoesForEach(GetFiles("./Tests/**/*.csproj"), project =>
    {
        var processSettings = new ProcessSettings
        {
            Arguments = $"test --project \"{project.FullPath}\" --configuration {configuration} --no-build --no-restore",
        };

        var exitCode = StartProcess("dotnet", processSettings);
        if (exitCode != 0)
        {
            throw new Exception($"dotnet test failed for {project}");
        }
    });

Task("Pack")
    .Description("Creates NuGet packages and outputs them to the artefacts directory.")
    .Does(() =>
    {
        DotNetCorePack(
            "./Source/Swashbuckle.AspNetCore.Community.OData/Swashbuckle.AspNetCore.Community.OData.csproj",
            new DotNetCorePackSettings()
            {
                Configuration = configuration,
                IncludeSymbols = true,
                MSBuildSettings = new DotNetCoreMSBuildSettings()
                {
                    ContinuousIntegrationBuild = !BuildSystem.IsLocalBuild,
                },
                NoBuild = true,
                NoRestore = true,
                OutputDirectory = artefactsDirectory,
            });
    });

Task("Default")
    .Description("Cleans, restores NuGet packages, builds the solution, runs unit tests and then creates NuGet packages.")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack");

RunTarget(target);