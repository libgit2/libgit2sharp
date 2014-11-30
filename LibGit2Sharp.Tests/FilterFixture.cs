using System;
using System.IO;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class FilterFixture : BaseFixture
    {
        private const int GitPassThrough = -30;
        private readonly FilterCallbacks emptyCallbacks = new FilterCallbacks();
        readonly Func<FilterSource, int> successCallback = source => 0;
        readonly Func<FilterSource, string, int> checkSuccess = (source, attr) => 0;

        private const string FilterName = "the-filter";
        const string Attributes = "test";
        const int Version = 1;

        [Fact]
        public void CanRegisterAndUnregisterTheSameFilter()
        {
            var filter = new Filter(FilterName + 1, Attributes, Version, emptyCallbacks);

            filter.Register();
            filter.Deregister();

            filter.Register();
            filter.Deregister();
        }

        [Fact]
        public void CanRegisterAndDeregisterAfterGarbageCollection()
        {
            var filter = new Filter(FilterName + 2, Attributes, Version, emptyCallbacks);
            filter.Register();

            GC.Collect();

            filter.Deregister();
        }

        [Fact]
        public void SameFilterIsEqual()
        {
            var filter = new Filter(FilterName + 3, Attributes, Version, emptyCallbacks);
            Assert.Equal(filter, filter);
        }

        [Fact]
        public void WhenLookingUpFilterResultIsEqual()
        {
            var filter = new Filter(FilterName + 4, Attributes, Version, emptyCallbacks);
            filter.Register();

            var registry = new FilterRegistry();
            Filter lookupByName = registry.LookupByName(FilterName + 4);

            filter.Deregister();
            Assert.Equal(filter, lookupByName);
        }

        [Fact]
        public void LookingUpFilterResultSurvivesGarbageCollection()
        {
            var filter = new Filter(FilterName + 5, Attributes, Version, emptyCallbacks);
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
            var filter = new Filter(FilterName + 6, Attributes, Version, emptyCallbacks);
            filter.Register();

            var registry = new FilterRegistry();
            var lookedUpFilter = registry.LookupByName(FilterName + 6);

            filter.Deregister();

            Assert.Equal(FilterName + 6, lookedUpFilter.Name);
            Assert.Equal(Version, lookedUpFilter.Version);
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

            new Filter(FilterName + 7, Attributes, Version, callbacks);

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
            var filter = new Filter(FilterName + 8, Attributes, Version, callbacks);

            filter.Register();
            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo);
            }
            filter.Deregister();

            Assert.True(called);
        }

        [Fact]
        public void ApplyCallbackMadeWhenCheckCallbackReturnsZero()
        {
            bool called = false;

            Func<FilterSource, int> applyCallback = source =>
            {
                called = true;
                return 0; //successCallback
            };

            string repoPath = InitNewRepository();
            var callbacks = new FilterCallbacks(checkSuccess, applyCallback);
            var filter = new Filter(FilterName + 9, Attributes, Version, callbacks);

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
            Func<FilterSource, int> applyCallback = source =>
            {
                called = true;
                return 0;
            };

            string repoPath = InitNewRepository();
            var callbacks = new FilterCallbacks((source, attr) => GitPassThrough, applyCallback);
            var filter = new Filter(FilterName + 10, Attributes, Version, callbacks);

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

            var filter = new Filter(FilterName + 10, Attributes, Version, callbacks);
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

            var filter = new Filter(FilterName + 11, Attributes, Version, callbacks);

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
            var filter = new Filter(FilterName + 11, Attributes, Version, callbacks);
            filter.Register();

            string repoPath = InitNewRepository();
            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo);
            }
            Assert.False(called);

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
            var filter = new Filter(FilterName + 12, Attributes, Version, callbacks);

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
            var filter = new Filter(FilterName + 13, Attributes, Version, callbacks);

            filter.Register();
            Assert.False(called);

            string repoPath = InitNewRepository();
            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo);
            }

            filter.Deregister();
            Assert.True(called);
        }

        [Fact]
        public void WhenStagingFileCheckIsCalledWithCleanForCorrectPath()
        {
            string repoPath = InitNewRepository();

            var calledWithMode = FilterMode.Smudge;
            string expectedPath;
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

            var filter = new Filter(FilterName + 14, Attributes, Version, callbacks);

            filter.Register();

            using (var repo = new Repository(repoPath))
            {
                expectedPath = StageNewFile(repo);
            }

            filter.Deregister();

            Assert.Equal(FilterMode.Clean, calledWithMode);
            Assert.Equal(expectedPath, actualPath);
            Assert.Equal(Attributes, actualAttributes);
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

            var filter = new Filter(FilterName + 14, Attributes, Version, callbacks);

            filter.Register();

            string expectedPath = CheckoutFileForSmudge(repoPath, branchName);

            filter.Deregister();

            Assert.Equal(FilterMode.Smudge, calledWithMode);
            Assert.Equal(expectedPath, actualPath);
            Assert.Equal(Attributes, actualAttributes);
        }

        [Fact]
        public void WhenStagingFileApplyIsCalledWithCleanForCorrectPath()
        {
            string repoPath = InitNewRepository();

            var calledWithMode = FilterMode.Smudge;
            string expectedPath;
            string actualPath = string.Empty;
            Func<FilterSource, int> callback = source =>
            {
                calledWithMode = source.SourceMode;
                actualPath = source.Path;
                return GitPassThrough;
            };
            var callbacks = new FilterCallbacks(checkSuccess, callback);

            var filter = new Filter(FilterName + 14, Attributes, Version, callbacks);

            filter.Register();

            using (var repo = new Repository(repoPath))
            {
                expectedPath = StageNewFile(repo);
            }

            filter.Deregister();

            Assert.Equal(FilterMode.Clean, calledWithMode);
            Assert.Equal(expectedPath, actualPath);
        }

        [Fact]
        public void CleanToObdb()
        {
            const string branchName = "branch";
            string repoPath = InitNewRepository();

            var calledWithMode = FilterMode.Clean;
            string actualPath = string.Empty;

            Func<FilterSource, int> callback = source =>
            {
                calledWithMode = source.SourceMode;
                actualPath = source.Path;
                return 0;
            };

            var callbacks = new FilterCallbacks(checkSuccess, callback);

            var filter1 = new Filter(FilterName + 14, Attributes, Version, callbacks);

            filter1.Register();

            string expectedPath;
            using (var repo = new Repository(repoPath))
            {
                expectedPath = StageNewFile(repo);

                var commit = repo.Commit("bom", Constants.Signature, Constants.Signature);

                var blob = (Blob)commit.Tree[expectedPath].Target;
                Assert.Equal(6, blob.Size);
                using (var stream = blob.GetContentStream())
                {
                    Assert.Equal(6, stream.Length);
                }

                var textDetected = blob.GetContentText();
                Assert.Equal("777333", textDetected);
            }

            filter1.Deregister();

            // Assert.Equal(FilterMode.Smudge, calledWithMode);
            //Assert.Equal(expectedPath, actualPath);

            string combine = Path.Combine(repoPath, "..", expectedPath);
            var fileInfo = new FileInfo(combine);
            Console.WriteLine("Final read from:" + fileInfo.Name + "Size: " + fileInfo.Length);
            Console.WriteLine("Final contents :" + File.ReadAllText(combine));
        }


        [Fact]
        public void WhenCheckingOutAFileFileApplyIsCalledWithSmudgeForCorrectPath()
        {
            const string branchName = "branch";
            string repoPath = InitNewRepository();

            var calledWithMode = FilterMode.Clean;
            string actualPath = string.Empty;

            Func<FilterSource, int> callback = source =>
            {
                calledWithMode = source.SourceMode;
                actualPath = source.Path;
                return 0;
            };

            var callbacks = new FilterCallbacks(checkSuccess, callback);

            var filter1 = new Filter(FilterName + 14, Attributes, Version, callbacks);
            var filter2 = new Filter(FilterName + 15, Attributes, Version, callbacks);
            var filter3 = new Filter(FilterName + 16, Attributes, Version, callbacks);

            filter1.Register();
            filter2.Register();
            filter3.Register();

            string expectedPath = CheckoutFileForSmudge(repoPath, branchName);

            filter1.Deregister();
            filter2.Deregister();
            filter3.Deregister();

           // Assert.Equal(FilterMode.Smudge, calledWithMode);
            //Assert.Equal(expectedPath, actualPath);

            string combine = Path.Combine(repoPath,"..", expectedPath);
            var fileInfo = new FileInfo(combine);
            Console.WriteLine("Final read from:" + fileInfo.Name + "Size: "+ fileInfo.Length);
            string readAllText = File.ReadAllText(combine);
            Console.WriteLine("Final contents :" + readAllText);
            Assert.Equal("777333", readAllText);
        }

        private static string CheckoutFileForSmudge(string repoPath, string branchName)
        {
            string expectedPath;
            using (var repo = new Repository(repoPath))
            {
                Console.WriteLine("Staging File");
                StageNewFile(repo);
                Console.WriteLine("Comit File");
                repo.Commit("Initial commit", Constants.Signature, Constants.Signature);

                expectedPath = CommitFileOnBranch(repo, branchName);

                Console.WriteLine("Checkout master");
                repo.Branches["master"].Checkout();

                Console.WriteLine("Checkout " +branchName);
                //should smudge file on checkout
                repo.Branches[branchName].Checkout();
            }
            return expectedPath;
        }

        private static string CommitFileOnBranch(Repository repo, string branchName)
        {
            Console.WriteLine("Create Checkout branch " + branchName);
            var branch = repo.CreateBranch(branchName);
            branch.Checkout();

            Console.WriteLine("Stage and comit on " + branchName);
            string expectedPath = StageNewFile(repo);
            repo.Commit("Commit", Constants.Signature, Constants.Signature);
            return expectedPath;
        }

        private static string StageNewFile(IRepository repo)
        {
            string newFilePath = Touch(repo.Info.WorkingDirectory, Guid.NewGuid() + ".txt", "333777");
            var stageNewFile = new FileInfo(newFilePath);

            string combine = Path.Combine(repo.Info.WorkingDirectory, stageNewFile.Name);

            Console.WriteLine("Pre stage from:" + stageNewFile.Name + "Size: " + stageNewFile.Length);
            Console.WriteLine("Pre stage :" + File.ReadAllText(combine));
           
            repo.Stage(newFilePath);

            return stageNewFile.Name;
        }
    }
}
