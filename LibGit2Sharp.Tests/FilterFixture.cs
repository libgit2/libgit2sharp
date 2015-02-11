using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class FilterFixture : BaseFixture
    {
        private const int GitPassThrough = -30;

        readonly Func<FilterSource, IEnumerable<string>, int> checkPassThrough = (source, attr) => GitPassThrough;
        readonly Func<Stream, Stream, int> successCallback = (reader, writer) => 0;
        readonly Func<FilterSource, IEnumerable<string>, int> checkSuccess = (source, attr) => 0;

        private const string FilterName = "the-filter";
        const string Attribute = "test";

        [Fact]
        public void CanRegisterFilterWithSingleAttribute()
        {
            var filter = new EmptyFilter(FilterName, Attribute);
            Assert.Equal(new List<string> { Attribute }, filter.Attributes);
        }

        [Fact]
        public void CanRegisterFilterWithCommaSeparatedListOfAttributes()
        {
            var filter = new EmptyFilter(FilterName, "one,two,three");
            Assert.Equal(new List<string> { "one", "two", "three" }, filter.Attributes);
        }

        [Fact]
        public void CanRegisterAndUnregisterTheSameFilter()
        {
            var filter = new EmptyFilter(FilterName + 1, Attribute);

            GlobalSettings.RegisterFilter(filter);
            GlobalSettings.DeregisterFilter(filter);

            GlobalSettings.RegisterFilter(filter);
            GlobalSettings.DeregisterFilter(filter);
        }

        [Fact]
        public void CanRegisterAndDeregisterAfterGarbageCollection()
        {
            var filter = new EmptyFilter(FilterName + 2, Attribute);
            GlobalSettings.RegisterFilter(filter);

            GC.Collect();

            GlobalSettings.DeregisterFilter(filter);
        }

        [Fact]
        public void SameFilterIsEqual()
        {
            var filter = new EmptyFilter(FilterName + 3, Attribute);
            Assert.Equal(filter, filter);
        }

        [Fact]
        public void CheckCallbackNotMadeWhenFileStagedAndFilterNotRegistered()
        {
            bool called = false;
            Func<FilterSource, IEnumerable<string>, int> callback = (source, attr) =>
            {
                called = true;
                return GitPassThrough;
            };

            string repoPath = InitNewRepository();

            new FakeFilter(FilterName + 4, Attribute, callback);

            using (var repo = CreateTestRepository(repoPath))
            {
                StageNewFile(repo);
            }

            Assert.False(called);
        }

        [Fact]
        public void CheckCallbackMadeWhenFileStaged()
        {
            bool called = false;
            Func<FilterSource, IEnumerable<string>, int> checkCallBack = (source, attr) =>
            {
                called = true;
                return GitPassThrough;
            };
            string repoPath = InitNewRepository();

            var filter = new FakeFilter(FilterName + 5, Attribute, checkCallBack);

            GlobalSettings.RegisterFilter(filter);
            using (var repo = CreateTestRepository(repoPath))
            {
                StageNewFile(repo);
                Assert.True(called);
            }

            GlobalSettings.DeregisterFilter(filter);
        }

        [Fact]
        public void ApplyCallbackMadeWhenCheckCallbackReturnsZero()
        {
            bool called = false;

            Func<Stream, Stream, int> applyCallback = (reader, writer) =>
            {
                called = true;
                return 0; //successCallback
            };

            string repoPath = InitNewRepository();
            var filter = new FakeFilter(FilterName + 6, Attribute, checkSuccess, applyCallback);

            GlobalSettings.RegisterFilter(filter);
            using (var repo = CreateTestRepository(repoPath))
            {
                StageNewFile(repo);
            }

            GlobalSettings.DeregisterFilter(filter);

            Assert.True(called);
        }

        [Fact]
        public void ApplyCallbackNotMadeWhenCheckCallbackReturnsPassThrough()
        {
            bool called = false;

            Func<Stream, Stream, int> applyCallback = (reader, writer) =>
            {
                called = true;
                return 0;
            };

            string repoPath = InitNewRepository();
            var filter = new FakeFilter(FilterName + 7, Attribute, checkPassThrough, applyCallback);

            GlobalSettings.RegisterFilter(filter);
            using (var repo = CreateTestRepository(repoPath))
            {
                StageNewFile(repo);
            }

            GlobalSettings.DeregisterFilter(filter);

            Assert.False(called);
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

            var filter = new FakeFilter(FilterName + 11, Attribute,
                checkSuccess,
                successCallback,
                successCallback,
                initializeCallback);

            GlobalSettings.RegisterFilter(filter);

            Assert.False(called);

            GlobalSettings.DeregisterFilter(filter);
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

            var filter = new FakeFilter(FilterName + 12, Attribute,
                checkSuccess,
                successCallback,
                successCallback,
                initializeCallback);

            GlobalSettings.RegisterFilter(filter);
            Assert.False(called);

            string repoPath = InitNewRepository();
            using (var repo = CreateTestRepository(repoPath))
            {
                StageNewFile(repo);
                Assert.True(called);
            }

            GlobalSettings.DeregisterFilter(filter);
        }

        [Fact]
        public void WhenStagingFileCheckIsCalledWithCleanForCorrectPath()
        {
            string repoPath = InitNewRepository();

            var calledWithMode = FilterMode.Smudge;
            string actualPath = string.Empty;
            IEnumerable<string> actualAttributes = Enumerable.Empty<string>();
            Func<FilterSource, IEnumerable<string>, int> callback = (source, attr) =>
            {
                calledWithMode = source.SourceMode;
                actualPath = source.Path;
                actualAttributes = attr;
                return GitPassThrough;
            };

            var filter = new FakeFilter(FilterName + 13, Attribute, callback);

            GlobalSettings.RegisterFilter(filter);

            using (var repo = CreateTestRepository(repoPath))
            {
                FileInfo expectedFile = StageNewFile(repo);

                Assert.Equal(FilterMode.Clean, calledWithMode);
                Assert.Equal(expectedFile.Name, actualPath);
                Assert.Equal(new List<string> { Attribute }, actualAttributes);
            }

            GlobalSettings.DeregisterFilter(filter);
        }


        [Fact]
        public void WhenCheckingOutAFileFileCheckIsCalledWithSmudgeForCorrectPath()
        {
            const string branchName = "branch";
            string repoPath = InitNewRepository();

            var calledWithMode = FilterMode.Clean;
            string actualPath = string.Empty;
            IEnumerable<string> actualAttributes = Enumerable.Empty<string>();
            Func<FilterSource, IEnumerable<string>, int> callback = (source, attr) =>
            {
                calledWithMode = source.SourceMode;
                actualPath = source.Path;
                actualAttributes = attr;
                return GitPassThrough;
            };

            var filter = new FakeFilter(FilterName + 14, Attribute, callback);

            GlobalSettings.RegisterFilter(filter);

            FileInfo expectedFile = CheckoutFileForSmudge(repoPath, branchName, "hello");
            Assert.Equal(FilterMode.Smudge, calledWithMode);
            Assert.Equal(expectedFile.FullName, actualPath);
            Assert.Equal(new List<string> { Attribute }, actualAttributes);

            GlobalSettings.DeregisterFilter(filter);
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
            var filter = new FakeFilter(FilterName + 15, Attribute, checkSuccess, clean);

            GlobalSettings.RegisterFilter(filter);

            using (var repo = CreateTestRepository(repoPath))
            {
                StageNewFile(repo);
                Assert.True(called);
            }

            GlobalSettings.DeregisterFilter(filter);
        }

        [Fact]
        public void CleanFilterWritesOutputToObjectTree()
        {
            const string decodedInput = "This is a substitution cipher";
            const string encodedInput = "Guvf vf n fhofgvghgvba pvcure";

            string repoPath = InitNewRepository();

            Func<Stream, Stream, int> cleanCallback = SubstitutionCipherFilter.RotateByThirteenPlaces;

            var filter = new FakeFilter(FilterName + 16, Attribute, checkSuccess, cleanCallback);

            GlobalSettings.RegisterFilter(filter);

            using (var repo = CreateTestRepository(repoPath))
            {
                FileInfo expectedFile = StageNewFile(repo, decodedInput);
                var commit = repo.Commit("Clean that file");

                var blob = (Blob)commit.Tree[expectedFile.Name].Target;

                var textDetected = blob.GetContentText();
                Assert.Equal(encodedInput, textDetected);
            }

            GlobalSettings.DeregisterFilter(filter);
        }


        [Fact]
        public void WhenCheckingOutAFileFileSmudgeWritesCorrectFileToWorkingDirectory()
        {
            const string decodedInput = "This is a substitution cipher";
            const string encodedInput = "Guvf vf n fhofgvghgvba pvcure";

            const string branchName = "branch";
            string repoPath = InitNewRepository();

            Func<Stream, Stream, int> smudgeCallback = SubstitutionCipherFilter.RotateByThirteenPlaces;

            var filter = new FakeFilter(FilterName + 17, Attribute, checkSuccess, null, smudgeCallback);
            GlobalSettings.RegisterFilter(filter);

            FileInfo expectedFile = CheckoutFileForSmudge(repoPath, branchName, encodedInput);

            string combine = Path.Combine(repoPath, "..", expectedFile.Name);
            string readAllText = File.ReadAllText(combine);
            Assert.Equal(decodedInput, readAllText);

            GlobalSettings.DeregisterFilter(filter);
        }

        [Fact]
        public void FilterStreamsAreCoherent()
        {
            string repoPath = InitNewRepository();

            bool inputCanWrite = true, inputCanRead = false, inputCanSeek = true;
            bool outputCanWrite = false, outputCanRead = true, outputCanSeek = true;

            Func<Stream, Stream, int> assertor = (input, output) =>
            {
                inputCanRead = input.CanRead;
                inputCanWrite = input.CanWrite;
                inputCanSeek = input.CanSeek;

                outputCanRead = output.CanRead;
                outputCanWrite = output.CanWrite;
                outputCanSeek = output.CanSeek;

                return GitPassThrough;
            };

            var filter = new FakeFilter(FilterName + 18, Attribute, checkSuccess, assertor, assertor);

            GlobalSettings.RegisterFilter(filter);

            using (var repo = CreateTestRepository(repoPath))
            {
                StageNewFile(repo);
            }

            GlobalSettings.DeregisterFilter(filter);

            Assert.True(inputCanRead);
            Assert.False(inputCanWrite);
            Assert.False(inputCanSeek);

            Assert.False(outputCanRead);
            Assert.True(outputCanWrite);
            Assert.False(outputCanSeek);
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
            return new Repository(path, repositoryOptions);
        }

        class EmptyFilter : Filter
        {
            public EmptyFilter(string name, string attributes)
                : base(name, attributes)
            { }
        }

        class FakeFilter : Filter
        {
            private readonly Func<FilterSource, IEnumerable<string>, int> checkCallBack;
            private readonly Func<Stream, Stream, int> cleanCallback;
            private readonly Func<Stream, Stream, int> smudgeCallback;
            private readonly Func<int> initCallback;

            public FakeFilter(string name, string attributes,
                Func<FilterSource, IEnumerable<string>, int> checkCallBack = null,
                Func<Stream, Stream, int> cleanCallback = null,
                Func<Stream, Stream, int> smudgeCallback = null,
                Func<int> initCallback = null)
                : base(name, attributes)
            {
                this.checkCallBack = checkCallBack;
                this.cleanCallback = cleanCallback;
                this.smudgeCallback = smudgeCallback;
                this.initCallback = initCallback;
            }

            protected override int Check(IEnumerable<string> attributes, FilterSource filterSource)
            {
                return checkCallBack != null ? checkCallBack(filterSource, attributes) : base.Check(attributes, filterSource);
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
