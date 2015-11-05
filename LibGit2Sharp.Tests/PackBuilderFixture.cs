using System;
using System.IO;
using System.Linq;
using System.Text;
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
            string mrrRepoPackDirPath = Path.Combine(mrrRepoPath + ".git", "objects");

            DirectoryHelper.DeleteDirectory(mrrRepoPackDirPath);
            string packDir = Path.Combine(mrrRepoPackDirPath, "pack");
            Directory.CreateDirectory(packDir);

            using (Repository orgRepo = new Repository(orgRepoPath))
            {
                var packBuilderOptions = new PackBuilderOptions();

                PackBuilderResults results;
                if (packDelegate != null)
                {
                    packBuilderOptions.PackDelegate = b => packDelegate(orgRepo, b);
                }

                results = orgRepo.ObjectDatabase.Pack(packDir, packBuilderOptions);

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
            using (Repository repo = new Repository(SandboxPackBuilderTestRepo()))
            {
                Assert.Throws<DirectoryNotFoundException>(() =>
                {
                    repo.ObjectDatabase.Pack("aaa", new PackBuilderOptions());
                });
            }
        }

        [Fact]
        public void ExceptionIfPathEqualsNull()
        {
            using (Repository repo = new Repository(SandboxPackBuilderTestRepo()))
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    repo.ObjectDatabase.Pack(default(string), new PackBuilderOptions());
                });
            }
        }

        [Fact]
        public void ExceptionIfStreamIsNull()
        {

            using (Repository repo = new Repository(SandboxPackBuilderTestRepo()))
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    repo.ObjectDatabase.Pack(default(Stream), new PackBuilderOptions());
                });
            }
        }

        [Fact]
        public void ExceptionIfOptionsEqualsNull()
        {
            string orgRepoPath = SandboxPackBuilderTestRepo();

            using (Repository orgRepo = new Repository(orgRepoPath))
            {
                string packDir = Path.Combine(orgRepoPath, "objects", "pack");
                Directory.CreateDirectory(packDir);

                Assert.Throws<ArgumentNullException>(() =>
                {
                    orgRepo.ObjectDatabase.Pack(packDirectory: packDir, options: null);
                });
            }
        }

        [Fact]
        public void ExceptionIfNegativeNumberOfThreads()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                (new PackBuilderOptions()).MaximumNumberOfThreads = -1;
            });
        }

        [Fact]
        public void ExceptionIfAddNullObjectID()
        {
            string orgRepoPath = SandboxPackBuilderTestRepo();
            var packBuilderOptions = new PackBuilderOptions();

            using (Repository orgRepo = new Repository(orgRepoPath))
            {
                packBuilderOptions.PackDelegate = builder => builder.Add(null);

                using (var ms = new MemoryStream())
                {
                    Assert.Throws<ArgumentNullException>(() =>
                    {
                        orgRepo.ObjectDatabase.Pack(ms, packBuilderOptions);
                    });
                }
            }
        }

        [Fact]
        public void ExceptionIfAddRecursivelyNullObjectID()
        {
            string orgRepoPath = SandboxPackBuilderTestRepo();
            var packBuilderOptions = new PackBuilderOptions();

            using (Repository orgRepo = new Repository(orgRepoPath))
            {
                packBuilderOptions.PackDelegate = builder => builder.AddRecursively(null);

                using (var ms = new MemoryStream())
                {
                    Assert.Throws<ArgumentNullException>(() =>
                    {
                        orgRepo.ObjectDatabase.Pack(ms, packBuilderOptions);
                    });
                }
            }
        }

        [Fact]
        public void WriteToStreamWritesAllObjects()
        {
            var testRepoPath = SandboxPackBuilderTestRepo();
            using (var testRepo = new Repository(testRepoPath))
            {
                using (var packOutput = new MemoryStream())
                {
                    PackBuilderResults results = testRepo.ObjectDatabase.Pack(packOutput, new PackBuilderOptions());

                    const string packHeader = "PACK";
                    Assert.Equal(
                        packHeader,
                        Encoding.UTF8.GetString(packOutput.GetBuffer(), 0, packHeader.Length));

                    Assert.Equal(testRepo.ObjectDatabase.Count(), results.WrittenObjectsCount);
                }
            }
        }
    }
}
