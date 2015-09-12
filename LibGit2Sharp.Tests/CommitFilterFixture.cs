using System;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class CommitFilterFixture : BaseFixture
    {
        [Theory]
        [InlineData("HEAD^", "HEAD^", null)]
        [InlineData("HEAD^..HEAD", "HEAD", "HEAD^")]
        [InlineData("HEAD^..", null, "HEAD^")]
        public void tada(string expression, object includeReachableFrom, object excludeReachableFrom)
        {
            CommitFilter cf = CommitFilter.Parse(expression);

            Assert.Equal(includeReachableFrom, cf.IncludeReachableFrom);
            Assert.Equal(excludeReachableFrom, cf.ExcludeReachableFrom);
        }

        [Fact]
        public void throws()
        {
            Assert.Throws<ArgumentException>(() => CommitFilter.Parse(""));
            Assert.Throws<ArgumentNullException>(() => CommitFilter.Parse(default(string)));
        }
    }
}
