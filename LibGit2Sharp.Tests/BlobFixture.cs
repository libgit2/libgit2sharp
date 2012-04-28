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
                text.ShouldEqual("hey there\n");
            }
        }

        [Fact]
        public void CanGetBlobSize()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                blob.Size.ShouldEqual(10);
            }
        }

        [Fact]
        public void CanLookUpBlob()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                blob.ShouldNotBeNull();
            }
        }

        [Fact]
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

        [Fact]
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

        public static void CopyStream(Stream input, Stream output)
        {
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
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            using (Repository repo = Repository.Init(scd.DirectoryPath))
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
                entry.Id.Sha.ShouldEqual("baae1fb3760a73481ced1fa03dc15614142c19ef");

                var blob = repo.Lookup<Blob>(entry.Id.Sha);

                using (Stream stream = blob.ContentStream)
                using (Stream file = File.OpenWrite(Path.Combine(repo.Info.WorkingDirectory, "small.fromblob.txt")))
                {
                    CopyStream(stream, file);
                }

                repo.Index.Stage("small.fromblob.txt");
                IndexEntry newentry = repo.Index["small.fromblob.txt"];

                newentry.Id.Sha.ShouldEqual("baae1fb3760a73481ced1fa03dc15614142c19ef");
            }
        }
    }
}
