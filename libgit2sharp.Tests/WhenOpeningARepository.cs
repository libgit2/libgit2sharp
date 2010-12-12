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
        [Ignore("https://github.com/libgit2/libgit2/issues/issue/28")]
        public void ShouldThrowIfPassedANonValidGitDirectory()
        {
            var notAValidRepo = Path.GetTempPath();
            var exception = Assert.Throws<Exception>(() => new Repository(notAValidRepo));
            Assert.Fail("To be finalized.");
        }

        [Test]
        [Ignore("https://github.com/libgit2/libgit2/issues/issue/28")]
        public void ShouldThrowIfPassedANonExistingFolder()
        {
            var notAValidRepo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString());
            var exception = Assert.Throws<Exception>(() => new Repository(notAValidRepo));
            Assert.Fail("To be finalized.");
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