﻿#addin "Cake.Slack"
///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target          = Argument<string>("target", "Default");
var configuration   = Argument<string>("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var GitHookUri        = EnvironmentVariable("Githookuri");
var GitChannel        = "#cake";
var isLocalBuild        = !AppVeyor.IsRunningOnAppVeyor;
var isPullRequest       = AppVeyor.Environment.PullRequest.IsPullRequest;
var solutions           = GetFiles("./**/*.sln");
var solutionPaths       = solutions.Select(solution => solution.GetDirectory());
var releaseNotes        = ParseReleaseNotes("./ReleaseNotes.md");
var version             = releaseNotes.Version.ToString();
var binDir              = "./src/Cake.Git/bin/" + configuration;
var nugetRoot           = "./nuget/";
var semVersion          = isLocalBuild
                                ? version
                                : string.Concat(version, "-build-", AppVeyor.Environment.Build.Number.ToString("0000"));
var assemblyInfo        = new AssemblyInfoSettings {
                                Title                   = "Cake.Git",
                                Description             = "Cake Git AddIn",
                                Product                 = "Cake.Git",
                                Company                 = "WCOM AB",
                                Version                 = version,
                                FileVersion             = version,
                                InformationalVersion    = semVersion,
                                Copyright               = string.Format("Copyright © WCOM AB {0}", DateTime.Now.Year),
                                CLSCompliant            = true
                            };
var nuGetPackSettings   = new NuGetPackSettings {
                                Id                      = assemblyInfo.Product,
                                Version                 = assemblyInfo.InformationalVersion,
                                Title                   = assemblyInfo.Title,
                                Authors                 = new[] {assemblyInfo.Company},
                                Owners                  = new[] {assemblyInfo.Company},
                                Description             = assemblyInfo.Description,
                                Summary                 = "Cake AddIn that extends Cake with Git SCM features",
                                ProjectUrl              = new Uri("https://github.com/WCOMAB/Cake_Git/"),
                                IconUrl                 = new Uri("http://cdn.rawgit.com/WCOMAB/nugetpackages/master/Chocolatey/icons/wcom.png"),
                                LicenseUrl              = new Uri("https://github.com/WCOMAB/Cake.Git/blob/master/LICENSE"),
                                Copyright               = assemblyInfo.Copyright,
                                ReleaseNotes            = releaseNotes.Notes.ToArray(),
                                Tags                    = new [] {"Cake", "Script", "Build", "Git"},
                                RequireLicenseAcceptance= false,
                                Symbols                 = false,
                                NoPackageAnalysis       = true,
                                Files                   = new [] {
                                                                    new NuSpecContent {Source = "Cake.Git.dll"},
                                                                    new NuSpecContent {Source = "Cake.Git.pdb"},
                                                                    new NuSpecContent {Source = "Cake.Git.xml"},
                                                                    new NuSpecContent {Source = "LibGit2Sharp.dll"},
                                                                    new NuSpecContent {Source = "LibGit2Sharp.xml"},
                                                                    new NuSpecContent {Source = "lib/linux/x86_64/libgit2-75db289.so", Target = "lib/linux/x86_64/libgit2-75db289.so"},
                                                                    new NuSpecContent {Source = "lib/osx/libgit2-75db289.dylib", Target = "lib/osx/libgit2-75db289.dylib"},
                                                                    new NuSpecContent {Source = "lib/win32/x64/git2-75db289.dll", Target = "lib/win32/x64/git2-75db289.dll"},
                                                                    new NuSpecContent {Source = "lib/win32/x64/git2-75db289.pdb", Target = "lib/win32/x64/git2-75db289.pdb"},
                                                                    new NuSpecContent {Source = "lib/win32/x86/git2-75db289.dll", Target = "lib/win32/x86/git2-75db289.dll"},
                                                                    new NuSpecContent {Source = "lib/win32/x86/git2-75db289.pdb", Target = "lib/win32/x86/git2-75db289.pdb"}
                                                                 },
                                BasePath                = binDir,
                                OutputDirectory         = nugetRoot
                            };

if (!isLocalBuild)
{
    AppVeyor.UpdateBuildVersion(semVersion);
}

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
    // Executed BEFORE the first task.
    Information("Running tasks...");

    var buildStartMessage = string.Format(
                            "Building version {0} of {1} ({2}).",
                            version,
                            assemblyInfo.Product,
                            semVersion
                            );

    Information(buildStartMessage);
    if(!string.IsNullOrEmpty(GitHookUri))
    {
        Slack.Chat.PostMessage(
                channel:GitChannel,
                text:buildStartMessage,
                messageSettings:new SlackChatMessageSettings { IncomingWebHookUrl = GitHookUri }
            );
    }
});

Teardown(ctx =>
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
});

Task("Restore")
    .Does(() =>
{
    // Restore all NuGet packages.
    foreach(var solution in solutions)
    {
        Information("Restoring {0}...", solution);
        NuGetRestore(solution);
    }
});

Task("SolutionInfo")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var file = "./src/SolutionInfo.cs";
    CreateAssemblyInfo(file, assemblyInfo);
});

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("SolutionInfo")
    .Does(() =>
{
    // Build all solutions.
    foreach(var solution in solutions)
    {
        Information("Building {0}", solution);
        if (IsRunningOnUnix())
        {
             XBuild(solution, new XBuildSettings()
                .SetConfiguration(configuration)
                .WithProperty("POSIX", "True")
                .WithProperty("TreatWarningsAsErrors", "True")
                .SetVerbosity(Verbosity.Minimal)
            );
        }
        else
        {
            MSBuild(solution, settings =>
                settings.SetPlatformTarget(PlatformTarget.MSIL)
                    .WithProperty("TreatWarningsAsErrors","true")
                    .WithTarget("Build")
                    .SetConfiguration(configuration));
        }
    }
});

Task("Test")
    .IsDependentOn("Build")
    .WithCriteria(() => StringComparer.OrdinalIgnoreCase.Equals(configuration, "Release"))
    .Does(() =>
{
    Action executeTests = ()=> CakeExecuteScript("./test.cake", new CakeSettings{ Arguments = new Dictionary<string, string>{{"target", target == "Default" ? "Default-Tests" : "Local-Tests"}}});
    if (TravisCI.IsRunningOnTravisCI)
    {
        using(TravisCI.Fold("Execute-Tests"))
        {
            executeTests();
            return;
        }
    }

    executeTests();
});

Task("Create-NuGet-Package")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .Does(() =>
{
    if (!DirectoryExists(nugetRoot))
    {
        CreateDirectory(nugetRoot);
    }
    NuGetPack(nuGetPackSettings);
});

Task("Publish-MyGet")
    .IsDependentOn("Create-NuGet-Package")
    .WithCriteria(() => !isLocalBuild)
    .WithCriteria(() => !isPullRequest)
    .Does(() =>
{
    // Resolve the API key.
    var apiKey = EnvironmentVariable("MYGET_API_KEY");
    if(string.IsNullOrEmpty(apiKey)) {
        throw new InvalidOperationException("Could not resolve MyGet API key.");
    }

    var source = EnvironmentVariable("MYGET_SOURCE");
    if(string.IsNullOrEmpty(apiKey)) {
        throw new InvalidOperationException("Could not resolve MyGet source.");
    }

    // Get the path to the package.
    var package = nugetRoot + "Cake.Git." + semVersion + ".nupkg";

    // Push the package.
    NuGetPush(package, new NuGetPushSettings {
        Source = source,
        ApiKey = apiKey
    });
});


Task("Default")
    .IsDependentOn("Create-NuGet-Package");

Task("Local-Tests")
    .IsDependentOn("Create-NuGet-Package");

Task("AppVeyor")
    .IsDependentOn("Publish-MyGet");

Task("Travis")
    .IsDependentOn("Test");

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);
