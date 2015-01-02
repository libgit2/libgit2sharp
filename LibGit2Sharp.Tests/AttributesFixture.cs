using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class AttributesFixture : BaseFixture
    {
        [Fact]
        public void StagingHonorsTheAttributesFiles()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                CreateAttributesFile(repo);

                AssertNormalization(repo, "text.txt", true, "22c74203bace3c2e950278c7ab08da0fca9f4e9b");
                AssertNormalization(repo, "huh.dunno", true, "22c74203bace3c2e950278c7ab08da0fca9f4e9b");
                AssertNormalization(repo, "binary.data", false, "66eeff1fcbacf589e6d70aa70edd3fce5be2b37c");
            }
        }

        private static void AssertNormalization(IRepository repo, string filename, bool shouldHaveBeenNormalized, string expectedSha)
        {
            var sb = new StringBuilder();
            sb.Append("I'm going to be dynamically processed\r\n");
            sb.Append("And my line endings...\r\n");
            sb.Append("...are going to be\n");
            sb.Append("normalized!\r\n");

            Touch(repo.Info.WorkingDirectory, filename, sb.ToString());

            repo.Stage(filename);

            IndexEntry entry = repo.Index[filename];
            Assert.NotNull(entry);

            Assert.Equal(expectedSha, entry.Id.Sha);

            var blob = repo.Lookup<Blob>(entry.Id);
            Assert.NotNull(blob);

            Assert.Equal(!shouldHaveBeenNormalized, blob.GetContentText().Contains("\r"));
        }

        private static void CreateAttributesFile(IRepository repo)
        {
            var sb = new StringBuilder();
            sb.Append("* text=auto\n");
            sb.Append("*.txt text\n");
            sb.Append("*.data binary\n");

            Touch(repo.Info.WorkingDirectory, ".gitattributes", sb.ToString());
        }
    }
}
