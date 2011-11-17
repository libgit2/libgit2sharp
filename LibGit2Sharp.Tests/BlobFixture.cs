using System.IO;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class BlobFixture : BaseFixture
    {
        [Test]
        public void CanGetBlobAsUtf8()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");

                string text = blob.ContentAsUtf8();
                text.ShouldEqual("hey there\n");
            }
        }

        [Test]
        public void CanGetBlobSize()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                blob.Size.ShouldEqual(10);
            }
        }

        [Test]
        public void CanLookUpBlob()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                blob.ShouldNotBeNull();
            }
        }

        [Test]
        public void CanReadBlobContent()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                byte[] bytes = blob.Content;
                bytes.Length.ShouldEqual(10);

                string content = Encoding.UTF8.GetString(bytes);
                content.ShouldEqual("hey there\n");
            }
        }

        [Test]
        public void CanReadBlobStream()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");

                using (var tr = new StreamReader(blob.ContentStream, Encoding.UTF8))
                {
                    string content = tr.ReadToEnd();
                    content.ShouldEqual("hey there\n");
                }
            }
        }
    }
}
