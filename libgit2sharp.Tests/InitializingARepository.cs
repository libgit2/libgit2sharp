using System.IO;
using NUnit.Framework;

namespace libgit2sharp.Tests
{
    [TestFixture]
    public class InitializingARepository : RepositoryToBeCreatedFixtureBase
    {
        [TestCase(true)]
        [TestCase(false)]
        public void ShouldReturnAValidGitPath(bool isBare)
        {
            var expectedGitDirName = new DirectoryInfo(PathToTempDirectory).Name;

            expectedGitDirName += isBare ? "/" : "/.git/";

            var gitDirPath = Repository.Init(PathToTempDirectory, isBare);
            StringAssert.EndsWith(expectedGitDirName, gitDirPath);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ShouldGenerateAValidRepository(bool isBare)
        {
            var gitDirPath = Repository.Init(PathToTempDirectory, isBare);

            using (var repo = new Repository(gitDirPath))
            {
                Assert.AreEqual(gitDirPath, repo.Details.RepositoryDirectory);
            }
        }
    }
}