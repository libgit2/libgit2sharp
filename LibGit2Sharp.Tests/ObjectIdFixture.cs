using System;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class ObjectIdFixture
    {
        [Test]
        public void CanConvertOidToSha()
        {
            var bytes = new byte[] {206, 8, 254, 72, 132, 101, 15, 6, 123, 213, 112, 59, 106, 89, 168, 179, 179, 201, 154, 9};

            var id = new ObjectId(bytes);

            id.Sha.ShouldEqual("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            id.ToString().ShouldEqual("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
        }

        [Test]
        public void CanConvertShaToOid()
        {
            var id = new ObjectId("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            id.RawId.ShouldEqual(new byte[] {206, 8, 254, 72, 132, 101, 15, 6, 123, 213, 112, 59, 106, 89, 168, 179, 179, 201, 154, 9});
        }

        [TestCase("Dummy", typeof(ArgumentException))]
        [TestCase("", typeof(ArgumentException))]
        [TestCase("8e", typeof(ArgumentException))]
        [TestCase(null, typeof(ArgumentNullException))]
        [TestCase("ce08fe4884650f067bd5703b6a59a8b3b3c99a09dd", typeof(ArgumentException))]
        public void PreventsFromBuildingWithAnInvalidSha(string malformedSha, Type expectedExceptionType)
        {
            Assert.Throws(expectedExceptionType, () => new ObjectId(malformedSha));
        }

        [Test]
        public void SameObjectsAreEqual()
        {
            var a = new ObjectId("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            var b = new ObjectId("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            (a.Equals(b)).ShouldBeTrue();
            (a == b).ShouldBeTrue();
        }

        [Test]
        public void SameObjectsHaveSameHashCode()
        {
            var a = new ObjectId("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            var b = new ObjectId("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            a.GetHashCode().ShouldEqual(b.GetHashCode());
        }
    }
}