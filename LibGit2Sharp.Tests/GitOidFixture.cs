using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class GitOidFixture
    {
        [Test]
        public void CanConvertOidToSha()
        {
            var oid = new GitOid {Id = new byte[] {206, 8, 254, 72, 132, 101, 15, 6, 123, 213, 112, 59, 106, 89, 168, 179, 179, 201, 154, 9}};
            oid.ToSha().ShouldEqual("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            oid.ToString().ShouldEqual("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
        }

        [Test]
        public void CanConvertShaToOid()
        {
            GitOid.FromSha("ce08fe4884650f067bd5703b6a59a8b3b3c99a09").Id.ShouldEqual(new byte[] {206, 8, 254, 72, 132, 101, 15, 6, 123, 213, 112, 59, 106, 89, 168, 179, 179, 201, 154, 9});
        }

        [Test]
        public void SameObjectsAreEqual()
        {
            var a = GitOid.FromSha("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            var b = GitOid.FromSha("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            (a.Equals(b)).ShouldBeTrue();
        }

        [Test]
        public void SameObjectsHaveSameHashCode()
        {
            var a = GitOid.FromSha("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            var b = GitOid.FromSha("ce08fe4884650f067bd5703b6a59a8b3b3c99a09");
            a.GetHashCode().ShouldEqual(b.GetHashCode());
        }
    }
}