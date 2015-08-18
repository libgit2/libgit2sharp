using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using LibGit2Sharp.Core;
using Xunit;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class BaseFixture : IPostTestDirectoryRemover, IDisposable
    {
        private readonly List<string> directories = new List<string>();

#if LEAKS_IDENTIFYING
        public BaseFixture()
        {
            LeaksContainer.Clear();
        }
#endif

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

        public static DirectoryInfo ResourcesDirectory { get; private set; }

        public static bool IsFileSystemCaseSensitive { get; private set; }

        protected static DateTimeOffset TruncateSubSeconds(DateTimeOffset dto)
        {
            int seconds = dto.ToSecondsSinceEpoch();
            return Epoch.ToDateTimeOffset(seconds, (int)dto.Offset.TotalMinutes);
        }

        private static void SetUpTestEnvironment()
        {
            IsFileSystemCaseSensitive = IsFileSystemCaseSensitiveInternal();

            string initialAssemblyParentFolder = Directory.GetParent(new Uri(typeof(BaseFixture).Assembly.EscapedCodeBase).LocalPath).FullName;

            const string sourceRelativePath = @"../../Resources";
            ResourcesDirectory = new DirectoryInfo(Path.Combine(initialAssemblyParentFolder, sourceRelativePath));

            // Setup standard paths to our test repositories
            BareTestRepoPath = Path.Combine(sourceRelativePath, "testrepo.git");
            StandardTestRepoWorkingDirPath = Path.Combine(sourceRelativePath, "testrepo_wd");
            StandardTestRepoPath = Path.Combine(StandardTestRepoWorkingDirPath, "dot_git");
            ShallowTestRepoPath = Path.Combine(sourceRelativePath, "shallow.git");
            MergedTestRepoWorkingDirPath = Path.Combine(sourceRelativePath, "mergedrepo_wd");
            MergeRenamesTestRepoWorkingDirPath = Path.Combine(sourceRelativePath, "mergerenames_wd");
            MergeTestRepoWorkingDirPath = Path.Combine(sourceRelativePath, "merge_testrepo_wd");
            RevertTestRepoWorkingDirPath = Path.Combine(sourceRelativePath, "revert_testrepo_wd");
            SubmoduleTestRepoWorkingDirPath = Path.Combine(sourceRelativePath, "submodule_wd");
            SubmoduleTargetTestRepoWorkingDirPath = Path.Combine(sourceRelativePath, "submodule_target_wd");
            AssumeUnchangedRepoWorkingDirPath = Path.Combine(sourceRelativePath, "assume_unchanged_wd");
            SubmoduleSmallTestRepoWorkingDirPath = Path.Combine(sourceRelativePath, "submodule_small_wd");

            CleanupTestReposOlderThan(TimeSpan.FromMinutes(15));
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

        protected Repository InitIsolatedRepository(string path = null, bool isBare = false, RepositoryOptions options = null)
        {
            path = path ?? InitNewRepository(isBare);
            options = BuildFakeConfigs(BuildSelfCleaningDirectory(), options);

            return new Repository(path, options);
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

            if (LeaksContainer.TypeNames.Any())
            {
                Assert.False(true, string.Format("Some handles of the following types haven't been properly released: {0}.{1}"
                    + "In order to get some help fixing those leaks, uncomment the define LEAKS_TRACKING in SafeHandleBase.cs{1}"
                    + "and run the tests locally.", string.Join(", ", LeaksContainer.TypeNames), Environment.NewLine));
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

        public RepositoryOptions BuildFakeConfigs(SelfCleaningDirectory scd, RepositoryOptions options = null)
        {
            options = BuildFakeRepositoryOptions(scd, options);

            StringBuilder sb = new StringBuilder()
                .AppendFormat("[Woot]{0}", Environment.NewLine)
                .AppendFormat("this-rocks = global{0}", Environment.NewLine)
                .AppendFormat("[Wow]{0}", Environment.NewLine)
                .AppendFormat("Man-I-am-totally-global = 42{0}", Environment.NewLine);
            File.WriteAllText(options.GlobalConfigurationLocation, sb.ToString());

            sb = new StringBuilder()
                .AppendFormat("[Woot]{0}", Environment.NewLine)
                .AppendFormat("this-rocks = system{0}", Environment.NewLine);
            File.WriteAllText(options.SystemConfigurationLocation, sb.ToString());

            sb = new StringBuilder()
                .AppendFormat("[Woot]{0}", Environment.NewLine)
                .AppendFormat("this-rocks = xdg{0}", Environment.NewLine);
            File.WriteAllText(options.XdgConfigurationLocation, sb.ToString());

            return options;
        }

        private static RepositoryOptions BuildFakeRepositoryOptions(SelfCleaningDirectory scd, RepositoryOptions options = null)
        {
            options = options ?? new RepositoryOptions();

            string confs = Path.Combine(scd.DirectoryPath, "confs");
            Directory.CreateDirectory(confs);

            options.GlobalConfigurationLocation = Path.Combine(confs, "my-global-config");
            options.XdgConfigurationLocation = Path.Combine(confs, "my-xdg-config");
            options.SystemConfigurationLocation = Path.Combine(confs, "my-system-config");

            return options;
        }

        /// <summary>
        /// Creates a configuration file with user.name and user.email set to signature
        /// </summary>
        /// <remarks>The configuration file will be removed automatically when the tests are finished</remarks>
        /// <param name="identity">The identity to use for user.name and user.email</param>
        /// <returns>The path to the configuration file</returns>
        protected string CreateConfigurationWithDummyUser(Identity identity)
        {
            return CreateConfigurationWithDummyUser(identity.Name, identity.Email);
        }

        protected string CreateConfigurationWithDummyUser(string name, string email)
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            string configFilePath = Touch(scd.DirectoryPath, "fake-config");

            using (Configuration config = Configuration.BuildFrom(configFilePath))
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

            return configFilePath;
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

            Directory.CreateDirectory(dir);

            File.WriteAllText(filePath, content ?? string.Empty, encoding ?? Encoding.ASCII);

            return filePath;
        }

        protected static string Touch(string parent, string file, Stream stream)
        {
            Debug.Assert(stream != null);

            string filePath = Path.Combine(parent, file);
            string dir = Path.GetDirectoryName(filePath);
            Debug.Assert(dir != null);

            Directory.CreateDirectory(dir);

            using (var fs = File.Open(filePath, FileMode.Create))
            {
                CopyStream(stream, fs);
                fs.Flush();
            }

            return filePath;
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
            Assert.InRange(reflogEntry.Committer.When, before, DateTimeOffset.Now);
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
