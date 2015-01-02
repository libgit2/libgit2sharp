using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class PatchStatsFixture : BaseFixture
    {
        [Fact]
        public void CanExtractStatisticsFromDiff()
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                var oldTree = repo.Lookup<Commit>("origin/packed-test").Tree;
                var newTree = repo.Lookup<Commit>("HEAD").Tree;
                var stats = repo.Diff.Compare<PatchStats>(oldTree, newTree);

                Assert.Equal(8, stats.TotalLinesAdded);
                Assert.Equal(1, stats.TotalLinesDeleted);

                var contentStats = stats["new.txt"];
                Assert.Equal(1, contentStats.LinesAdded);
                Assert.Equal(1, contentStats.LinesDeleted);
            }
        }
    }
}
