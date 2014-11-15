using System;
using System.Collections.Generic;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class FilterFixture : BaseFixture
    {
        private const int GitPassThrough = -30;
        private readonly FilterCallbacks emptyCallbacks = new FilterCallbacks(null, null);
        private readonly List<Filter> filtersForCleanUp;

        public FilterFixture()
        {
            filtersForCleanUp = new List<Filter>();
        }

        [Fact]
        public void CanRegisterAndUnregisterFilter()
        {
            var filter = new Filter("radness-filter", "test", 1, emptyCallbacks);

            filter.Register();
            filter.Deregister();

            filter.Register();
            filter.Deregister();
        }

        [Fact]
        public void CanNotRegisterFilterWithTheSameNameMoreThanOnce()
        {
            var filterOne = CreateFilterForAutomaticCleanUp("filter-one", "test", 1);
            var filterTwo = CreateFilterForAutomaticCleanUp("filter-one", "test", 1);

            filterOne.Register();
            Assert.Throws<InvalidOperationException>(() => filterTwo.Register());
        }

        [Fact]
        public void CanRegisterAndUnregisterTheSameFilter()
        {
            var filterOne = new Filter("filter-two", "test", 1, emptyCallbacks);
            filterOne.Register();
            filterOne.Deregister();

            var filterTwo = new Filter("filter-two", "test", 1, emptyCallbacks);
            filterTwo.Register();
            filterTwo.Deregister();
        }

        [Fact]
        public void CanLookupRegisteredFilterByNameAndValuesAreMarshalCorrectly()
        {
            const string filterName = "filter-three";
            const string attributes = "test";
            const int version = 1;

            string repoPathOne = InitNewRepository();
            string repoPathTwo = InitNewRepository();

            using (var repoOne = new Repository(repoPathOne))
            {
                Console.WriteLine("First");
                StageNewFile(repoOne, 1);
                GC.Collect();
                Console.WriteLine("Second");
                StageNewFile(repoOne, 2);
            }
            var filter = CreateFilterForAutomaticCleanUp(filterName, attributes, version);
            filter.Register();


            var registry = new FilterRegistry();
            var lookedUpFilter = registry.LookupByName(filterName);

            Assert.Equal(filterName, lookedUpFilter.Name);
            Assert.Equal(version, lookedUpFilter.Version);
            Assert.Equal(attributes, lookedUpFilter.Attributes);
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
                StageNewFile(repo);
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
            var filter = new Filter("test-filter", "filter", 1, callbacks);
            using (var repo = new Repository(repoPath))
            {
                filter.Register();

                StageNewFile(repo);
            }

            filter.Deregister();

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
            var filter = new Filter("test-filter", "filter", 1, callbacks);
            using (var repo = new Repository(repoPath))
            {
                filter.Register();

                StageNewFile(repo);
            }

            filter.Deregister();

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
            var filter = new Filter("test-filter", "filter", 1, callbacks);
            using (var repo = new Repository(repoPath))
            {
                filter.Register();

                StageNewFile(repo);
            }
            Assert.False(called);

        }

        private static void StageNewFile(Repository repo)
        {
            const string path = "new.txt";
            Touch(repo.Info.WorkingDirectory, path, "null");
            repo.Index.Stage(path);
        }

        private Filter CreateFilterForAutomaticCleanUp(string name, string attributes, int version)
        {

            var filter = new Filter(name, attributes, version, emptyCallbacks);
            filtersForCleanUp.Add(filter);
            return filter;
        }

        public override void Dispose()
        {
            foreach (var filter in filtersForCleanUp)
            {
                try
                {
                    filter.Deregister();
                }
                catch (LibGit2SharpException)
                { }
            }
            base.Dispose();
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

            var callbacks = new FilterCallbacks(() => 0, ()=> 0, shutdownCallback);
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
