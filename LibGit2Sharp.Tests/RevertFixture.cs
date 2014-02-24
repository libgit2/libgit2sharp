using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class RevertFixture : BaseFixture
    {
        [Fact]
        public void RevertDoesntCrash()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Reset(ResetMode.Hard);
                repo.Revert(repo.Head.Tip);
            }
        }
    }
}
