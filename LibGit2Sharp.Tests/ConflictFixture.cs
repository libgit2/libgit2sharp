using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class ConflictFixture : BaseFixture
    {
        public static IEnumerable<object[]> ConflictData
        {
            get
            {
                return new List<object[]>
                {
                    new[] { "ancestor-and-ours.txt", "5dee68477001f447f50fa7ee7e6a818370b5c2fb", "dad0664ae617d36e464ec08ed969ff496432b075", null },
                    new[] { "ancestor-and-theirs.txt", "3aafd4d0bac33cc3c78c4c070f3966fb6e6f641a", null, "7b26cd5ac0ee68483ae4d5e1e00b064547ea8c9b" },
                    new[] { "ancestor-only.txt", "9736f4cd77759672322f3222ed3ddead1412d969", null, null },
                    new[] { "conflicts-one.txt", "1f85ca51b8e0aac893a621b61a9c2661d6aa6d81", "b7a41c703dc1f33185c76944177f3844ede2ee46", "516bd85f78061e09ccc714561d7b504672cb52da" },
                    new[] { "conflicts-two.txt", "84af62840be1b1c47b778a8a249f3ff45155038c", "ef70c7154145b09c7d08806e55fd0bfb7172576d", "220bd62631c8cf7a83ef39c6b94595f00517211e" },
                    new[] { "ours-and-theirs.txt", null, "9aaa9ae562a5f7362425a3fedc4d33ff74fe39e6", "0ca3f55d4ac2fa4703c149123b0b31d733112f86" },
                    new[] { "ours-only.txt", null, "9736f4cd77759672322f3222ed3ddead1412d969", null },
                    new[] { "theirs-only.txt", null, null, "9736f4cd77759672322f3222ed3ddead1412d969" },
                };
            }
        }

        private static List<object[]> RenameConflictData
        {
            get
            {
                return new List<object[]>
                {
                    new[] { "3a-renamed-in-ours-deleted-in-theirs.txt", "3a-newname-in-ours-deleted-in-theirs.txt", null },
                    new[] { "3b-renamed-in-theirs-deleted-in-ours.txt", null, "3b-newname-in-theirs-deleted-in-ours.txt" },
                    new[] { "4a-renamed-in-ours-added-in-theirs.txt", "4a-newname-in-ours-added-in-theirs.txt", null },
                    new[] { "4b-renamed-in-theirs-added-in-ours.txt", null, "4b-newname-in-theirs-added-in-ours.txt" },
                    new[] { "5a-renamed-in-ours-added-in-theirs.txt", "5a-newname-in-ours-added-in-theirs.txt", "5a-renamed-in-ours-added-in-theirs.txt" },
                    new[] { "5b-renamed-in-theirs-added-in-ours.txt", "5b-renamed-in-theirs-added-in-ours.txt", "5b-newname-in-theirs-added-in-ours.txt" },
                    new[] { "6-both-renamed-1-to-2.txt", "6-both-renamed-1-to-2-ours.txt", "6-both-renamed-1-to-2-theirs.txt" },
                    new[] { "7-both-renamed-side-1.txt", "7-both-renamed.txt", "7-both-renamed-side-1.txt" },
                    new[] { "7-both-renamed-side-2.txt", "7-both-renamed-side-2.txt", "7-both-renamed.txt" },
                };
            }
        }

        [Theory]
        [InlineData(true, "ancestor-and-ours.txt", true, false, FileStatus.DeletedFromIndex, 2)]
        [InlineData(false, "ancestor-and-ours.txt", true, true, FileStatus.DeletedFromIndex | FileStatus.NewInWorkdir, 2)]
        [InlineData(true, "ancestor-and-theirs.txt", true, false, FileStatus.Nonexistent, 2)]
        [InlineData(false, "ancestor-and-theirs.txt", true, true, FileStatus.NewInWorkdir, 2)]
        [InlineData(true, "ancestor-only.txt", false, false, FileStatus.Nonexistent, 1)]
        [InlineData(false, "ancestor-only.txt", false, false, FileStatus.Nonexistent, 1)]
        [InlineData(true, "conflicts-one.txt", true, false, FileStatus.DeletedFromIndex, 3)]
        [InlineData(false, "conflicts-one.txt", true, true, FileStatus.DeletedFromIndex | FileStatus.NewInWorkdir, 3)]
        [InlineData(true, "conflicts-two.txt", true, false, FileStatus.DeletedFromIndex, 3)]
        [InlineData(false, "conflicts-two.txt", true, true, FileStatus.DeletedFromIndex | FileStatus.NewInWorkdir, 3)]
        [InlineData(true, "ours-and-theirs.txt", true, false, FileStatus.DeletedFromIndex, 2)]
        [InlineData(false, "ours-and-theirs.txt", true, true, FileStatus.DeletedFromIndex | FileStatus.NewInWorkdir, 2)]
        [InlineData(true, "ours-only.txt", true, false, FileStatus.DeletedFromIndex, 1)]
        [InlineData(false, "ours-only.txt", true, true, FileStatus.DeletedFromIndex | FileStatus.NewInWorkdir, 1)]
        [InlineData(true, "theirs-only.txt", true, false, FileStatus.Nonexistent, 1)]
        [InlineData(false, "theirs-only.txt", true, true, FileStatus.NewInWorkdir, 1)]
        public void CanResolveConflictsByRemovingFromTheIndex(
            bool removeFromWorkdir, string filename, bool existsBeforeRemove, bool existsAfterRemove, FileStatus lastStatus, int removedIndexEntries)
        {
            var path = SandboxMergedTestRepo();
            using (var repo = new Repository(path))
            {
                int count = repo.Index.Count;

                string fullpath = Path.Combine(repo.Info.WorkingDirectory, filename);

                Assert.Equal(existsBeforeRemove, File.Exists(fullpath));
                Assert.NotNull(repo.Index.Conflicts[filename]);
                Assert.Empty(repo.Index.Conflicts.ResolvedConflicts);

                Commands.Remove(repo, filename, removeFromWorkdir);

                Assert.Null(repo.Index.Conflicts[filename]);
                Assert.Equal(count - removedIndexEntries, repo.Index.Count);
                Assert.Equal(existsAfterRemove, File.Exists(fullpath));
                Assert.Equal(lastStatus, repo.RetrieveStatus(filename));

                Assert.Single(repo.Index.Conflicts.ResolvedConflicts);
                Assert.NotNull(repo.Index.Conflicts.ResolvedConflicts[filename]);
            }
        }

        [Fact]
        public void CanGetOriginalNamesOfRenameConflicts()
        {
            var path = Sandbox(MergeRenamesTestRepoWorkingDirPath);
            using (var repo = new Repository(path))
            {
                var expected = RenameConflictData;
                var actual = repo.Index.Conflicts.Names;

                Assert.Equal(expected.Count, actual.Count());

                int i = 0;
                foreach (var name in actual)
                {
                    Assert.Equal(expected[i][0], name.Ancestor);
                    Assert.Equal(expected[i][1], name.Ours);
                    Assert.Equal(expected[i][2], name.Theirs);

                    i++;
                }
            }
        }

        [Theory, MemberData(nameof(ConflictData))]
        public void CanRetrieveSingleConflictByPath(string filepath, string ancestorId, string ourId, string theirId)
        {
            var path = SandboxMergedTestRepo();
            using (var repo = new Repository(path))
            {
                Conflict conflict = repo.Index.Conflicts[filepath];
                Assert.NotNull(conflict);

                ObjectId expectedAncestor = ancestorId != null ? new ObjectId(ancestorId) : null;
                ObjectId expectedOurs = ourId != null ? new ObjectId(ourId) : null;
                ObjectId expectedTheirs = theirId != null ? new ObjectId(theirId) : null;

                Assert.Null(repo.Index[filepath]);
                Assert.Equal(expectedAncestor, conflict.Ancestor != null ? conflict.Ancestor.Id : null);
                Assert.Equal(expectedOurs, conflict.Ours != null ? conflict.Ours.Id : null);
                Assert.Equal(expectedTheirs, conflict.Theirs != null ? conflict.Theirs.Id : null);
            }
        }

        private string GetPath(Conflict conflict)
        {
            if (conflict.Ancestor != null)
            {
                return conflict.Ancestor.Path;
            }
            if (conflict.Ours != null)
            {
                return conflict.Ours.Path;
            }
            if (conflict.Theirs != null)
            {
                return conflict.Theirs.Path;
            }

            return null;
        }

        private string GetId(IndexEntry e)
        {
            if (e == null)
            {
                return null;
            }

            return e.Id.ToString();
        }

        [Fact]
        public void CanRetrieveAllConflicts()
        {
            var path = SandboxMergedTestRepo();
            using (var repo = new Repository(path))
            {
                var expected = repo.Index.Conflicts.Select(c => new[] { GetPath(c), GetId(c.Ancestor), GetId(c.Ours), GetId(c.Theirs) }).ToArray();
                Assert.Equal(expected, ConflictData);
            }
        }
    }
}
