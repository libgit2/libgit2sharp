using LibGit2Sharp.Core.Compat;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class TupleFixture
    {
        const int integer = 2;
        const string stringy = "hello";

        private readonly Tuple<int, string> sut = new Tuple<int, string>(integer, stringy);

        [Fact]
        public void Properties()
        {
            Assert.Equal(integer, sut.Item1);
            Assert.Equal(stringy, sut.Item2);
        }

        [Fact]
        public void GetHashCodeIsTheSame()
        {
            var sut2 = new Tuple<int, string>(integer, stringy);

            Assert.Equal(sut2.GetHashCode(), sut.GetHashCode());
        }

        [Fact]
        public void GetHashCodeIsDifferent()
        {
            var sut2 = new Tuple<int, string>(integer + 1, stringy);

            Assert.NotEqual(sut2.GetHashCode(), sut.GetHashCode());
        }

        [Fact]
        public void VerifyEquals()
        {
            var sut2 = new Tuple<int, string>(integer, stringy);

            Assert.True(sut.Equals(sut2));
            Assert.True(Equals(sut, sut2));
        }

        [Fact]
        public void VerifyNotEquals()
        {
            var sut2 = new Tuple<int, string>(integer + 1, stringy);

            Assert.False(sut.Equals(sut2));
            Assert.False(Equals(sut, sut2));
        }
    }
}
