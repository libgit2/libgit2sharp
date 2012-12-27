using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LibGit2Sharp.Core;
using Xunit;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class BaseFixture : IPostTestDirectoryRemover, IDisposable
    {
        private readonly List<string> directories = new List<string>();

        static BaseFixture()
        {
            // Do the set up in the static ctor so it only happens once
            SetUpTestEnvironment();
        }

        public static string BareTestRepoPath { get; private set; }
        public static string StandardTestRepoWorkingDirPath { get; private set; }
        public static string StandardTestRepoPath { get; private set; }
        public static DirectoryInfo ResourcesDirectory { get; private set; }

        public static readonly Signature DummySignature = new Signature("Author N. Ame", "him@there.com", TruncateSubSeconds(DateTimeOffset.Now));

        protected static DateTimeOffset TruncateSubSeconds(DateTimeOffset dto)
        {
            int seconds = dto.ToSecondsSinceEpoch();
            return Epoch.ToDateTimeOffset(seconds, (int) dto.Offset.TotalMinutes);
        }

        private static void SetUpTestEnvironment()
        {
            var source = new DirectoryInfo(@"../../Resources");
            ResourcesDirectory = new DirectoryInfo(string.Format(@"Resources/{0}", Guid.NewGuid()));
            var parent = new DirectoryInfo(@"Resources");

            if (parent.Exists)
            {
                DirectoryHelper.DeleteSubdirectories(parent.FullName);
            }

            DirectoryHelper.CopyFilesRecursively(source, ResourcesDirectory);

            // Setup standard paths to our test repositories
            BareTestRepoPath = Path.Combine(ResourcesDirectory.FullName, "testrepo.git");
            StandardTestRepoWorkingDirPath = Path.Combine(ResourcesDirectory.FullName, "testrepo_wd");
            StandardTestRepoPath = Path.Combine(StandardTestRepoWorkingDirPath, ".git");

            // The test repo under source control has its .git folder renamed to dot_git to avoid confusing git,
            // so we need to rename it back to .git in our copy under the target folder
            string tempDotGit = Path.Combine(StandardTestRepoWorkingDirPath, "dot_git");
            Directory.Move(tempDotGit, StandardTestRepoPath);
        }

        protected void CreateCorruptedDeadBeefHead(string repoPath)
        {
            const string deadbeef = "deadbeef";
            string headPath = string.Format("{0}refs/heads/{1}", repoPath, deadbeef);
            File.WriteAllText(headPath, string.Format("{0}{0}{0}{0}{0}\n", deadbeef));
        }

        protected SelfCleaningDirectory BuildSelfCleaningDirectory()
        {
            return new SelfCleaningDirectory(this);
        }

        protected SelfCleaningDirectory BuildSelfCleaningDirectory(string path)
        {
            return new SelfCleaningDirectory(this, path);
        }

        protected TemporaryCloneOfTestRepo BuildTemporaryCloneOfTestRepo()
        {
            return BuildTemporaryCloneOfTestRepo(BareTestRepoPath);
        }

        protected TemporaryCloneOfTestRepo BuildTemporaryCloneOfTestRepo(string path)
        {
            return new TemporaryCloneOfTestRepo(this, path);
        }

        public void Register(string directoryPath)
        {
            directories.Add(directoryPath);
        }

        public void Dispose()
        {
            foreach (string directory in directories)
            {
                DirectoryHelper.DeleteDirectory(directory);
            }
        }

        protected void InconclusiveIf(Func<bool> predicate, string message)
        {
            if (!predicate())
            {
                return;
            }

            throw new SkipException(message);
        }

        protected static void AssertValueInConfigFile(string configFilePath, string regex)
        {
            var text = File.ReadAllText(configFilePath);
            var r = new Regex(regex, RegexOptions.Multiline).Match(text);
            Assert.True(r.Success, text);
        }

        public RepositoryOptions BuildFakeConfigs(SelfCleaningDirectory scd)
        {
            var options = BuildFakeRepositoryOptions(scd);

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

        private static RepositoryOptions BuildFakeRepositoryOptions(SelfCleaningDirectory scd)
        {
            string confs = Path.Combine(scd.DirectoryPath, "confs");
            Directory.CreateDirectory(confs);

            string globalLocation = Path.Combine(confs, "my-global-config");
            string xdgLocation = Path.Combine(confs, "my-xdg-config");
            string systemLocation = Path.Combine(confs, "my-system-config");

            return new RepositoryOptions
            {
                GlobalConfigurationLocation = globalLocation,
                XdgConfigurationLocation = xdgLocation,
                SystemConfigurationLocation = systemLocation,
            };
        }
    }
}
