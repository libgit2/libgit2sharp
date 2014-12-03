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
        private readonly FilterCallbacks emptyCallbacks = new FilterCallbacks();
        readonly Func<FilterSource,GitBufReader, GitBufWriter, int> successCallback = (source, reader, writer) => 0;
        readonly Func<FilterSource, string, int> checkSuccess = (source, attr) => 0;

        private const string FilterName = "the-filter";
        const string Attributes = "test";

        [Fact]
        public void CanRegisterAndUnregisterTheSameFilter()
        {
            var filter = new Filter(FilterName + 1, Attributes, emptyCallbacks);

            filter.Register();
            filter.Deregister();

            filter.Register();
            filter.Deregister();
        }

        [Fact]
        public void CanRegisterAndDeregisterAfterGarbageCollection()
        {
            var filter = new Filter(FilterName + 2, Attributes, emptyCallbacks);
            filter.Register();

            GC.Collect();

            filter.Deregister();
        }

        [Fact]
        public void SameFilterIsEqual()
        {
            var filter = new Filter(FilterName + 3, Attributes, emptyCallbacks);
            Assert.Equal(filter, filter);
        }

        [Fact]
        public void WhenLookingUpFilterResultIsEqual()
        {
            var filter = new Filter(FilterName + 4, Attributes, emptyCallbacks);
            filter.Register();

            var registry = new FilterRegistry();
            Filter lookupByName = registry.LookupByName(FilterName + 4);

            filter.Deregister();
            Assert.Equal(filter, lookupByName);
        }

        [Fact]
        public void LookingUpFilterResultSurvivesGarbageCollection()
        {
            var filter = new Filter(FilterName + 5, Attributes, emptyCallbacks);
            filter.Register();

            GC.Collect();

            var registry = new FilterRegistry();
            Filter lookupByName = registry.LookupByName(FilterName + 5);

            filter.Deregister();
            Assert.Equal(filter, lookupByName);
        }

        [Fact]
        public void CanLookupRegisteredFilterByNameAndValuesAreMarshalCorrectly()
        {
            var filter = new Filter(FilterName + 6, Attributes, emptyCallbacks);
            filter.Register();

            var registry = new FilterRegistry();
            var lookedUpFilter = registry.LookupByName(FilterName + 6);

            filter.Deregister();

            Assert.Equal(FilterName + 6, lookedUpFilter.Name);
            Assert.Equal(Attributes, lookedUpFilter.Attributes);
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
            var callbacks = new FilterCallbacks(callback);

            new Filter(FilterName + 7, Attributes, callbacks);

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
            var callbacks = new FilterCallbacks(callback);
            var filter = new Filter(FilterName + 8, Attributes, callbacks);

            filter.Register();
            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo);
                Assert.True(called);
            }

            filter.Deregister();
        }

        [Fact]
        public void ApplyCallbackMadeWhenCheckCallbackReturnsZero()
        {
            bool called = false;

            Func<FilterSource, GitBufReader, GitBufWriter, int> applyCallback =
                (source, reader, writer) =>
                {
                    called = true;
                    return 0; //successCallback
                };

            string repoPath = InitNewRepository();
            var callbacks = new FilterCallbacks(checkSuccess, applyCallback);
            var filter = new Filter(FilterName + 9, Attributes, callbacks);

            filter.Register();
            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo);
            }
            filter.Deregister();

            Assert.True(called);
        }

        [Fact]
        public void ApplyCallbackNotMadeWhenCheckCallbackReturnsPassThrough()
        {
            bool called = false;

            Func<FilterSource, GitBufReader, GitBufWriter, int> applyCallback =
                (source, reader, writer) =>
                {
                    called = true;
                    return 0; //successCallback
                };

            string repoPath = InitNewRepository();
            var callbacks = new FilterCallbacks((source, attr) => GitPassThrough, applyCallback);
            var filter = new Filter(FilterName + 10, Attributes, callbacks);

            filter.Register();
            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo);
            }
            filter.Deregister();

            Assert.False(called);
        }

        [Fact]
        public void CleanUpIsCalledAfterStage()
        {
            bool called = false;

            Action cleanUpCallback = () =>
            {
                called = true;
            };

            string repoPath = InitNewRepository();
            var callbacks = new FilterCallbacks(checkSuccess, successCallback, () => { }, () => 0, cleanUpCallback);

            var filter = new Filter(FilterName + 10, Attributes, callbacks);
            filter.Register();

            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo);
            }
            filter.Deregister();

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

            var callbacks = new FilterCallbacks(checkSuccess, successCallback, shutdownCallback);

            var filter = new Filter(FilterName + 11, Attributes, callbacks);

            filter.Register();
            Assert.False(called);

            filter.Deregister();
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

            var callbacks = new FilterCallbacks(checkSuccess, successCallback, shutdownCallback);
            var filter = new Filter(FilterName + 11, Attributes, callbacks);
            filter.Register();

            string repoPath = InitNewRepository();
            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo);
                Assert.False(called);
            }

            filter.Deregister();
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

            var callbacks = new FilterCallbacks(checkSuccess, successCallback, () => { }, initializeCallback);
            var filter = new Filter(FilterName + 12, Attributes, callbacks);

            filter.Register();

            Assert.False(called);

            filter.Deregister();
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

            var callbacks = new FilterCallbacks(checkSuccess, successCallback, () => { }, initializeCallback);
            var filter = new Filter(FilterName + 13, Attributes, callbacks);

            filter.Register();
            Assert.False(called);

            string repoPath = InitNewRepository();
            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo);
                Assert.True(called);
            }

            filter.Deregister();
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
            var callbacks = new FilterCallbacks(callback);

            var filter = new Filter(FilterName + 14, Attributes, callbacks);

            filter.Register();

            using (var repo = new Repository(repoPath))
            {
                string expectedPath = StageNewFile(repo);

                Assert.Equal(FilterMode.Clean, calledWithMode);
                Assert.Equal(expectedPath, actualPath);
                Assert.Equal(Attributes, actualAttributes);
            }

            filter.Deregister();
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
            var callbacks = new FilterCallbacks(callback);

            var filter = new Filter(FilterName + 14, Attributes, callbacks);

            filter.Register();

            string expectedPath = CheckoutFileForSmudge(repoPath, branchName);
            Assert.Equal(FilterMode.Smudge, calledWithMode);
            Assert.Equal(expectedPath, actualPath);
            Assert.Equal(Attributes, actualAttributes);

            filter.Deregister();
        }

        [Fact]
        public void WhenStagingFileApplyIsCalledWithCleanForCorrectPath()
        {
            string repoPath = InitNewRepository();

            var calledWithMode = FilterMode.Smudge;
            string actualPath = string.Empty;
            Func<FilterSource, GitBufReader, GitBufWriter, int> callback = (source, reader, writer) =>
            {
                calledWithMode = source.SourceMode;
                actualPath = source.Path;
                return GitPassThrough;
            };
            var callbacks = new FilterCallbacks(checkSuccess, callback);

            var filter = new Filter(FilterName + 14, Attributes, callbacks);

            filter.Register();

            using (var repo = new Repository(repoPath))
            {
                string expectedPath = StageNewFile(repo);

                Assert.Equal(FilterMode.Clean, calledWithMode);
                Assert.Equal(expectedPath, actualPath);
            }

            filter.Deregister();
        }

        [Fact]
        public void CleanToObdb()
        {
            string repoPath = InitNewRepository();

            var calledWithMode = FilterMode.Smudge;
            string actualPath = string.Empty;

            Func<FilterSource, GitBufReader, GitBufWriter, int> callback = (source, reader, writer) =>
            {
                calledWithMode = source.SourceMode;
                actualPath = source.Path;
                var input = reader.Read();
                writer.Write(ReverseBytes(input));
                return 0;
            };

            var callbacks = new FilterCallbacks(checkSuccess, callback);

            var filter1 = new Filter(FilterName + 14, Attributes, callbacks);

            filter1.Register();

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
                Assert.Equal(FilterMode.Clean, calledWithMode);
                Assert.Equal(expectedPath, actualPath);
            }

            filter1.Deregister();
        }


        [Fact]
        public void WhenCheckingOutAFileFileApplyIsCalledWithSmudgeForCorrectPath()
        {
            const string branchName = "branch";
            string repoPath = InitNewRepository();

            var calledWithMode = FilterMode.Clean;
            string actualPath = string.Empty;

            Func<FilterSource, GitBufReader, GitBufWriter, int> callback = (source, reader, writer) =>
            {
                calledWithMode = source.SourceMode;
                if (source.SourceMode == FilterMode.Smudge)
                {
                    var input = reader.Read();
                    var reversedInput = ReverseBytes(input);
                    writer.Write(reversedInput);
                }
                actualPath = source.Path;
                return 0;
            };

            Func<FilterSource, string, int> checkCallback = (source, s) => 
                source.SourceMode == FilterMode.Smudge ? 0 : GitPassThrough;

            var callbacks = new FilterCallbacks(checkCallback, callback);

            var filter1 = new Filter(FilterName + 14, Attributes, callbacks);

            filter1.Register();

            string expectedPath = CheckoutFileForSmudge(repoPath, branchName);
            Assert.Equal(FilterMode.Smudge, calledWithMode);
            Assert.Equal(expectedPath, actualPath);

            string combine = Path.Combine(repoPath, "..", expectedPath);
            string readAllText = File.ReadAllText(combine);
            Assert.Equal("777333", readAllText);

            filter1.Deregister();
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
