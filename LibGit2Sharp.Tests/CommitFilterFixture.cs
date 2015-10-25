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
        [InlineData("..HEAD^", "HEAD^", null)]
        public void tada(string expression, object includeReachableFrom, object excludeReachableFrom)
        {
            CommitFilter cf = CommitFilter.Parse(expression);

            Assert.Equal(includeReachableFrom, cf.IncludeReachableFrom);
            Assert.Equal(excludeReachableFrom, cf.ExcludeReachableFrom);
        }

        [Theory]
        [InlineData("..")]
        [InlineData("...")]
        public void tada2(string expression)
        {
            Assert.Throws<InvalidOperationException>(() => CommitFilter.Parse(expression));
        }

        [Fact]
        public void throws()
        {
            Assert.Throws<ArgumentException>(() => CommitFilter.Parse(""));
            Assert.Throws<ArgumentNullException>(() => CommitFilter.Parse(default(string)));
        }
    }
}
