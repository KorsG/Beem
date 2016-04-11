///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////

var isLocal = BuildSystem.IsLocalBuild;

var solutionDir = Directory("./src");
var solutions = new List<FilePath> {
    solutionDir + File("Beem.sln"),
};
var solutionPaths = solutions.Select(solution => solution.GetDirectory());

var artifactsDir = Directory("./artifacts");

var nugetPackageResultDir = artifactsDir + Directory("nuget");

var gitVersionResult = GitVersion(new GitVersionSettings {
        OutputType = GitVersionOutput.Json,
    });

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(() =>
{
    // Executed BEFORE the first task.
    Information("Running tasks...");
    
    /*
    Information("GitVersion.InformationalVersion {0}", gitVersionResult.InformationalVersion);
    Information("GitVersion.AssemblySemVer {0}", gitVersionResult.AssemblySemVer);
    Information("GitVersion.LegacySemVerPadded {0}", gitVersionResult.LegacySemVerPadded);
    Information("GitVersion.SemVer {0}", gitVersionResult.SemVer);
    Information("GitVersion.MajorMinorPatch {0}", gitVersionResult.MajorMinorPatch);
    Information("GitVersion.NuGetVersion {0}", gitVersionResult.NuGetVersion);
    Information("GitVersion.NuGetVersionV2 {0}", gitVersionResult.NuGetVersionV2);
    Information("GitVersion.BranchName {0}", gitVersionResult.BranchName);   
    */
    
});

Teardown(() =>
{
    // Executed AFTER the last task.
    Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASK DEFINITIONS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    // Clean solution directories.
    foreach(var path in solutionPaths)
    {
        Information("Cleaning {0}", path);
        CleanDirectories(path + "/**/bin/" + configuration);
        CleanDirectories(path + "/**/obj/" + configuration);
    }
    
    CleanDirectories(new DirectoryPath[] {
        artifactsDir,
        nugetPackageResultDir
  	});
});

Task("Restore")
    .Does(() =>
{
    // Restore NuGet packages.
    foreach(var solution in solutions)
    {
        Information("Restoring {0}...", solution);
        NuGetRestore(solution);
    }
});

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{
    // Build solutions.
    foreach(var solution in solutions)
    {
        Information("Building {0}", solution);
        MSBuild(solution, settings => 
            settings.SetPlatformTarget(PlatformTarget.MSIL)
                .WithProperty("TreatWarningsAsErrors","false")
                .WithTarget("Build")
                .SetVerbosity(Verbosity.Minimal) // Diagnostic, Minimal, Normal, Quiet, Verbose
                .SetConfiguration(configuration));
    }
});

Task("Create-NuGet-Packages")
    .IsDependentOn("Build")
    .Does(() =>
{
    var nuGetPackSettings = new NuGetPackSettings {
        Version                 = gitVersionResult.NuGetVersion,
        NoPackageAnalysis       = true,
        Properties              = new Dictionary<string, string>
            {
                { "Configuration", configuration },
            },
        OutputDirectory         = nugetPackageResultDir,
        //BasePath                = "./src/Beem/bin/" + configuration,
        //BasePath                = "./",
        //Files                   = new [] 
            //{
                //new NuSpecContent {Source = "src/Beem/bin/$configuration$/Beem.dll", Target = "bin/net45"},
                //new NuSpecContent {Source = "src/Beem/bin/" + configuration + "/Beem.dll", Target = "lib/net45"},
                //new NuSpecContent {Source = "LICENSE.txt"},
            //},
    };
    NuGetPack(solutionDir + File("Beem/Beem.nuspec"), nuGetPackSettings);
});

///////////////////////////////////////////////////////////////////////////////
// TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build");

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);