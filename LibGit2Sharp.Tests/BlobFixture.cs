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
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");

                var text = blob.GetContentText();

                Assert.Equal("hey there\n", text);
            }
        }

        [SkippableTheory]
        [InlineData("false", "hey there\n")]
        [InlineData("input", "hey there\n")]
        [InlineData("true", "hey there\r\n")]
        public void CanGetBlobAsFilteredText(string autocrlf, string expectedText)
        {
            SkipIfNotSupported(autocrlf);

            var path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Config.Set("core.autocrlf", autocrlf);

                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");

                var text = blob.GetContentText(new FilteringOptions("foo.txt"));

                Assert.Equal(expectedText, text);
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
            var path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var bomFile = "bom.txt";
                var content = "1234";
                var encoding = Encoding.GetEncoding(encodingName);

                var bomPath = Touch(repo.Info.WorkingDirectory, bomFile, content, encoding);
                Assert.Equal(expectedContentBytes, File.ReadAllBytes(bomPath).Length);

                repo.Stage(bomFile);
                var commit = repo.Commit("bom", Constants.Signature, Constants.Signature);

                var blob = (Blob)commit.Tree[bomFile].Target;
                Assert.Equal(expectedContentBytes, blob.Size);
                using (var stream = blob.GetContentStream())
                {
                    Assert.Equal(expectedContentBytes, stream.Length);
                }

                var textDetected = blob.GetContentText();
                Assert.Equal(content, textDetected);

                var text = blob.GetContentText(encoding);
                Assert.Equal(content, text);

                var utf7Chars = blob.GetContentText(Encoding.UTF7).Select(c => ((int)c).ToString("X2")).ToArray();
                Assert.Equal(expectedUtf7Chars, string.Join(" ", utf7Chars));
            }
        }

        [Fact]
        public void CanGetBlobSize()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                Assert.Equal(10, blob.Size);
            }
        }

        [Fact]
        public void CanLookUpBlob()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                Assert.NotNull(blob);
            }
        }

        [Fact]
        public void CanReadBlobStream()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");

                var contentStream = blob.GetContentStream();
                Assert.Equal(blob.Size, contentStream.Length);

                using (var tr = new StreamReader(contentStream, Encoding.UTF8))
                {
                    string content = tr.ReadToEnd();
                    Assert.Equal("hey there\n", content);
                }
            }
        }

        [SkippableTheory]
        [InlineData("false", "hey there\n")]
        [InlineData("input", "hey there\n")]
        [InlineData("true", "hey there\r\n")]
        public void CanReadBlobFilteredStream(string autocrlf, string expectedContent)
        {
            SkipIfNotSupported(autocrlf);

            var path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                repo.Config.Set("core.autocrlf", autocrlf);

                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");

                var contentStream = blob.GetContentStream(new FilteringOptions("foo.txt"));
                Assert.Equal(expectedContent.Length, contentStream.Length);

                using (var tr = new StreamReader(contentStream, Encoding.UTF8))
                {
                    string content = tr.ReadToEnd();

                    Assert.Equal(expectedContent, content);
                }
            }
        }

        [Fact]
        public void CanReadBlobFilteredStreamOfUnmodifiedBinary()
        {
            var binaryContent = new byte[] { 0, 1, 2, 3, 4, 5 };

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                using (var stream = new MemoryStream(binaryContent))
                {
                    Blob blob = repo.ObjectDatabase.CreateBlob(stream);

                    using (var filtered = blob.GetContentStream(new FilteringOptions("foo.txt")))
                    {
                        Assert.Equal(blob.Size, filtered.Length);
                        Assert.True(StreamEquals(stream, filtered));
                    }
                }
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

                repo.Stage("small.txt");
                IndexEntry entry = repo.Index["small.txt"];
                Assert.Equal("baae1fb3760a73481ced1fa03dc15614142c19ef", entry.Id.Sha);

                var blob = repo.Lookup<Blob>(entry.Id.Sha);

                using (Stream stream = blob.GetContentStream())
                using (Stream file = File.OpenWrite(Path.Combine(repo.Info.WorkingDirectory, "small.fromblob.txt")))
                {
                    CopyStream(stream, file);
                }

                repo.Stage("small.fromblob.txt");
                IndexEntry newentry = repo.Index["small.fromblob.txt"];

                Assert.Equal("baae1fb3760a73481ced1fa03dc15614142c19ef", newentry.Id.Sha);
            }
        }

        [Fact]
        public void CanTellIfTheBlobContentLooksLikeBinary()
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var blob = repo.Lookup<Blob>("a8233120f6ad708f843d861ce2b7228ec4e3dec6");
                Assert.Equal(false, blob.IsBinary);
            }
        }

        private static void SkipIfNotSupported(string autocrlf)
        {
            InconclusiveIf(() => autocrlf == "true" && Constants.IsRunningOnUnix, "Non-Windows does not support core.autocrlf = true");
        }
    }
}
