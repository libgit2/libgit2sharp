﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
        public static string ShallowTestRepoPath { get; private set; }
        public static string MergedTestRepoWorkingDirPath { get; private set; }
        public static string SubmoduleTestRepoWorkingDirPath { get; private set; }
        public static DirectoryInfo ResourcesDirectory { get; private set; }

        public static readonly Signature DummySignature = new Signature("Author N. Ame", "him@there.com", TruncateSubSeconds(DateTimeOffset.Now));

        public static bool IsFileSystemCaseSensitive { get; private set; }

        protected static DateTimeOffset TruncateSubSeconds(DateTimeOffset dto)
        {
            int seconds = dto.ToSecondsSinceEpoch();
            return Epoch.ToDateTimeOffset(seconds, (int) dto.Offset.TotalMinutes);
        }

        private static void SetUpTestEnvironment()
        {
            IsFileSystemCaseSensitive = IsFileSystemCaseSensitiveInternal();

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
            ShallowTestRepoPath = Path.Combine(ResourcesDirectory.FullName, "shallow.git");
            MergedTestRepoWorkingDirPath = Path.Combine(ResourcesDirectory.FullName, "mergedrepo_wd");
            SubmoduleTestRepoWorkingDirPath = Path.Combine(ResourcesDirectory.FullName, "submodule_wd");
        }

        private static bool IsFileSystemCaseSensitiveInternal()
        {
            var mixedPath = Path.Combine(Constants.TemporaryReposPath, "mIxEdCase");

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

        protected string CloneBareTestRepo()
        {
            return Clone(BareTestRepoPath);
        }

        protected string CloneStandardTestRepo()
        {
            return Clone(StandardTestRepoWorkingDirPath);
        }

        protected string CloneMergedTestRepo()
        {
            return Clone(MergedTestRepoWorkingDirPath);
        }

        public string CloneSubmoduleTestRepo()
        {
            var submoduleTarget = Path.Combine(ResourcesDirectory.FullName, "submodule_target_wd");
            return Clone(SubmoduleTestRepoWorkingDirPath, submoduleTarget);
        }

        private string Clone(string sourceDirectoryPath, params string[] additionalSourcePaths)
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

        public void Register(string directoryPath)
        {
            directories.Add(directoryPath);
        }

        public void Dispose()
        {
#if LEAKS
            GC.Collect();
#endif

            foreach (string directory in directories)
            {
                DirectoryHelper.DeleteDirectory(directory);
            }
        }

        protected static void InconclusiveIf(Func<bool> predicate, string message)
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

        protected string Touch(string parent, string file, string content = null)
        {
            var lastIndex = file.LastIndexOf('/');
            if (lastIndex > 0)
            {
                var parents = file.Substring(0, lastIndex);
                Directory.CreateDirectory(Path.Combine(parent, parents));
            }

            var filePath = Path.Combine(parent, file);
            File.AppendAllText(filePath, content ?? string.Empty, Encoding.ASCII);

            return file;
        }

        protected static void AssertReflogEntryIsCreated(IEnumerable<ReflogEntry> reflog, string targetSha, 
            string logMessage, string fromSha = null)
        {
            var reflogEntry = reflog.First();

            if (!string.IsNullOrEmpty(fromSha))
            {
                Assert.Equal(fromSha, reflogEntry.From.Sha);
            }

            Assert.Equal(targetSha, reflogEntry.To.Sha);
            Assert.NotNull(reflogEntry.Commiter.Email);
            Assert.NotNull(reflogEntry.Commiter.Name);
            Assert.Equal(logMessage, reflogEntry.Message);
        }

        protected string Expected(string filename)
        {
            return File.ReadAllText(Path.Combine(ResourcesDirectory.FullName, "expected/" + filename));
        }

        protected string Expected(string filenameFormat, params object[] args)
        {
            return Expected(string.Format(CultureInfo.InvariantCulture, filenameFormat, args));
        }
    }
}
