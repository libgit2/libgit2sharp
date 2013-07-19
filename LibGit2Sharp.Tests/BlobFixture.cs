using System.IO;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class BlobFixture : BaseFixture
    {
        [Fact]
        public void CanGetBlobAsUtf8()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");

                string text = blob.ContentAsUtf8();
                Assert.Equal("hey there\n", text);
            }
        }

        [Fact]
        public void CanGetBlobSize()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                Assert.Equal(10, blob.Size);
            }
        }

        [Fact]
        public void CanLookUpBlob()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                Assert.NotNull(blob);
            }
        }

        [Fact]
        public void CanReadBlobContent()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                byte[] bytes = blob.Content;
                Assert.Equal(10, bytes.Length);

                string content = Encoding.UTF8.GetString(bytes);
                Assert.Equal("hey there\n", content);
            }
        }

        [Fact]
        public void CanReadBlobStream()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");

                using (var tr = new StreamReader(blob.ContentStream, Encoding.UTF8))
                {
                    string content = tr.ReadToEnd();
                    Assert.Equal("hey there\n", content);
                }
            }
        }

        public static void CopyStream(Stream input, Stream output)
        {
            // Reused from the following Stack Overflow post with permission
            // of Jon Skeet (obtained on 25 Feb 2013)
            // http://stackoverflow.com/questions/411592/how-do-i-save-a-stream-to-a-file/411605#411605
            var buffer = new byte[8*1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }

        [Fact]
        public void CanStageAFileGeneratedFromABlobContentStream()
        {
            string repoPath = InitNewRepository();

            using (var repo = new Repository(repoPath))
            {
                for (int i = 0; i < 5; i++)
                {
                    var sb = new StringBuilder();
                    for (int j = 0; j < 2000; j++)
                    {
                        sb.Append(((i + 1)*(j + 1)).ToString("X8"));
                    }
                    File.AppendAllText(Path.Combine(repo.Info.WorkingDirectory, "small.txt"), sb.ToString());
                }

                repo.Index.Stage("small.txt");
                IndexEntry entry = repo.Index["small.txt"];
                Assert.Equal("baae1fb3760a73481ced1fa03dc15614142c19ef", entry.Id.Sha);

                var blob = repo.Lookup<Blob>(entry.Id.Sha);

                using (Stream stream = blob.ContentStream)
                using (Stream file = File.OpenWrite(Path.Combine(repo.Info.WorkingDirectory, "small.fromblob.txt")))
                {
                    CopyStream(stream, file);
                }

                repo.Index.Stage("small.fromblob.txt");
                IndexEntry newentry = repo.Index["small.fromblob.txt"];

                Assert.Equal("baae1fb3760a73481ced1fa03dc15614142c19ef", newentry.Id.Sha);
            }
        }

        [Fact]
        public void CanTellIfTheBlobContentLooksLikeBinary()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                Assert.Equal(false, blob.IsBinary);
            }
        }
    }
}
