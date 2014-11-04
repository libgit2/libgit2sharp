using System;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class SignatureFixture : BaseFixture
    {
        [Theory]
        [InlineData("\0Leading zero")]
        [InlineData("Trailing zero\0")]
        [InlineData("Zero \0inside")]
        [InlineData("\0")]
        [InlineData("\0\0\0")]
        public void CreatingASignatureWithANameContainingZerosThrows(string name)
        {
            Assert.Throws<ArgumentException>(() => new Signature(name, "me@there.com", DateTimeOffset.Now));
        }

        [Theory]
        [InlineData("\0Leading@zero.com")]
        [InlineData("Trailing@zero.com\0")]
        [InlineData("Zero@\0inside.com")]
        [InlineData("\0")]
        [InlineData("\0\0\0")]
        public void CreatingASignatureWithAnEmailContainingZerosThrows(string email)
        {
            Assert.Throws<ArgumentException>(() => new Signature("Me", email, DateTimeOffset.Now));
        }

        [Fact]
        public void CreatingASignatureWithBadParamsThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new Signature(null, "me@there.com", DateTimeOffset.Now));
            Assert.Throws<ArgumentException>(() => new Signature(string.Empty, "me@there.com", DateTimeOffset.Now));
            Assert.Throws<ArgumentNullException>(() => new Signature("Me", null, DateTimeOffset.Now));
            Assert.Throws<ArgumentException>(() => new Signature("Me", string.Empty, DateTimeOffset.Now));
        }
    }
}
