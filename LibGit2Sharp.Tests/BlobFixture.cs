using System.IO;
using System.Linq;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class BlobFixture : BaseFixture
    {
        [Fact]
        public void CanGetBlobAsText()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");

                var text = blob.ContentAsText();

                Assert.Equal("hey there\n", text);
            }
        }

        [Theory]
        [InlineData("ascii", 4, "31 32 33 34")]
        [InlineData("utf-7", 4, "31 32 33 34")]
        [InlineData("utf-8", 7, "EF BB BF 31 32 33 34")]
        [InlineData("utf-16", 10, "FF FE 31 00 32 00 33 00 34 00")]
        [InlineData("unicodeFFFE", 10, "FE FF 00 31 00 32 00 33 00 34")]
        [InlineData("utf-32", 20, "FF FE 00 00 31 00 00 00 32 00 00 00 33 00 00 00 34 00 00 00")]
        public void CanGetBlobAsTextWithVariousEncodings(string encodingName, int expectedContentBytes, string expectedUtf7Chars)
        {
            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var bomFile = "bom.txt";
                var content = "1234";
                var encoding = Encoding.GetEncoding(encodingName);

                var bomPath = Touch(repo.Info.WorkingDirectory, bomFile, content, encoding);
                Assert.Equal(expectedContentBytes, File.ReadAllBytes(bomPath).Length);

                repo.Index.Stage(bomFile);
                var commit = repo.Commit("bom", Constants.Signature, Constants.Signature);

                var blob = (Blob)commit.Tree[bomFile].Target;
                Assert.Equal(expectedContentBytes, blob.Content.Length);

                var textDetected = blob.ContentAsText();
                Assert.Equal(content, textDetected);

                var text = blob.ContentAsText(encoding);
                Assert.Equal(content, text);

                var utf7Chars = blob.ContentAsText(Encoding.UTF7).Select(c => ((int)c).ToString("X2")).ToArray();
                Assert.Equal(expectedUtf7Chars, string.Join(" ", utf7Chars));
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
