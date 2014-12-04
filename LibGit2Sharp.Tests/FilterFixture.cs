using System;
using System.IO;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class FilterFixture : BaseFixture
    {
        private const int GitPassThrough = -30;

        readonly Func<FilterSource, string, int> checkPassThrough = (source, attr) => GitPassThrough;
        readonly Func<GitBufReader, GitBufWriter, int> successCallback = (reader, writer) => 0;
        readonly Func<FilterSource, string, int> checkSuccess = (source, attr) => 0;
        readonly Func<int> cleanUpSuccess = () => 0;
        readonly Func<int> initSuccess = () => 0;

        private const string FilterName = "the-filter";
        const string Attributes = "test";

        class EmptyFilter : Filter
        {
            public EmptyFilter(string name, string attributes)
                : base(name, attributes)
            { }
        }

        class FakeFilter : Filter
        {
            private readonly Func<FilterSource, string, int> checkCallBack;
            private readonly Func<GitBufReader, GitBufWriter, int> cleanCallback;
            private readonly Func<GitBufReader, GitBufWriter, int> smudgeCallback;
            private readonly Func<int> initCallback;
            private readonly Action shutdownCallback;
            private readonly Func<int> cleanUpCallback;

            public FakeFilter(string name, string attributes, 
                Func<FilterSource, string, int> checkCallBack = null, 
                Func<GitBufReader, GitBufWriter, int> cleanCallback = null,
                Func<GitBufReader, GitBufWriter, int> smudgeCallback = null,
                Func<int> initCallback = null,
                Action shutdownCallback = null,
                Func<int> cleanUpCallback = null)
                : base(name, attributes)
            {
                this.checkCallBack = checkCallBack;
                this.cleanCallback = cleanCallback;
                this.smudgeCallback = smudgeCallback;
                this.initCallback = initCallback;
                this.shutdownCallback = shutdownCallback;
                this.cleanUpCallback = cleanUpCallback;
            }

            protected override int Check(string attributes, FilterSource filterSource)
            {
                return cleanCallback != null ? checkCallBack(filterSource, attributes) : base.Check(attributes, filterSource);
            }

            protected override int Clean(GitBufReader input, GitBufWriter output)
            {
                return cleanCallback != null ? cleanCallback(input, output) : base.Clean(input, output);
            }

            protected override int Smudge(GitBufReader input, GitBufWriter output)
            {
                return smudgeCallback != null ? smudgeCallback(input, output) : base.Smudge(input, output);
            }

            protected override void ShutDown()
            {
                if (shutdownCallback != null)
                {
                    shutdownCallback();
                }
                else
                {
                    base.ShutDown();
                }
            }

            protected override int Initialize()
            {
                return initCallback != null ? initCallback() : base.Initialize();
            }

            protected override void CleanUp()
            {
                if (cleanUpCallback != null)
                {
                    cleanUpCallback();
                }
                else
                {
                    base.CleanUp();
                }
            }
        }

        [Fact]
        public void CanRegisterAndUnregisterTheSameFilter()
        {
            var filter = new EmptyFilter(FilterName + 1, Attributes);

            GlobalSettings.RegisterFilter(filter);
            GlobalSettings.DeregisterFilter(filter);

            GlobalSettings.RegisterFilter(filter);
            GlobalSettings.DeregisterFilter(filter);
        }

        [Fact]
        public void CanRegisterAndDeregisterAfterGarbageCollection()
        {
            var filter = new EmptyFilter(FilterName + 2, Attributes);
            GlobalSettings.RegisterFilter(filter);

            GC.Collect();

            GlobalSettings.DeregisterFilter(filter);
        }

        [Fact]
        public void SameFilterIsEqual()
        {
            var filter = new EmptyFilter(FilterName + 3, Attributes);
            Assert.Equal(filter, filter);
        }

        [Fact]
        public void CheckCallbackNotMadeWhenFileStagedAndFilterNotRegistered()
        {
            bool called = false;
            Func<FilterSource, string, int> callback = (source, attr) =>
            {
                called = true;
                return GitPassThrough;
            };

            string repoPath = InitNewRepository();

            new FakeFilter(FilterName + 7, Attributes, callback);

            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo);
            }

            Assert.False(called);
        }

        [Fact]
        public void CheckCallbackMadeWhenFileStaged()
        {
            bool called = false;
            Func<FilterSource, string, int> callback = (source, attr) =>
            {
                called = true;
                return GitPassThrough;
            };
            string repoPath = InitNewRepository();

            var filter = new FakeFilter(FilterName + 8, Attributes, callback);

            GlobalSettings.RegisterFilter(filter);
            using (var repo = new Repository(repoPath))
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

            Func<GitBufReader, GitBufWriter, int> applyCallback = (reader, writer) =>
            {
                called = true;
                return 0; //successCallback
            };

            string repoPath = InitNewRepository();
            var filter = new FakeFilter(FilterName + 9, Attributes, checkSuccess, applyCallback);

            GlobalSettings.RegisterFilter(filter);
            using (var repo = new Repository(repoPath))
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

            Func<GitBufReader, GitBufWriter, int> applyCallback = (reader, writer) =>
            {
                called = true;
                return 0; //successCallback
            };

            string repoPath = InitNewRepository();
            var filter = new FakeFilter(FilterName + 10, Attributes, checkPassThrough, applyCallback);

            GlobalSettings.RegisterFilter(filter);
            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo);
            }

            GlobalSettings.DeregisterFilter(filter);

            Assert.False(called);
        }

        [Fact]
        public void CleanUpIsCalledAfterStage()
        {
            bool called = false;

            Func<int> cleanUpCallback = () =>
            {
                called = true;
                return 0;
            };

            string repoPath = InitNewRepository();

            var filter = new FakeFilter(FilterName + 10, Attributes, 
                checkSuccess, 
                successCallback, 
                successCallback,
                cleanUpSuccess,
                () => { }, 
                cleanUpCallback);

            GlobalSettings.RegisterFilter(filter);

            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo);
            }

            GlobalSettings.DeregisterFilter(filter);

            Assert.True(called);
        }


        [Fact]
        public void ShutdownCallbackNotMadeWhenFilterNeverUsed()
        {
            bool called = false;
            Action shutdownCallback = () =>
            {
                called = true;
            };

            var filter = new FakeFilter(FilterName + 11, Attributes,
                checkSuccess,
                successCallback,
                successCallback,
                cleanUpSuccess,
                shutdownCallback);

            GlobalSettings.RegisterFilter(filter);
            Assert.False(called);

            GlobalSettings.DeregisterFilter(filter);
            Assert.False(called);
        }

        [Fact]
        public void ShutdownCallbackMadeOnDeregisterOfFilter()
        {
            bool called = false;
            Action shutdownCallback = () =>
            {
                called = true;
            };

            var filter = new FakeFilter(FilterName + 11, Attributes,
                checkSuccess,
                successCallback,
                successCallback,
                initSuccess,
                shutdownCallback);

            GlobalSettings.RegisterFilter(filter);

            string repoPath = InitNewRepository();
            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo);
                Assert.False(called);
            }

            GlobalSettings.DeregisterFilter(filter);
            Assert.True(called);
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

            var filter = new FakeFilter(FilterName + 12, Attributes,
                checkSuccess,
                successCallback,
                successCallback,
                initializeCallback,
                () => { },
                cleanUpSuccess);

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

            var filter = new FakeFilter(FilterName + 13, Attributes,
                checkSuccess,
                successCallback,
                successCallback,
                initializeCallback);

            GlobalSettings.RegisterFilter(filter);
            Assert.False(called);

            string repoPath = InitNewRepository();
            using (var repo = new Repository(repoPath))
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
            string actualAttributes = string.Empty;
            Func<FilterSource, string, int> callback = (source, attr) =>
            {
                calledWithMode = source.SourceMode;
                actualPath = source.Path;
                actualAttributes = attr;
                return GitPassThrough;
            };

            var filter = new FakeFilter(FilterName + 14, Attributes, callback);

            GlobalSettings.RegisterFilter(filter);

            using (var repo = new Repository(repoPath))
            {
                string expectedPath = StageNewFile(repo);

                Assert.Equal(FilterMode.Clean, calledWithMode);
                Assert.Equal(expectedPath, actualPath);
                Assert.Equal(Attributes, actualAttributes);
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
            string actualAttributes = string.Empty;
            Func<FilterSource, string, int> callback = (source, attr) =>
            {
                calledWithMode = source.SourceMode;
                actualPath = source.Path;
                actualAttributes = attr;
                return GitPassThrough;
            };

            var filter = new FakeFilter(FilterName + 14, Attributes, callback);

            GlobalSettings.RegisterFilter(filter);

            string expectedPath = CheckoutFileForSmudge(repoPath, branchName);
            Assert.Equal(FilterMode.Smudge, calledWithMode);
            Assert.Equal(expectedPath, actualPath);
            Assert.Equal(Attributes, actualAttributes);

            GlobalSettings.DeregisterFilter(filter);
        }

        [Fact]
        public void WhenStagingFileApplyIsCalledWithCleanForCorrectPath()
        {
            string repoPath = InitNewRepository();
            bool called = false;

            Func<GitBufReader, GitBufWriter, int> smudge = (reader, writer) =>
            {
                called = true;
                return GitPassThrough;
            };
            var filter = new FakeFilter(FilterName + 14, Attributes, checkSuccess, null, smudge);

            GlobalSettings.RegisterFilter(filter);

            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo);
                Assert.True(called);
            }

            GlobalSettings.DeregisterFilter(filter);
        }

        [Fact]
        public void CleanToObdb()
        {
            string repoPath = InitNewRepository();

            string actualPath = string.Empty;

            Func<GitBufReader, GitBufWriter, int> cleanCallback =  (reader, writer) =>
            {
                var input = reader.Read();
                writer.Write(ReverseBytes(input));
                return 0;
            };

            var filter = new FakeFilter(FilterName + 14, Attributes, checkSuccess, cleanCallback);

            GlobalSettings.RegisterFilter(filter);

            using (var repo = new Repository(repoPath))
            {
                string expectedPath = StageNewFile(repo, "333777");

                var commit = repo.Commit("bom", Constants.Signature, Constants.Signature);

                var blob = (Blob)commit.Tree[expectedPath].Target;
                Assert.Equal(6, blob.Size);
                using (var stream = blob.GetContentStream())
                {
                    Assert.Equal(6, stream.Length);
                }

                var textDetected = blob.GetContentText();
                Assert.Equal("777333", textDetected);
                Assert.Equal(expectedPath, actualPath);
            }

            GlobalSettings.DeregisterFilter(filter);
        }


        [Fact]
        public void WhenCheckingOutAFileFileApplyIsCalledWithSmudgeForCorrectPath()
        {
            const string branchName = "branch";
            string repoPath = InitNewRepository();

            string actualPath = string.Empty;

            Func<GitBufReader, GitBufWriter, int> callback = (reader, writer) =>
            {
                var input = reader.Read();
                var reversedInput = ReverseBytes(input);
                writer.Write(reversedInput);
                return 0;
            };

            Func<FilterSource, string, int> checkCallback = (source, s) => 
                source.SourceMode == FilterMode.Smudge ? 0 : GitPassThrough;

            var filter = new FakeFilter(FilterName + 14, Attributes, checkCallback, null, callback );

            GlobalSettings.RegisterFilter(filter);

            string expectedPath = CheckoutFileForSmudge(repoPath, branchName);
            Assert.Equal(expectedPath, actualPath);

            string combine = Path.Combine(repoPath, "..", expectedPath);
            string readAllText = File.ReadAllText(combine);
            Assert.Equal("777333", readAllText);

            GlobalSettings.DeregisterFilter(filter);
        }

        private static string CheckoutFileForSmudge(string repoPath, string branchName)
        {
            string expectedPath;
            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo, "333777");
                repo.Commit("Initial commit", Constants.Signature, Constants.Signature);

                expectedPath = CommitFileOnBranch(repo, branchName);

                repo.Branches["master"].Checkout();

                //should smudge file on checkout
                repo.Branches[branchName].Checkout();
            }
            return expectedPath;
        }

        private static string CommitFileOnBranch(Repository repo, string branchName)
        {
            var branch = repo.CreateBranch(branchName);
            branch.Checkout();

            string expectedPath = StageNewFile(repo, "333777");
            repo.Commit("Commit", Constants.Signature, Constants.Signature);
            return expectedPath;
        }

        private static string StageNewFile(IRepository repo, string contents = "null")
        {
            string newFilePath = Touch(repo.Info.WorkingDirectory, Guid.NewGuid() + ".txt", contents);
            var stageNewFile = new FileInfo(newFilePath);
            repo.Stage(newFilePath);
            return stageNewFile.Name;
        }

        private static byte[] ReverseBytes(byte[] input)
        {
            string inputString = Encoding.UTF8.GetString(input);
            char[] arr = inputString.ToCharArray();
            Array.Reverse(arr);
            var reversed = new string(arr);
            return Encoding.UTF8.GetBytes(reversed);
        }
    }
}
