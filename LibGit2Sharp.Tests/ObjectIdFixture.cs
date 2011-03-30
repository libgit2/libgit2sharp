using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class ObjectIdFixture
    {
        [Test]
        public void CanConvertOidToSha()
        {
            var oid = new GitOid {Id = new byte[] {206, 8, 254, 72, 132, 101, 15, 6, 123, 213, 112, 59, 106, 89, 168, 179, 179, 201, 154, 9}};

            var id = new ObjectId(oid);

            id.Sha.ShouldEqual("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            id.ToString().ShouldEqual("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
        }

        [Test]
        public void CanConvertShaToOid()
        {
            var id = new ObjectId("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            id.RawId.ShouldEqual(new byte[] {206, 8, 254, 72, 132, 101, 15, 6, 123, 213, 112, 59, 106, 89, 168, 179, 179, 201, 154, 9});
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