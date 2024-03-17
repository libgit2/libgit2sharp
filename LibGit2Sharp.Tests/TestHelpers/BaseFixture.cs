using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class BaseFixture : IPostTestDirectoryRemover, IDisposable
    {
        private readonly List<string> directories = new List<string>();

        public BaseFixture()
        {
            BuildFakeConfigs(this);

#if LEAKS_IDENTIFYING
            Core.LeaksContainer.Clear();
#endif
        }

        static BaseFixture()
        {
            // Do the set up in the static ctor so it only happens once
            SetUpTestEnvironment();
        }

        public static string BareTestRepoPath { get; private set; }
        public static string StandardTestRepoWorkingDirPath { get; private set; }
        public static string StandardTestRepoPath { get; private set; }
        public static string ShallowTestRepoPath { get; private set; }
        public static string MergedTestRepoWorkingDirPath { get; private set; }
        public static string MergeTestRepoWorkingDirPath { get; private set; }
        public static string MergeRenamesTestRepoWorkingDirPath { get; private set; }
        public static string RevertTestRepoWorkingDirPath { get; private set; }
        public static string SubmoduleTestRepoWorkingDirPath { get; private set; }
        private static string SubmoduleTargetTestRepoWorkingDirPath { get; set; }
        private static string AssumeUnchangedRepoWorkingDirPath { get; set; }
        public static string SubmoduleSmallTestRepoWorkingDirPath { get; set; }
        public static string WorktreeTestRepoWorkingDirPath { get; private set; }
        public static string WorktreeTestRepoWorktreesDirPath { get; private set; }
        public static string PackBuilderTestRepoPath { get; private set; }

        public static DirectoryInfo ResourcesDirectory { get; private set; }

        public static bool IsFileSystemCaseSensitive { get; private set; }

        protected static DateTimeOffset TruncateSubSeconds(DateTimeOffset dto)
        {
            var seconds = dto.ToUnixTimeSeconds();
            return DateTimeOffset.FromUnixTimeSeconds(seconds).ToOffset(dto.Offset);
        }

        private static void SetUpTestEnvironment()
        {
            IsFileSystemCaseSensitive = IsFileSystemCaseSensitiveInternal();

            var resourcesPath = Environment.GetEnvironmentVariable("LIBGIT2SHARP_RESOURCES");

            if (resourcesPath == null)
            {
#if NETFRAMEWORK
                resourcesPath = Path.Combine(Directory.GetParent(new Uri(typeof(BaseFixture).GetTypeInfo().Assembly.CodeBase).LocalPath).FullName, "Resources");
#else
                resourcesPath = Path.Combine(Directory.GetParent(typeof(BaseFixture).GetTypeInfo().Assembly.Location).FullName, "Resources");
#endif
            }

            ResourcesDirectory = new DirectoryInfo(resourcesPath);

            // Setup standard paths to our test repositories
            BareTestRepoPath = Path.Combine(ResourcesDirectory.FullName, "testrepo.git");
            StandardTestRepoWorkingDirPath = Path.Combine(ResourcesDirectory.FullName, "testrepo_wd");
            StandardTestRepoPath = Path.Combine(StandardTestRepoWorkingDirPath, "dot_git");
            ShallowTestRepoPath = Path.Combine(ResourcesDirectory.FullName, "shallow.git");
            MergedTestRepoWorkingDirPath = Path.Combine(ResourcesDirectory.FullName, "mergedrepo_wd");
            MergeRenamesTestRepoWorkingDirPath = Path.Combine(ResourcesDirectory.FullName, "mergerenames_wd");
            MergeTestRepoWorkingDirPath = Path.Combine(ResourcesDirectory.FullName, "merge_testrepo_wd");
            RevertTestRepoWorkingDirPath = Path.Combine(ResourcesDirectory.FullName, "revert_testrepo_wd");
            SubmoduleTestRepoWorkingDirPath = Path.Combine(ResourcesDirectory.FullName, "submodule_wd");
            SubmoduleTargetTestRepoWorkingDirPath = Path.Combine(ResourcesDirectory.FullName, "submodule_target_wd");
            AssumeUnchangedRepoWorkingDirPath = Path.Combine(ResourcesDirectory.FullName, "assume_unchanged_wd");
            SubmoduleSmallTestRepoWorkingDirPath = Path.Combine(ResourcesDirectory.FullName, "submodule_small_wd");
            PackBuilderTestRepoPath = Path.Combine(ResourcesDirectory.FullName, "packbuilder_testrepo_wd");
            WorktreeTestRepoWorkingDirPath = Path.Combine(ResourcesDirectory.FullName, "worktree", "testrepo_wd");
            WorktreeTestRepoWorktreesDirPath = Path.Combine(ResourcesDirectory.FullName, "worktree", "worktrees");

            CleanupTestReposOlderThan(TimeSpan.FromMinutes(15));
        }

        public static void BuildFakeConfigs(IPostTestDirectoryRemover dirRemover)
        {
            var scd = new SelfCleaningDirectory(dirRemover);

            string global = null, xdg = null, system = null, programData = null;
            BuildFakeRepositoryOptions(scd, out global, out xdg, out system, out programData);

            StringBuilder sb = new StringBuilder()
                .AppendFormat("[Woot]{0}", Environment.NewLine)
                .AppendFormat("this-rocks = global{0}", Environment.NewLine)
                .AppendFormat("[Wow]{0}", Environment.NewLine)
                .AppendFormat("Man-I-am-totally-global = 42{0}", Environment.NewLine);
            File.WriteAllText(Path.Combine(global, ".gitconfig"), sb.ToString());

            sb = new StringBuilder()
                .AppendFormat("[Woot]{0}", Environment.NewLine)
                .AppendFormat("this-rocks = system{0}", Environment.NewLine);
            File.WriteAllText(Path.Combine(system, "gitconfig"), sb.ToString());

            sb = new StringBuilder()
                .AppendFormat("[Woot]{0}", Environment.NewLine)
                .AppendFormat("this-rocks = xdg{0}", Environment.NewLine);
            File.WriteAllText(Path.Combine(xdg, "config"), sb.ToString());

            sb = new StringBuilder()
                .AppendFormat("[Woot]{0}", Environment.NewLine)
                .AppendFormat("this-rocks = programdata{0}", Environment.NewLine);
            File.WriteAllText(Path.Combine(programData, "config"), sb.ToString());

            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Global, global);
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Xdg, xdg);
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.System, system);
            GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.ProgramData, programData);
        }

        private static void CleanupTestReposOlderThan(TimeSpan olderThan)
        {
            var oldTestRepos = new DirectoryInfo(Constants.TemporaryReposPath)
                .EnumerateDirectories()
                .Where(di => di.CreationTimeUtc < DateTimeOffset.Now.Subtract(olderThan))
                .Select(di => di.FullName);

            foreach (var dir in oldTestRepos)
            {
                DirectoryHelper.DeleteDirectory(dir);
            }
        }

        private static bool IsFileSystemCaseSensitiveInternal()
        {
            var mixedPath = Path.Combine(Constants.TemporaryReposPath, "mIxEdCase-" + Path.GetRandomFileName());

            if (Directory.Exists(mixedPath))
            {
                Directory.Delete(mixedPath);
            }

            Directory.CreateDirectory(mixedPath);
            bool isInsensitive = Directory.Exists(mixedPath.ToLowerInvariant());

            Directory.Delete(mixedPath);

            return !isInsensitive;
        }

        protected void CreateCorruptedDeadBeefHead(string repoPath)
        {
            const string deadbeef = "deadbeef";
            string headPath = string.Format("refs/heads/{0}", deadbeef);

            Touch(repoPath, headPath, string.Format("{0}{0}{0}{0}{0}\n", deadbeef));
        }

        protected SelfCleaningDirectory BuildSelfCleaningDirectory()
        {
            return new SelfCleaningDirectory(this);
        }

        protected SelfCleaningDirectory BuildSelfCleaningDirectory(string path)
        {
            return new SelfCleaningDirectory(this, path);
        }

        protected string SandboxBareTestRepo()
        {
            return Sandbox(BareTestRepoPath);
        }

        protected string SandboxStandardTestRepo()
        {
            return Sandbox(StandardTestRepoWorkingDirPath);
        }

        protected string SandboxMergedTestRepo()
        {
            return Sandbox(MergedTestRepoWorkingDirPath);
        }

        protected string SandboxStandardTestRepoGitDir()
        {
            return Sandbox(Path.Combine(StandardTestRepoWorkingDirPath));
        }

        protected string SandboxMergeTestRepo()
        {
            return Sandbox(MergeTestRepoWorkingDirPath);
        }

        protected string SandboxRevertTestRepo()
        {
            return Sandbox(RevertTestRepoWorkingDirPath);
        }

        public string SandboxSubmoduleTestRepo()
        {
            return Sandbox(SubmoduleTestRepoWorkingDirPath, SubmoduleTargetTestRepoWorkingDirPath);
        }

        public string SandboxAssumeUnchangedTestRepo()
        {
            return Sandbox(AssumeUnchangedRepoWorkingDirPath);
        }

        public string SandboxSubmoduleSmallTestRepo()
        {
            var path = Sandbox(SubmoduleSmallTestRepoWorkingDirPath, SubmoduleTargetTestRepoWorkingDirPath);
            Directory.CreateDirectory(Path.Combine(path, "submodule_target_wd"));

            return path;
        }

        public string SandboxWorktreeTestRepo()
        {
            return Sandbox(WorktreeTestRepoWorkingDirPath, WorktreeTestRepoWorktreesDirPath);
        }

        protected string SandboxPackBuilderTestRepo()
        {
            return Sandbox(PackBuilderTestRepoPath);
        }

        protected string Sandbox(string sourceDirectoryPath, params string[] additionalSourcePaths)
        {
            var scd = BuildSelfCleaningDirectory();
            var source = new DirectoryInfo(sourceDirectoryPath);

            var clonePath = Path.Combine(scd.DirectoryPath, source.Name);
            DirectoryHelper.CopyFilesRecursively(source, new DirectoryInfo(clonePath));

            foreach (var additionalPath in additionalSourcePaths)
            {
                var additional = new DirectoryInfo(additionalPath);
                var targetForAdditional = Path.Combine(scd.DirectoryPath, additional.Name);

                DirectoryHelper.CopyFilesRecursively(additional, new DirectoryInfo(targetForAdditional));
            }

            return clonePath;
        }

        protected string InitNewRepository(bool isBare = false)
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            return Repository.Init(scd.DirectoryPath, isBare);
        }

        public void Register(string directoryPath)
        {
            directories.Add(directoryPath);
        }

        public virtual void Dispose()
        {
            foreach (string directory in directories)
            {
                DirectoryHelper.DeleteDirectory(directory);
            }

#if LEAKS_IDENTIFYING
            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (Core.LeaksContainer.TypeNames.Any())
            {
                Assert.Fail(string.Format("Some handles of the following types haven't been properly released: {0}.{1}"
                    + "In order to get some help fixing those leaks, uncomment the define LEAKS_TRACKING in Libgit2Object.cs{1}"
                    + "and run the tests locally.", string.Join(", ", Core.LeaksContainer.TypeNames), Environment.NewLine));
            }
#endif
        }

        protected static void InconclusiveIf(Func<bool> predicate, string message)
        {
            if (!predicate())
            {
                return;
            }

            throw new SkipException(message);
        }

        protected void RequiresDotNetOrMonoGreaterThanOrEqualTo(System.Version minimumVersion)
        {
            Type type = Type.GetType("Mono.Runtime");

            if (type == null)
            {
                // We're running on top of .Net
                return;
            }

            MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);

            if (displayName == null)
            {
                throw new InvalidOperationException("Cannot access Mono.RunTime.GetDisplayName() method.");
            }

            var version = (string)displayName.Invoke(null, null);

            System.Version current;

            try
            {
                current = new System.Version(version.Split(' ')[0]);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Cannot parse Mono version '{0}'.", version), e);
            }

            InconclusiveIf(() => current < minimumVersion,
                string.Format(
                    "Current Mono version is {0}. Minimum required version to run this test is {1}.",
                    current, minimumVersion));
        }

        protected static void AssertValueInConfigFile(string configFilePath, string regex)
        {
            var text = File.ReadAllText(configFilePath);
            var r = new Regex(regex, RegexOptions.Multiline).Match(text);
            Assert.True(r.Success, text);
        }

        private static void BuildFakeRepositoryOptions(SelfCleaningDirectory scd, out string global, out string xdg, out string system, out string programData)
        {
            string confs = Path.Combine(scd.DirectoryPath, "confs");
            Directory.CreateDirectory(confs);

            global = Path.Combine(confs, "my-global-config");
            Directory.CreateDirectory(global);
            xdg = Path.Combine(confs, "my-xdg-config");
            Directory.CreateDirectory(xdg);
            system = Path.Combine(confs, "my-system-config");
            Directory.CreateDirectory(system);
            programData = Path.Combine(confs, "my-programdata-config");
            Directory.CreateDirectory(programData);
        }

        /// <summary>
        /// Creates a configuration file with user.name and user.email set to signature
        /// </summary>
        /// <remarks>The configuration file will be removed automatically when the tests are finished</remarks>
        /// <param name="identity">The identity to use for user.name and user.email</param>
        /// <returns>The path to the configuration file</returns>
        protected void CreateConfigurationWithDummyUser(Repository repo, Identity identity)
        {
            CreateConfigurationWithDummyUser(repo, identity.Name, identity.Email);
        }

        protected void CreateConfigurationWithDummyUser(Repository repo, string name, string email)
        {
            Configuration config = repo.Config;
            {
                if (name != null)
                {
                    config.Set("user.name", name);
                }

                if (email != null)
                {
                    config.Set("user.email", email);
                }
            }
        }

        /// <summary>
        /// Asserts that the commit has been authored and committed by the specified signature
        /// </summary>
        /// <param name="commit">The commit</param>
        /// <param name="identity">The identity to compare author and commiter to</param>
        protected void AssertCommitIdentitiesAre(Commit commit, Identity identity)
        {
            Assert.Equal(identity.Name, commit.Author.Name);
            Assert.Equal(identity.Email, commit.Author.Email);
            Assert.Equal(identity.Name, commit.Committer.Name);
            Assert.Equal(identity.Email, commit.Committer.Email);
        }

        protected static string Touch(string parent, string file, string content = null, Encoding encoding = null)
        {
            string filePath = Path.Combine(parent, file);
            string dir = Path.GetDirectoryName(filePath);
            Debug.Assert(dir != null);

            var newFile = !File.Exists(filePath);

            Directory.CreateDirectory(dir);

            File.WriteAllText(filePath, content ?? string.Empty, encoding ?? Encoding.ASCII);

            //Workaround for .NET Core 1.x behavior where all newly created files have execute permissions set.
            //https://github.com/dotnet/corefx/issues/13342
            if (Constants.IsRunningOnUnix && newFile)
            {
                RemoveExecutePermissions(filePath, newFile);
            }

            return filePath;
        }

        protected static string Touch(string parent, string file, Stream stream)
        {
            Debug.Assert(stream != null);

            string filePath = Path.Combine(parent, file);
            string dir = Path.GetDirectoryName(filePath);
            Debug.Assert(dir != null);

            var newFile = !File.Exists(filePath);

            Directory.CreateDirectory(dir);

            using (var fs = File.Open(filePath, FileMode.Create))
            {
                CopyStream(stream, fs);
                fs.Flush();
            }

            //Work around .NET Core 1.x behavior where all newly created files have execute permissions set.
            //https://github.com/dotnet/corefx/issues/13342
            if (Constants.IsRunningOnUnix && newFile)
            {
                RemoveExecutePermissions(filePath, newFile);
            }

            return filePath;
        }

        private static void RemoveExecutePermissions(string filePath, bool newFile)
        {
            var process = Process.Start("chmod", $"644 {filePath}");
            process.WaitForExit();
        }

        protected string Expected(string filename)
        {
            return File.ReadAllText(Path.Combine(ResourcesDirectory.FullName, "expected/" + filename));
        }

        protected string Expected(string filenameFormat, params object[] args)
        {
            return Expected(string.Format(CultureInfo.InvariantCulture, filenameFormat, args));
        }

        protected static void AssertRefLogEntry(IRepository repo, string canonicalName,
                                                string message, ObjectId @from, ObjectId to,
                                                Identity committer, DateTimeOffset before)
        {
            var reflogEntry = repo.Refs.Log(canonicalName).First();

            Assert.Equal(to, reflogEntry.To);
            Assert.Equal(message, reflogEntry.Message);
            Assert.Equal(@from ?? ObjectId.Zero, reflogEntry.From);

            Assert.Equal(committer.Email, reflogEntry.Committer.Email);

            // When verifying the timestamp range, give a little more room on the range.
            // Git or file system datetime truncation seems to cause these stamps to jump up to a second earlier
            // than we expect. See https://github.com/libgit2/libgit2sharp/issues/1764
            var low = before - TimeSpan.FromSeconds(1);
            var high = DateTimeOffset.Now.TruncateMilliseconds() + TimeSpan.FromSeconds(1);
            Assert.InRange(reflogEntry.Committer.When, low, high);
        }

        protected static void EnableRefLog(IRepository repository, bool enable = true)
        {
            repository.Config.Set("core.logAllRefUpdates", enable);
        }

        public static void CopyStream(Stream input, Stream output)
        {
            // Reused from the following Stack Overflow post with permission
            // of Jon Skeet (obtained on 25 Feb 2013)
            // http://stackoverflow.com/questions/411592/how-do-i-save-a-stream-to-a-file/411605#411605
            var buffer = new byte[8 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }

        public static bool StreamEquals(Stream one, Stream two)
        {
            int onebyte, twobyte;

            while ((onebyte = one.ReadByte()) >= 0 && (twobyte = two.ReadByte()) >= 0)
            {
                if (onebyte != twobyte)
                    return false;
            }

            return true;
        }

        public void AssertBelongsToARepository<T>(IRepository repo, T instance)
            where T : IBelongToARepository
        {
            Assert.Same(repo, ((IBelongToARepository)instance).Repository);
        }

        protected void CreateAttributesFile(IRepository repo, string attributeEntry)
        {
            Touch(repo.Info.WorkingDirectory, ".gitattributes", attributeEntry);
        }
    }
}
