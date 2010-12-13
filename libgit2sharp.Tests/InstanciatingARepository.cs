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
        [Ignore]
        public void ShouldAllowPassedRelativeBackslashedPath()
        {
            Assert.Fail("To be finalized.");
        }

        [Test]
        [Ignore]
        public void ShouldAllowPassedRelativeBackslahedPathWithaTrailingBackslash()
        {
            Assert.Fail("To be finalized.");
        }

    
    }
}