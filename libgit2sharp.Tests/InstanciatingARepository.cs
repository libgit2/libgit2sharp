using System;
using System.IO;
using NUnit.Framework;

namespace libgit2sharp.Tests
{
    [TestFixture]
    public class InstanciatingARepository
    {
        const string PathToRepository = "../../Resources/testrepo.git";

        [Test]
        public void ShouldThrowIfPassedANonValidGitDirectory()
        {
            var notAValidRepo = Path.GetTempPath();
            Assert.Throws<NotAValidRepositoryException>(() => new Repository(notAValidRepo));
        }

        [Test]
        public void ShouldThrowIfPassedANonExistingFolder()
        {
            var notAValidRepo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString());
            Assert.Throws<NotAValidRepositoryException>(() => new Repository(notAValidRepo));
        }

        [Test]
        public void ShouldAcceptPlatormNativeRelativePath()
        {
            string repoPath = PathToRepository.Replace('/', Path.DirectorySeparatorChar);

            AssertRepositoryPath(repoPath);
        }

        [Test]
        public void ShouldAcceptPlatormNativeAbsolutePath()
        {
            string repoPath = Path.GetFullPath(PathToRepository);

            AssertRepositoryPath(repoPath);
        }

        [Test]
        public void ShouldAcceptPlatormNativeRelativePathWithATrailingDirectorySeparatorChar()
        {
            string repoPath = PathToRepository.Replace('/', Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            AssertRepositoryPath(repoPath);
        }

        [Test]
        public void ShouldAcceptPlatormNativeAbsolutePathWithATrailingDirectorySeparatorChar()
        {
            string repoPath = Path.GetFullPath(PathToRepository) + Path.DirectorySeparatorChar;

            AssertRepositoryPath(repoPath);
        }

        private static void AssertRepositoryPath(string repoPath)
        {
            var expected = new DirectoryInfo(repoPath);

            DirectoryInfo current;
            
            using (var repo = new Repository(repoPath))
            {
                current = new DirectoryInfo(repo.Details.RepositoryDirectory);
            }

            Assert.AreEqual(expected, current);
        }
    }
}