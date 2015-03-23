using System;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class CommitAncestorFixture : BaseFixture
    {
        /*
         * BareTestRepoPath structure
         *
         *  * commit 4c062a6361ae6959e06292c1fa5e2822d9c96345
         *  |
         *  *   commit be3563ae3f795b2b4353bcce3a527ad0a4f7f644
         *  |\
         *  | |
         *  | * commit c47800c7266a2be04c571c04d5a6614691ea99bd
         *  | |
         *  * | commit 9fd738e8f7967c078dceed8190330fc8648ee56a
         *  | |
         *  * | commit 4a202b346bb0fb0db7eff3cffeb3c70babbd2045
         *  |/
         *  |
         *  * commit 5b5b025afb0b4c913b4c338a42934a3863bf3644
         *  |
         *  * commit 8496071c1b46c854b31185ea97743be6a877447
         *
        */

        [Theory]
        [InlineData("5b5b025afb0b4c913b4c338a42934a3863bf3644", "c47800c", "9fd738e")]
        [InlineData("9fd738e8f7967c078dceed8190330fc8648ee56a", "be3563a", "9fd738e")]
        [InlineData(null, "be3563a", "-")]
        public void FindCommonAncestorForTwoCommits(string result, string sha1, string sha2)
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var first = sha1 == "-" ? CreateOrphanedCommit(repo) : repo.Lookup<Commit>(sha1);
                var second = sha2 == "-" ? CreateOrphanedCommit(repo) : repo.Lookup<Commit>(sha2);

                Commit ancestor = repo.ObjectDatabase.FindMergeBase(first, second);

                if (result == null)
                {
                    Assert.Null(ancestor);
                }
                else
                {
                    Assert.NotNull(ancestor);
                    Assert.Equal(result, ancestor.Id.Sha);
                }
            }
        }

        [Theory]
        [InlineData("5b5b025afb0b4c913b4c338a42934a3863bf3644", new[] { "c47800c", "9fd738e" }, MergeBaseFindingStrategy.Octopus)]
        [InlineData("5b5b025afb0b4c913b4c338a42934a3863bf3644", new[] { "c47800c", "9fd738e" }, MergeBaseFindingStrategy.Standard)]
        [InlineData("5b5b025afb0b4c913b4c338a42934a3863bf3644", new[] { "4c062a6", "be3563a", "c47800c", "5b5b025" }, MergeBaseFindingStrategy.Octopus)]
        [InlineData("be3563ae3f795b2b4353bcce3a527ad0a4f7f644", new[] { "4c062a6", "be3563a", "c47800c", "5b5b025" }, MergeBaseFindingStrategy.Standard)]
        [InlineData(null, new[] { "4c062a6", "be3563a", "-", "c47800c", "5b5b025" }, MergeBaseFindingStrategy.Octopus)]
        [InlineData("be3563ae3f795b2b4353bcce3a527ad0a4f7f644", new[] { "4c062a6", "be3563a", "-", "c47800c", "5b5b025" }, MergeBaseFindingStrategy.Standard)]
        [InlineData(null, new[] { "4c062a6", "-" }, MergeBaseFindingStrategy.Octopus)]
        [InlineData(null, new[] { "4c062a6", "-" }, MergeBaseFindingStrategy.Standard)]
        public void FindCommonAncestorForCommitsAsEnumerable(string result, string[] shas, MergeBaseFindingStrategy strategy)
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var commits = shas.Select(sha => sha == "-" ? CreateOrphanedCommit(repo) : repo.Lookup<Commit>(sha)).ToArray();

                Commit ancestor = repo.ObjectDatabase.FindMergeBase(commits, strategy);

                if (result == null)
                {
                    Assert.Null(ancestor);
                }
                else
                {
                    Assert.NotNull(ancestor);
                    Assert.Equal(result, ancestor.Id.Sha);
                }
            }
        }

        [Theory]
        [InlineData("4c062a6", "0000000")]
        [InlineData("0000000", "4c062a6")]
        public void FindCommonAncestorForTwoCommitsThrows(string sha1, string sha2)
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var first = repo.Lookup<Commit>(sha1);
                var second = repo.Lookup<Commit>(sha2);

                Assert.Throws<ArgumentNullException>(() => repo.ObjectDatabase.FindMergeBase(first, second));
            }
        }

        [Theory]
        [InlineData(new[] { "4c062a6" }, MergeBaseFindingStrategy.Octopus)]
        [InlineData(new[] { "4c062a6" }, MergeBaseFindingStrategy.Standard)]
        [InlineData(new[] { "4c062a6", "be3563a", "000000" }, MergeBaseFindingStrategy.Octopus)]
        [InlineData(new[] { "4c062a6", "be3563a", "000000" }, MergeBaseFindingStrategy.Standard)]
        public void FindCommonAncestorForCommitsAsEnumerableThrows(string[] shas, MergeBaseFindingStrategy strategy)
        {
            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path))
            {
                var commits = shas.Select(sha => sha == "-" ? CreateOrphanedCommit(repo) : repo.Lookup<Commit>(sha)).ToArray();

                Assert.Throws<ArgumentException>(() => repo.ObjectDatabase.FindMergeBase(commits, strategy));
            }
        }

        private static Commit CreateOrphanedCommit(IRepository repo)
        {
            Commit random = repo.Head.Tip;

            Commit orphanedCommit = repo.ObjectDatabase.CreateCommit(
                random.Author,
                random.Committer,
                "This is a test commit created by 'CommitFixture.CannotFindCommonAncestorForCommmitsWithoutCommonAncestor'",
                random.Tree,
                Enumerable.Empty<Commit>(),
                false);

            return orphanedCommit;
        }
    }
}
