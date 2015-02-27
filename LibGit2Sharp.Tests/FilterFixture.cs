using System;
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class FilterFixture : BaseFixture
    {
        private const int GitPassThrough = -30;

        readonly Func<Stream, Stream, int> successCallback = (reader, writer) => 0;

        private const string FilterName = "the-filter";
        readonly List<FilterAttributeEntry> attributes = new List<FilterAttributeEntry> { new FilterAttributeEntry("test") };

        [Fact]
        public void CanRegisterFilterWithSingleAttribute()
        {
            var filter = new EmptyFilter(FilterName, attributes);
            Assert.Equal( attributes , filter.Attributes);
        }

        [Fact]
        public void CanRegisterAndUnregisterTheSameFilter()
        {
            var filter = new EmptyFilter(FilterName + 1, attributes);

            var registration = GlobalSettings.RegisterFilter(filter);
            GlobalSettings.DeregisterFilter(registration);

            var secondRegistration = GlobalSettings.RegisterFilter(filter);
            GlobalSettings.DeregisterFilter(secondRegistration);
        }

        [Fact]
        public void CanRegisterAndDeregisterAfterGarbageCollection()
        {
            var filter = new EmptyFilter(FilterName + 2, attributes);
            var filterRegistration = GlobalSettings.RegisterFilter(filter);

            GC.Collect();

            GlobalSettings.DeregisterFilter(filterRegistration);
        }

        [Fact]
        public void SameFilterIsEqual()
        {
            var filter = new EmptyFilter(FilterName + 3, attributes);
            Assert.Equal(filter, filter);
        }

        [Fact]
        public void InitCallbackNotMadeWhenFilterNeverUsed()
        {
            bool called = false;
            Func<int> initializeCallback = () =>
            {
                called = true;
                return 0;
            };

            var filter = new FakeFilter(FilterName + 11, attributes,
                successCallback,
                successCallback,
                initializeCallback);

            var filterRegistration = GlobalSettings.RegisterFilter(filter);

            Assert.False(called);

            GlobalSettings.DeregisterFilter(filterRegistration);
        }

        [Fact]
        public void InitCallbackMadeWhenUsingTheFilter()
        {
            bool called = false;
            Func<int> initializeCallback = () =>
            {
                called = true;
                return 0;
            };

            var filter = new FakeFilter(FilterName + 12, attributes,
                successCallback,
                successCallback,
                initializeCallback);

            var filterRegistration = GlobalSettings.RegisterFilter(filter);
            Assert.False(called);

            string repoPath = InitNewRepository();
            using (var repo = CreateTestRepository(repoPath))
            {
                StageNewFile(repo);
                Assert.True(called);
            }

            GlobalSettings.DeregisterFilter(filterRegistration);
        }

        [Fact]
        public void WhenStagingFileApplyIsCalledWithCleanForCorrectPath()
        {
            string repoPath = InitNewRepository();
            bool called = false;

            Func<Stream, Stream, int> clean = (reader, writer) =>
            {
                called = true;
                return GitPassThrough;
            };
            var filter = new FakeFilter(FilterName + 15, attributes, clean);

            var filterRegistration = GlobalSettings.RegisterFilter(filter);

            using (var repo = CreateTestRepository(repoPath))
            {
                StageNewFile(repo);
                Assert.True(called);
            }

            GlobalSettings.DeregisterFilter(filterRegistration);
        }

        [Fact]
        public void CleanFilterWritesOutputToObjectTree()
        {
            const string decodedInput = "This is a substitution cipher";
            const string encodedInput = "Guvf vf n fhofgvghgvba pvcure";

            string repoPath = InitNewRepository();

            Func<Stream, Stream, int> cleanCallback = SubstitutionCipherFilter.RotateByThirteenPlaces;

            var filter = new FakeFilter(FilterName + 16, attributes, cleanCallback);

            var filterRegistration = GlobalSettings.RegisterFilter(filter);

            using (var repo = CreateTestRepository(repoPath))
            {
                FileInfo expectedFile = StageNewFile(repo, decodedInput);
                var commit = repo.Commit("Clean that file");

                var blob = (Blob)commit.Tree[expectedFile.Name].Target;

                var textDetected = blob.GetContentText();
                Assert.Equal(encodedInput, textDetected);
            }

            GlobalSettings.DeregisterFilter(filterRegistration);
        }


        [Fact]
        public void WhenCheckingOutAFileFileSmudgeWritesCorrectFileToWorkingDirectory()
        {
            const string decodedInput = "This is a substitution cipher";
            const string encodedInput = "Guvf vf n fhofgvghgvba pvcure";

            const string branchName = "branch";
            string repoPath = InitNewRepository();

            Func<Stream, Stream, int> smudgeCallback = SubstitutionCipherFilter.RotateByThirteenPlaces;

            var filter = new FakeFilter(FilterName + 17, attributes, null, smudgeCallback);
            var filterRegistration = GlobalSettings.RegisterFilter(filter);

            FileInfo expectedFile = CheckoutFileForSmudge(repoPath, branchName, encodedInput);

            string combine = Path.Combine(repoPath, "..", expectedFile.Name);
            string readAllText = File.ReadAllText(combine);
            Assert.Equal(decodedInput, readAllText);

            GlobalSettings.DeregisterFilter(filterRegistration);
        }

        private FileInfo CheckoutFileForSmudge(string repoPath, string branchName, string content)
        {
            FileInfo expectedPath;
            using (var repo = CreateTestRepository(repoPath))
            {
                StageNewFile(repo, content);

                repo.Commit("Initial commit");

                expectedPath = CommitFileOnBranch(repo, branchName, content);

                repo.Checkout("master");

                repo.Checkout(branchName);
            }
            return expectedPath;
        }

        private static FileInfo CommitFileOnBranch(Repository repo, string branchName, String content)
        {
            var branch = repo.CreateBranch(branchName);
            repo.Checkout(branch.Name);

            FileInfo expectedPath = StageNewFile(repo, content);
            repo.Commit("Commit");
            return expectedPath;
        }

        private static FileInfo StageNewFile(IRepository repo, string contents = "null")
        {
            string newFilePath = Touch(repo.Info.WorkingDirectory, Guid.NewGuid() + ".txt", contents);
            var stageNewFile = new FileInfo(newFilePath);
            repo.Stage(newFilePath);
            return stageNewFile;
        }

        private Repository CreateTestRepository(string path)
        {
            string configPath = CreateConfigurationWithDummyUser(Constants.Signature);
            var repositoryOptions = new RepositoryOptions { GlobalConfigurationLocation = configPath };
            var repository = new Repository(path, repositoryOptions);
            CreateAttributesFile(repository, "* filter=test");
            return repository;
        }

        private static void CreateAttributesFile(IRepository repo, string attributeEntry)
        {
            Touch(repo.Info.WorkingDirectory, ".gitattributes", attributeEntry);
        }

        class EmptyFilter : Filter
        {
            public EmptyFilter(string name, IEnumerable<FilterAttributeEntry> attributes)
                : base(name, attributes)
            { }
        }

        class FakeFilter : Filter
        {
            private readonly Func<Stream, Stream, int> cleanCallback;
            private readonly Func<Stream, Stream, int> smudgeCallback;
            private readonly Func<int> initCallback;

            public FakeFilter(string name, IEnumerable<FilterAttributeEntry> attributes,
                Func<Stream, Stream, int> cleanCallback = null,
                Func<Stream, Stream, int> smudgeCallback = null,
                Func<int> initCallback = null)
                : base(name, attributes)
            {
                this.cleanCallback = cleanCallback;
                this.smudgeCallback = smudgeCallback;
                this.initCallback = initCallback;
            }

            protected override int Clean(string path, Stream input, Stream output)
            {
                return cleanCallback != null ? cleanCallback(input, output) : base.Clean(path, input, output);
            }

            protected override int Smudge(string path, Stream input, Stream output)
            {
                return smudgeCallback != null ? smudgeCallback(input, output) : base.Smudge(path, input, output);
            }

            protected override int Initialize()
            {
                return initCallback != null ? initCallback() : base.Initialize();
            }
        }
    }
}
