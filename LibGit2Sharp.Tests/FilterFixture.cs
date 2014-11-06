using System;
using System.Collections.Generic;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class FilterFixture : BaseFixture
    {
        private readonly List<Filter> filtersForCleanUp;

        public FilterFixture()
        {
            filtersForCleanUp = new List<Filter>();
        }

        [Fact]
        public void CanRegisterAndUnregisterFilter()
        {
            var filter = new Filter("radness-filter", "test", 1);
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
            var filterOne = new Filter("filter-two", "test", 1);
            filterOne.Register();
            filterOne.Deregister();

            var filterTwo = new Filter("filter-two", "test", 1);
            filterTwo.Register();
            filterTwo.Deregister();
        }

        [Fact]
        public void CanLookupRegisteredFilterByNameAndValuesAreMarshalCorrectly()
        {
            const string filterName = "filter-three";
            const string attributes = "test";
            const int version = 1;

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
                return -30; //pass through
            };
            string repoPath = InitNewRepository();
            new Filter("test-filter", "filter", 1, callback);
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
                return -30; //pass through
            };
            string repoPath = InitNewRepository();
            var filter = new Filter("test-filter", "filter", 1, callback);
            using (var repo = new Repository(repoPath))
            {
                filter.Register();

                StageNewFile(repo);
            }

            Assert.True(called);
        }

        private static void StageNewFile(Repository repo)
        {
            const string path = "new.txt";
            Touch(repo.Info.WorkingDirectory, path, "null");
            repo.Index.Stage(path);
        }

        private Filter CreateFilterForAutomaticCleanUp(string name, string attributes, int version)
        {
            var filter = new Filter(name, attributes, version);
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
                {
                }
            }
            base.Dispose();
        }
    }
}
