using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Tests.TestHelpers;
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
            sut.Item1.ShouldEqual(integer);
            sut.Item2.ShouldEqual(stringy);
        }

        [Fact]
        public void GetHashCodeIsTheSame()
        {
            var sut2 = new Tuple<int, string>(integer, stringy);

            sut.GetHashCode().ShouldEqual(sut2.GetHashCode());
        }

        [Fact]
        public void GetHashCodeIsDifferent()
        {
            var sut2 = new Tuple<int, string>(integer + 1, stringy);

            sut.GetHashCode().ShouldNotEqual(sut2.GetHashCode());
        }

        [Fact]
        public void VerifyEquals()
        {
            var sut2 = new Tuple<int, string>(integer, stringy);

            sut.Equals(sut2).ShouldBeTrue();
            Equals(sut, sut2).ShouldBeTrue();
        }

        [Fact]
        public void VerifyNotEquals()
        {
            var sut2 = new Tuple<int, string>(integer + 1, stringy);

            sut.Equals(sut2).ShouldBeFalse();
            Equals(sut, sut2).ShouldBeFalse();
        }
    }
}
