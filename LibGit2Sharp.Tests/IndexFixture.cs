using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class IndexFixture
    {
        [Test]
        public void CanCountEntriesInIndex()
        {
            using (var repo = new Repository(Constants.TestRepoWithWorkingDirPath))
            {
                repo.Index.Count.ShouldEqual(4);
            }
        }
    }
}