using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class PackBuilderFixture : BaseFixture
    {
        [Fact]
        public void TestDefaultPackDelegate()
        {
            TestIfSameRepoAfterPacking(null);
        }

        [Fact]
        public void TestCommitsPerBranchPackDelegate()
        {
            TestIfSameRepoAfterPacking(AddingObjectIdsTestDelegate);
        }

        [Fact]
        public void TestCommitsPerBranchIdsPackDelegate()
        {
            TestIfSameRepoAfterPacking(AddingObjectsTestDelegate);
        }

        internal void TestIfSameRepoAfterPacking(Action<IRepository, PackBuilder> packDelegate)
        {
            // read a repo
            // pack with the provided action
            // write the pack file in a mirror repo
            // read new repo
            // compare

            string orgRepoPath = SandboxPackBuilderTestRepo();
            string mrrRepoPath = SandboxPackBuilderTestRepo();
            string mrrRepoPackDirPath = Path.Combine(mrrRepoPath + "/.git/objects");

            DirectoryHelper.DeleteDirectory(mrrRepoPackDirPath);
            Directory.CreateDirectory(mrrRepoPackDirPath + "/pack");

            PackBuilderOptions packBuilderOptions = new PackBuilderOptions(mrrRepoPackDirPath + "/pack");

            using (Repository orgRepo = new Repository(orgRepoPath))
            {
                PackBuilderResults results;
                if (packDelegate != null)
                    results = orgRepo.ObjectDatabase.Pack(packBuilderOptions, b => packDelegate(orgRepo, b));
                else
                    results = orgRepo.ObjectDatabase.Pack(packBuilderOptions);

                // written objects count is the same as in objects database
                Assert.Equal(orgRepo.ObjectDatabase.Count(), results.WrittenObjectsCount);

                // loading a repo from the written pack file.
                using (Repository mrrRepo = new Repository(mrrRepoPath))
                {
                    // make sure the objects of the original repo are the same as the ones in the mirror repo
                    // doing that by making sure the count is the same, and the set difference is empty
                    Assert.True(mrrRepo.ObjectDatabase.Count() == orgRepo.ObjectDatabase.Count() && !mrrRepo.ObjectDatabase.Except(orgRepo.ObjectDatabase).Any());

                    Assert.Equal(orgRepo.Commits.Count(), mrrRepo.Commits.Count());
                    Assert.Equal(orgRepo.Branches.Count(), mrrRepo.Branches.Count());
                    Assert.Equal(orgRepo.Refs.Count(), mrrRepo.Refs.Count());
                }
            }
        }

        internal void AddingObjectIdsTestDelegate(IRepository repo, PackBuilder builder)
        {
            foreach (Branch branch in repo.Branches)
            {
                foreach (Commit commit in branch.Commits)
                {
                    builder.AddRecursively(commit.Id);
                }
            }

            foreach (Tag tag in repo.Tags)
            {
                builder.Add(tag.Target.Id);
            }
        }

        internal void AddingObjectsTestDelegate(IRepository repo, PackBuilder builder)
        {
            foreach (Branch branch in repo.Branches)
            {
                foreach (Commit commit in branch.Commits)
                {
                    builder.AddRecursively(commit);
                }
            }

            foreach (Tag tag in repo.Tags)
            {
                builder.Add(tag.Target);
            }
        }

        [Fact]
        public void ExceptionIfPathDoesNotExist()
        {
            Assert.Throws<DirectoryNotFoundException>(() => new PackBuilderOptions("aaa"));
        }

        [Fact]
        public void ExceptionIfPathEqualsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new PackBuilderOptions(null));
        }

        [Fact]
        public void ExceptionIfOptionsEqualsNull()
        {
            string orgRepoPath = SandboxPackBuilderTestRepo();

            using (Repository orgRepo = new Repository(orgRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    orgRepo.ObjectDatabase.Pack(null);
                });
            }
        }

        [Fact]
        public void ExceptionIfBuildDelegateEqualsNull()
        {
            string orgRepoPath = SandboxPackBuilderTestRepo();
            PackBuilderOptions packBuilderOptions = new PackBuilderOptions(orgRepoPath);

            using (Repository orgRepo = new Repository(orgRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    orgRepo.ObjectDatabase.Pack(packBuilderOptions, null);
                });
            }
        }

        [Fact]
        public void ExceptionIfNegativeNumberOfThreads()
        {
            string orgRepoPath = SandboxPackBuilderTestRepo();
            PackBuilderOptions packBuilderOptions = new PackBuilderOptions(orgRepoPath);

            Assert.Throws<ArgumentException>(() =>
            {
                packBuilderOptions.MaximumNumberOfThreads = -1;
            });
        }

        [Fact]
        public void ExceptionIfAddNullObjectID()
        {
            string orgRepoPath = SandboxPackBuilderTestRepo();
            PackBuilderOptions packBuilderOptions = new PackBuilderOptions(orgRepoPath);

            using (Repository orgRepo = new Repository(orgRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    orgRepo.ObjectDatabase.Pack(packBuilderOptions, builder =>
                    {
                        builder.Add(null);
                    });
                });
            }
        }

        [Fact]
        public void ExceptionIfAddRecursivelyNullObjectID()
        {
            string orgRepoPath = SandboxPackBuilderTestRepo();
            PackBuilderOptions packBuilderOptions = new PackBuilderOptions(orgRepoPath);

            using (Repository orgRepo = new Repository(orgRepoPath))
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    orgRepo.ObjectDatabase.Pack(packBuilderOptions, builder =>
                    {
                        builder.AddRecursively(null);
                    });
                });
            }
        }
    }
}
