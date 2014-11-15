using System;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class FilterFixture : BaseFixture
    {
        private const int GitPassThrough = -30;
        private readonly FilterCallbacks emptyCallbacks = new FilterCallbacks(null, null);

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
        public void TestingCallbacks()
        {
            string repoPathOne = InitNewRepository();
            string repoPathTwo = InitNewRepository();
            var filter = new Filter(FilterName + 7, Attributes, Version, emptyCallbacks);

            using (var repoOne = new Repository(repoPathOne))
            {
                Console.WriteLine("First");
                StageNewFile(repoOne, 1);
                GC.Collect();
                Console.WriteLine("Second");
                StageNewFile(repoOne, 2);
            }

            filter.Register();

            var lookup = new FilterRegistry();
            Filter lookupByName = lookup.LookupByName(FilterName + 7);

            Assert.Equal(Attributes, lookupByName.Attributes);


            Filter lookupByName1 = lookup.LookupByName(FilterName + 7);

            Assert.Equal(Attributes, lookupByName1.Attributes);

            GC.Collect();

            using (var repoTwo = new Repository(repoPathTwo))
            {
                Console.WriteLine("Third");
                StageNewFile(repoTwo, 3);

                GC.Collect();
            }

            lookupByName.Deregister();
        }

        [Fact]
        public void CheckCallbackNotMadeWhenFileStagedAndFilterNotRegistered()
        {
            bool called = false;
            Func<int> callback = () =>
            {
                called = true;
                return GitPassThrough;
            };
            string repoPath = InitNewRepository();
            var callbacks = new FilterCallbacks(callback);
            new Filter("test-filter", "filter", 1, callbacks);
            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo, 55);
            }

            Assert.False(called);
        }

        [Fact]
        public void CheckCallbackMadeWhenFileStaged()
        {
            bool called = false;
            Func<int> callback = () =>
            {
                called = true;
                return GitPassThrough;
            };
            string repoPath = InitNewRepository();
            var callbacks = new FilterCallbacks(callback);
            var test = new Filter("test-filter33", "filter", 1, callbacks);
            using (var repo = new Repository(repoPath))
            {
                test.Register();

                StageNewFile(repo, 22);
            }

            test.Deregister();

            Assert.True(called);
        }

        [Fact]
        public void ApplyCallbackMadeWhenCheckCallbackReturnsZero()
        {
            bool called = false;

            Func<int> applyCallback = () =>
            {
                called = true;
                return 0; //success
            };

            string repoPath = InitNewRepository();
            var callbacks = new FilterCallbacks(() => 0, applyCallback);
            var test = new Filter("test-filter55", "filter", 1, callbacks);
            using (var repo = new Repository(repoPath))
            {
                test.Register();

                StageNewFile(repo, 44);
            }

            test.Deregister();
            Assert.True(called);
        }

        [Fact]
        public void ApplyCallbackNotMadeWhenCheckCallbackReturnsPassThrough()
        {
            bool called = false;
            Func<int> applyCallback = () =>
            {
                called = true;
                return 0;
            };

            string repoPath = InitNewRepository();
            var callbacks = new FilterCallbacks(() => GitPassThrough, applyCallback);
            var test = new Filter("test-filter", "filter", 1, callbacks);
            using (var repo = new Repository(repoPath))
            {
                test.Register();

                StageNewFile(repo, 77);
            }

            test.Deregister();
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
            var callbacks = new FilterCallbacks(() => 0, () => 0, () => { }, () => 0, cleanUpCallback);
            var test = new Filter("test-filter55", "filter", 1, callbacks);
            using (var repo = new Repository(repoPath))
            {
                test.Register();

                StageNewFile(repo, 44);
            }

            test.Deregister();
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

            var callbacks = new FilterCallbacks(() => 0, () => 0, shutdownCallback);
            var filter = new Filter("test-filter", "filter", 1, callbacks);

            filter.Register();

            Assert.False(called);

            filter.Deregister();
            Assert.False(called);
        }

        [Fact]
        public void ShutdownCallbackMadeWhenDeregisteringFilter()
        {
            bool called = false;
            Action shutdownCallback = () =>
            {
                called = true;
            };

            var callbacks = new FilterCallbacks(() => 0, () => 0, shutdownCallback);
            var filter = new Filter("test-filter", "filter", 1, callbacks);

            filter.Register();

            string repoPath = InitNewRepository();
            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo, 77);
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

            var callbacks = new FilterCallbacks(() => 0, () => 0, () => { }, initializeCallback);
            var filter = new Filter("test-filter", "filter", 1, callbacks);

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

            var callbacks = new FilterCallbacks(() => 0, () => 0, () => { }, initializeCallback);
            var filter = new Filter("test-filter", "filter", 1, callbacks);

            filter.Register();
            Assert.False(called);

            string repoPath = InitNewRepository();
            using (var repo = new Repository(repoPath))
            {
                StageNewFile(repo, 77);
            }
            filter.Deregister();
            Assert.True(called);
        }

        private static void StageNewFile(Repository repo, int n)
        {
            string path = "new" + n + ".txt";
            Touch(repo.Info.WorkingDirectory, path, "null");
            repo.Index.Stage(path);
        }
    }
}
