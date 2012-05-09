using System;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class ObjectIdFixture
    {
        private const string validSha1 = "ce08fe4884650f067bd5703b6a59a8b3b3c99a09";
        private const string validSha2 = "de08fe4884650f067bd5703b6a59a8b3b3c99a09";

        [Theory]
        [InlineData("Dummy", typeof(ArgumentException))]
        [InlineData("", typeof(ArgumentException))]
        [InlineData("8e", typeof(ArgumentException))]
        [InlineData(null, typeof(ArgumentNullException))]
        [InlineData(validSha1 + "dd", typeof(ArgumentException))]
        public void PreventsFromBuildingWithAnInvalidSha(string malformedSha, Type expectedExceptionType)
        {
            Assert.Throws(expectedExceptionType, () => new ObjectId(malformedSha));
        }

        [Fact]
        public void CanConvertOidToSha()
        {
            var bytes = new byte[] { 206, 8, 254, 72, 132, 101, 15, 6, 123, 213, 112, 59, 106, 89, 168, 179, 179, 201, 154, 9 };

            var id = new ObjectId(bytes);

            id.Sha.ShouldEqual(validSha1);
            id.ToString().ShouldEqual(validSha1);
        }

        [Fact]
        public void CanConvertShaToOid()
        {
            var id = new ObjectId(validSha1);

            id.RawId.ShouldEqual(new byte[] { 206, 8, 254, 72, 132, 101, 15, 6, 123, 213, 112, 59, 106, 89, 168, 179, 179, 201, 154, 9 });
        }

        [Fact]
        public void CreatingObjectIdWithWrongNumberOfBytesThrows()
        {
            var bytes = new byte[] { 206, 8, 254, 72, 132, 101, 15, 6, 123, 213, 112, 59, 106, 89, 168, 179, 179, 201, 154 };

            Assert.Throws<ArgumentException>(() => { new ObjectId(bytes); });
        }

        [Fact]
        public void DifferentObjectIdsAreEqual()
        {
            var a = new ObjectId(validSha1);
            var b = new ObjectId(validSha2);

            (a.Equals(b)).ShouldBeFalse();
            (b.Equals(a)).ShouldBeFalse();

            (a == b).ShouldBeFalse();
            (a != b).ShouldBeTrue();
        }

        [Fact]
        public void DifferentObjectIdsDoesNotHaveSameHashCode()
        {
            var a = new ObjectId(validSha1);
            var b = new ObjectId(validSha2);

            a.GetHashCode().ShouldNotEqual(b.GetHashCode());
        }

        [Fact]
        public void SimilarObjectIdsAreEqual()
        {
            var a = new ObjectId(validSha1);
            var b = new ObjectId(validSha1);

            (a.Equals(b)).ShouldBeTrue();
            (b.Equals(a)).ShouldBeTrue();

            (a == b).ShouldBeTrue();
            (a != b).ShouldBeFalse();
        }

        [Fact]
        public void SimilarObjectIdsHaveSameHashCode()
        {
            var a = new ObjectId(validSha1);
            var b = new ObjectId(validSha1);

            a.GetHashCode().ShouldEqual(b.GetHashCode());
        }

        [Theory]
        [InlineData("Dummy", false)]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("0", false)]
        [InlineData("01", false)]
        [InlineData("012", false)]
        [InlineData("0123", true)]
        [InlineData("0123456", true)]
        [InlineData(validSha1 + "d", false)]
        [InlineData(validSha1, true)]
        public void TryParse(string maybeSha, bool isValidSha)
        {
            ObjectId parsedObjectId;
            bool result = ObjectId.TryParse(maybeSha, out parsedObjectId);
            result.ShouldEqual(isValidSha);

            if (!result)
            {
                return;
            }

            parsedObjectId.ShouldNotBeNull();
            parsedObjectId.Sha.ShouldEqual(maybeSha);
            maybeSha.StartsWith(parsedObjectId.ToString(3)).ShouldBeTrue();
            parsedObjectId.ToString(42).ShouldEqual(maybeSha);
        }
    }
}
