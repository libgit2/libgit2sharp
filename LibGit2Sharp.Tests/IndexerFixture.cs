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
        [Trait("run", "me")]
        public void CanIndexFromAFilePath()
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                var path = Path.Combine(repo.Info.Path, "objects/pack/pack-a81e489679b7d3418f9ab594bda8ceb37dd4c695.pack");
                var indexer = new Indexer(repo.Info.WorkingDirectory);
                indexer.Index(path);
                Assert.Equal(1628, indexer.Progress.TotalObjects);
            }
        }

        [Fact]
        [Trait("run", "me")]
        public void CanIndexFromAStream()
        {
            using (var repo = new Repository(CloneStandardTestRepo()))
            {
                var path = Path.Combine(repo.Info.Path, "objects/pack/pack-a81e489679b7d3418f9ab594bda8ceb37dd4c695.pack");
                var indexer = new Indexer(repo.Info.WorkingDirectory);
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    indexer.Index(stream);
                    Assert.Equal(1628, indexer.Progress.TotalObjects);
                }
            }
        }

    }
}

