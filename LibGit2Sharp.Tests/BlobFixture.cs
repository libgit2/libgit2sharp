using System.IO;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class BlobFixture
    {
        [Test]
         public void CanLookUpBlob()
         {
             using (var repo = new Repository(Constants.TestRepoPath))
             {
                 var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                 blob.ShouldNotBeNull();
             }
         }

        [Test]
        public void CanGetBlobSize()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                blob.Size.ShouldEqual(10);
            }            
        }

        [Test]
        public void CanReadBlobContent()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                var bytes = blob.Content;
                bytes.Length.ShouldEqual(10);

                var content = Encoding.UTF8.GetString(bytes);
                content.ShouldEqual("hey there\n");
            }            
        }

        [Test]
        public void CanReadBlobStream()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");

                using (var tr = new StreamReader(blob.ContentStream, Encoding.UTF8))
                {
                    var content = tr.ReadToEnd();
                    content.ShouldEqual("hey there\n");
                }
            }
        }

        [Test]
        public void CanGetBlobAsUtf8()
        {
            using (var repo = new Repository(Constants.TestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                
                var text = blob.ContentAsUtf8();
                text.ShouldEqual("hey there\n");
            }            
        }
    }
}