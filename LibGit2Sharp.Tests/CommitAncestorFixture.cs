using System;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

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

        [Fact]
        public void CanFindCommonAncestorForTwoCommits()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var first = repo.Lookup<Commit>("c47800c7266a2be04c571c04d5a6614691ea99bd");
                var second = repo.Lookup<Commit>("9fd738e8f7967c078dceed8190330fc8648ee56a");

                Commit ancestor = repo.Commits.FindCommonAncestor(first, second);

                Assert.NotNull(ancestor);
                Assert.Equal("5b5b025afb0b4c913b4c338a42934a3863bf3644", ancestor.Id.Sha);
            }
        }

        [Fact]
        public void CanFindCommonAncestorForTwoCommitsAsEnumerable()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var first = repo.Lookup<Commit>("c47800c7266a2be04c571c04d5a6614691ea99bd");
                var second = repo.Lookup<Commit>("9fd738e8f7967c078dceed8190330fc8648ee56a");

                Commit ancestor = repo.Commits.FindCommonAncestor(new[] { first, second });

                Assert.NotNull(ancestor);
                Assert.Equal("5b5b025afb0b4c913b4c338a42934a3863bf3644", ancestor.Id.Sha);
            }
        }

        [Fact]
        public void CanFindCommonAncestorForSeveralCommits()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var first = repo.Lookup<Commit>("4c062a6361ae6959e06292c1fa5e2822d9c96345");
                var second = repo.Lookup<Commit>("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                var third = repo.Lookup<Commit>("c47800c7266a2be04c571c04d5a6614691ea99bd");
                var fourth = repo.Lookup<Commit>("5b5b025afb0b4c913b4c338a42934a3863bf3644");

                Commit ancestor = repo.Commits.FindCommonAncestor(new[] { first, second, third, fourth });

                Assert.NotNull(ancestor);
                Assert.Equal("5b5b025afb0b4c913b4c338a42934a3863bf3644", ancestor.Id.Sha);
            }
        }

        [Fact]
        public void CannotFindAncestorForTwoCommmitsWithoutCommonAncestor()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                var first = repo.Lookup<Commit>("4c062a6361ae6959e06292c1fa5e2822d9c96345");
                var second = repo.Lookup<Commit>("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");
                var third = repo.Lookup<Commit>("c47800c7266a2be04c571c04d5a6614691ea99bd");
                var fourth = repo.Lookup<Commit>("5b5b025afb0b4c913b4c338a42934a3863bf3644");

                Commit orphanedCommit = CreateOrphanedCommit(repo);

                Commit ancestor = repo.Commits.FindCommonAncestor(new[] { first, second, orphanedCommit, third, fourth });
                Assert.Null(ancestor);
            }
        }

        [Fact]
        public void CannotFindCommonAncestorForSeveralCommmitsWithoutCommonAncestor()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                var first = repo.Lookup<Commit>("4c062a6361ae6959e06292c1fa5e2822d9c96345");

                var orphanedCommit = CreateOrphanedCommit(repo);

                Commit ancestor = repo.Commits.FindCommonAncestor(first, orphanedCommit);
                Assert.Null(ancestor);
            }
        }

        private static Commit CreateOrphanedCommit(IRepository repo)
        {
            Commit random = repo.Head.Tip;

            Commit orphanedCommit = repo.ObjectDatabase.CreateCommit(
                random.Author,
                random.Committer,
                "This is a test commit created by 'CommitFixture.CannotFindCommonAncestorForCommmitsWithoutCommonAncestor'",
                false,
                random.Tree,
                Enumerable.Empty<Commit>());

            return orphanedCommit;
        }

        [Fact]
        public void FindCommonAncestorForSingleCommitThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var first = repo.Lookup<Commit>("4c062a6361ae6959e06292c1fa5e2822d9c96345");

                Assert.Throws<ArgumentException>(() => repo.Commits.FindCommonAncestor(new[] { first }));
            }
        }

        [Fact]
        public void FindCommonAncestorForEnumerableWithNullCommitThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var first = repo.Lookup<Commit>("4c062a6361ae6959e06292c1fa5e2822d9c96345");
                var second = repo.Lookup<Commit>("be3563ae3f795b2b4353bcce3a527ad0a4f7f644");

                Assert.Throws<ArgumentException>(() => repo.Commits.FindCommonAncestor(new[] { first, second, null }));
            }
        }

        [Fact]
        public void FindCommonAncestorForWithNullCommitThrows()
        {
            using (var repo = new Repository(BareTestRepoPath))
            {
                var first = repo.Lookup<Commit>("4c062a6361ae6959e06292c1fa5e2822d9c96345");

                Assert.Throws<ArgumentNullException>(() => repo.Commits.FindCommonAncestor(first, null));
                Assert.Throws<ArgumentNullException>(() => repo.Commits.FindCommonAncestor(null, first));
            }
        }
    }
}
