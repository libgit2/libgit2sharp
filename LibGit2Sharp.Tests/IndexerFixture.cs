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
                var path = Path.Combine(repo.Info.Path, "objects/pack/pack-a81e489679b7d3418f9ab594bda8ceb37dd4c695.pack");
                using (var indexer = new Indexer(repo.Info.WorkingDirectory, 438 /* 0666 */,
                    onProgress: (p) => {
                        copiedProgress = p;
                        return true;
                    }))
                {
                    indexer.Index(path);
                    Assert.Equal(1628, indexer.Progress.TotalObjects);
                    Assert.Equal(indexer.Progress.TotalObjects, copiedProgress.TotalObjects);
                }
            }
        }

        [Fact]
        public void CanIndexFromAStream()
        {
            using (var repo = new Repository(SandboxStandardTestRepoGitDir()))
            {
                var path = Path.Combine(repo.Info.Path, "objects/pack/pack-a81e489679b7d3418f9ab594bda8ceb37dd4c695.pack");
                using (var indexer = new Indexer(repo.Info.WorkingDirectory, 438 /* 0666 */))
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    indexer.Index(stream);
                    Assert.Equal(1628, indexer.Progress.TotalObjects);
                }
            }
        }

    }
}

