using System.IO;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class ArchiveTarFixture : BaseFixture
    {
        [Fact]
        public void CanArchiveACommitWithDirectoryAsTar()
        {
            var path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                // This tests generates an archive of the bare test repo, and compares it with
                // a pre-generated tar file. The expected tar file has been generated with the
                // crlf filter active (on windows), so we need to make sure that even if the test
                // is launched on linux then the files content has the crlf filter applied (not
                // active by default).
                var sb = new StringBuilder();
                sb.Append("* text eol=crlf\n");
                Touch(Path.Combine(repo.Info.Path, "info"), "attributes", sb.ToString());

                var commit = repo.Lookup<Commit>("4c062a6361ae6959e06292c1fa5e2822d9c96345");

                var scd = BuildSelfCleaningDirectory();
                var archivePath = Path.Combine(scd.RootedDirectoryPath, Path.GetRandomFileName() + ".tar");
                Directory.CreateDirectory(scd.RootedDirectoryPath);

                repo.ObjectDatabase.Archive(commit, archivePath);

                using (var expectedStream = new StreamReader(Path.Combine(ResourcesDirectory.FullName, "expected_archives/commit_with_directory.tar")))
                using (var actualStream = new StreamReader(archivePath))
                {
                    string expected = expectedStream.ReadToEnd();
                    string actual = actualStream.ReadToEnd();

                    Assert.Equal(expected, actual);
                }
            }
        }
    }
}
