using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class TupleFixture
    {
        const int integer = 2;
        const string stringy = "hello";

        private readonly Tuple<int, string> sut = new Tuple<int, string>(integer, stringy);

        [Test]
        public void Properties()
        {
            sut.Item1.ShouldEqual(integer);
            sut.Item2.ShouldEqual(stringy);
        }

        [Test]
        public void GetHashCodeIsTheSame()
        {
            var sut2 = new Tuple<int, string>(integer, stringy);

            sut.GetHashCode().ShouldEqual(sut2.GetHashCode());
        }

        [Test]
        public void GetHashCodeIsDifferent()
        {
            var sut2 = new Tuple<int, string>(integer + 1, stringy);

            sut.GetHashCode().ShouldNotEqual(sut2.GetHashCode());
        }

        [Test]
        public void Equals()
        {
            var sut2 = new Tuple<int, string>(integer, stringy);

            sut.Equals(sut2).ShouldBeTrue();
            Equals(sut, sut2).ShouldBeTrue();
        }

        [Test]
        public void NotEquals()
        {
            var sut2 = new Tuple<int, string>(integer + 1, stringy);

            sut.Equals(sut2).ShouldBeFalse();
            Equals(sut, sut2).ShouldBeFalse();
        }
    }
}
