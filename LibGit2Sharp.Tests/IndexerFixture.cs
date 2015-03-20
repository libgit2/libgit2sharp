using System;
using System.IO;
using LibGit2Sharp.Advanced;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class IndexerFixture : BaseFixture
    {
        [Fact]
        public void CanIndexFromAFilePath()
        {
            TransferProgress copiedProgress = default(TransferProgress);
            using (var repo = new Repository(SandboxStandardTestRepoGitDir()))
            {
                var expectedId = new ObjectId("a81e489679b7d3418f9ab594bda8ceb37dd4c695");
                var path = Path.Combine(repo.Info.Path, "objects/pack/pack-a81e489679b7d3418f9ab594bda8ceb37dd4c695.pack");
                TransferProgress progress;
                var packId = Indexer.Index(out progress, path, repo.Info.WorkingDirectory, 438 /* 0666 */,
                    onProgress: (p) => {
                        copiedProgress = p;
                        return true;
                    });

                Assert.Equal(expectedId, packId);
                Assert.Equal(1628, progress.TotalObjects);
                Assert.Equal(1628, copiedProgress.TotalObjects);
            }
        }

        [Fact]
        public void CanIndexFromAStream()
        {
            using (var repo = new Repository(SandboxStandardTestRepoGitDir()))
            {
                var expectedId = new ObjectId("a81e489679b7d3418f9ab594bda8ceb37dd4c695");
                var path = Path.Combine(repo.Info.Path, "objects/pack/pack-a81e489679b7d3418f9ab594bda8ceb37dd4c695.pack");
                TransferProgress progress;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var packId = Indexer.Index(out progress, stream, repo.Info.WorkingDirectory, 438 /* 0666 */);
                    Assert.Equal(expectedId, packId);
                    Assert.Equal(1628, progress.TotalObjects);
                }
            }
        }

    }
}

