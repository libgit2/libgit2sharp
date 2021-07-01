using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Pack);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    AbsolutePath ArtifactsDirectory => RootDirectory / "bin";

    [Solution] readonly Solution Solution;

    // For outline of original build process used by original source repository, check ./azure-pipelines/dotnet.yml
    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            DeleteDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target TestNetCoreApp31 => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetFramework("netcoreapp3.1")  // Dont bother building for full framework
                .SetNoBuild(true)
                .SetFilter("TestCategory!=FailsInCloudTest & TestCategory!=FailsWhileInstrumented")
                .EnableNoRestore());
        });

    Target TestNet6 => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetFramework("net6.0")  // Dont bother building for full framework
                .SetNoBuild(true)
                .SetFilter("TestCategory!=FailsInCloudTest & TestCategory!=FailsWhileInstrumented")
                .EnableNoRestore());
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .DependsOn(TestNetCoreApp31)
        .DependsOn(TestNet6)
        .Executes(() =>
        {
            DotNetPack(s => s
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .SetRunCodeAnalysis(false)
                .SetIncludeSymbols(false)
                .SetPackageId("Octopus.LibGit2Sharp")
                .SetNoBuild(true)
            );
        });
}
