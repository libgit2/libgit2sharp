using System.Collections.Generic;
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
                return new[]
                {
                    new string[] { "ancestor-and-ours.txt", "5dee68477001f447f50fa7ee7e6a818370b5c2fb", "dad0664ae617d36e464ec08ed969ff496432b075", null },
                    new string[] { "ancestor-and-theirs.txt", "3aafd4d0bac33cc3c78c4c070f3966fb6e6f641a", null, "7b26cd5ac0ee68483ae4d5e1e00b064547ea8c9b" },
                    new string[] { "ancestor-only.txt", "9736f4cd77759672322f3222ed3ddead1412d969", null, null },
                    new string[] { "conflicts-one.txt", "1f85ca51b8e0aac893a621b61a9c2661d6aa6d81", "b7a41c703dc1f33185c76944177f3844ede2ee46", "516bd85f78061e09ccc714561d7b504672cb52da" },
                    new string[] { "conflicts-two.txt", "84af62840be1b1c47b778a8a249f3ff45155038c", "ef70c7154145b09c7d08806e55fd0bfb7172576d", "220bd62631c8cf7a83ef39c6b94595f00517211e" },
                    new string[] { "ours-and-theirs.txt", null, "9aaa9ae562a5f7362425a3fedc4d33ff74fe39e6", "0ca3f55d4ac2fa4703c149123b0b31d733112f86" },
                    new string[] { "ours-only.txt", null, "9736f4cd77759672322f3222ed3ddead1412d969", null },
                    new string[] { "theirs-only.txt", null, null, "9736f4cd77759672322f3222ed3ddead1412d969" },
                };
            }
        }

        [Theory, PropertyData("ConflictData")]
        public void CanRetrieveSingleConflictByPath(string filepath, string ancestorId, string ourId, string theirId)
        {
            using (var repo = new Repository(MergedTestRepoWorkingDirPath))
            {
                Conflict conflict = repo.Conflicts[filepath];
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
            using (var repo = new Repository(MergedTestRepoWorkingDirPath))
            {
                var expected = repo.Conflicts.Select(c => new string[] { GetPath(c), GetId(c.Ancestor), GetId(c.Ours), GetId(c.Theirs) }).ToArray();
                Assert.Equal(expected, ConflictData);
            }
        }
    }
}
