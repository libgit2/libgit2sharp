using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class MergeFixture : BaseFixture
    {
        [Fact]
        public void ANewRepoIsFullyMerged()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            using (var repo = Repository.Init(scd.DirectoryPath))
            {
                Assert.Equal(true, repo.Index.IsFullyMerged);
            }
        }
    }
}
