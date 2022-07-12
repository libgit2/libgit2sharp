using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.OctoVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
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

    [Parameter("Branch name for OctoVersion to use to calculate the version number. Can be set via the environment variable OCTOVERSION_CurrentBranch.",
     Name = "OCTOVERSION_CurrentBranch")]
    readonly string BranchName;

    [Parameter("Whether to auto-detect the branch name - this is okay for a local build, but should not be used under CI.")]
    readonly bool AutoDetectBranch = IsLocalBuild;

    [OctoVersion(UpdateBuildNumber = true, BranchParameter = nameof(BranchName),
        AutoDetectBranchParameter = nameof(AutoDetectBranch), Framework = "net6.0")]
    readonly OctoVersionInfo OctoVersionInfo;

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
                .SetVersion(OctoVersionInfo.FullSemVer)
                .SetInformationalVersion(OctoVersionInfo.InformationalVersion)
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
                .SetProperty("OverridePackageVersion", OctoVersionInfo.FullSemVer)
                .SetNoBuild(true)
            );
        });
}
