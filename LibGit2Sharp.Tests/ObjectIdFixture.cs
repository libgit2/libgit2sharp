using System;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class ObjectIdFixture
    {
        private const string validSha1 = "ce08fe4884650f067bd5703b6a59a8b3b3c99a09";
        private const string validSha2 = "de08fe4884650f067bd5703b6a59a8b3b3c99a09";

        [TestCase("Dummy", typeof(ArgumentException))]
        [TestCase("", typeof(ArgumentException))]
        [TestCase("8e", typeof(ArgumentException))]
        [TestCase(null, typeof(ArgumentNullException))]
        [TestCase(validSha1 + "dd", typeof(ArgumentException))]
        public void PreventsFromBuildingWithAnInvalidSha(string malformedSha, Type expectedExceptionType)
        {
            Assert.Throws(expectedExceptionType, () => new ObjectId(malformedSha));
        }

        [Test]
        public void CanConvertOidToSha()
        {
            var bytes = new byte[] { 206, 8, 254, 72, 132, 101, 15, 6, 123, 213, 112, 59, 106, 89, 168, 179, 179, 201, 154, 9 };

            var id = new ObjectId(bytes);

            id.Sha.ShouldEqual(validSha1);
            id.ToString().ShouldEqual(validSha1);
        }

        [Test]
        public void CanConvertShaToOid()
        {
            var id = new ObjectId(validSha1);

            id.RawId.ShouldEqual(new byte[] { 206, 8, 254, 72, 132, 101, 15, 6, 123, 213, 112, 59, 106, 89, 168, 179, 179, 201, 154, 9 });
        }

        [Test]
        public void CreatingObjectIdWithWrongNumberOfBytesThrows()
        {
            var bytes = new byte[] { 206, 8, 254, 72, 132, 101, 15, 6, 123, 213, 112, 59, 106, 89, 168, 179, 179, 201, 154 };

            Assert.Throws<ArgumentException>(() => { new ObjectId(bytes); });
        }

        [Test]
        public void DifferentObjectIdsAreEqual()
        {
            var a = new ObjectId(validSha1);
            var b = new ObjectId(validSha2);

            (a.Equals(b)).ShouldBeFalse();
            (b.Equals(a)).ShouldBeFalse();

            (a == b).ShouldBeFalse();
            (a != b).ShouldBeTrue();
        }

        [Test]
        public void DifferentObjectIdsDoesNotHaveSameHashCode()
        {
            var a = new ObjectId(validSha1);
            var b = new ObjectId(validSha2);

            a.GetHashCode().ShouldNotEqual(b.GetHashCode());
        }

        [Test]
        public void SimilarObjectIdsAreEqual()
        {
            var a = new ObjectId(validSha1);
            var b = new ObjectId(validSha1);

            (a.Equals(b)).ShouldBeTrue();
            (b.Equals(a)).ShouldBeTrue();

            (a == b).ShouldBeTrue();
            (a != b).ShouldBeFalse();
        }

        [Test]
        public void SimilarObjectIdsHaveSameHashCode()
        {
            var a = new ObjectId(validSha1);
            var b = new ObjectId(validSha1);

            a.GetHashCode().ShouldEqual(b.GetHashCode());
        }

        [TestCase("Dummy", false)]
        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase("0", false)]
        [TestCase("01", false)]
        [TestCase("012", false)]
        [TestCase("0123", true)]
        [TestCase("0123456", true)]
        [TestCase(validSha1 + "d", false)]
        [TestCase(validSha1, true)]
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
