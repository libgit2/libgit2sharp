using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using System.Collections.Generic;
using LibGit2Sharp.Advanced;

namespace LibGit2Sharp.Tests
{
    public class PackBuilderFixture : BaseFixture
    {
        [Fact]
        public void TestDefaultPackAllWithDirPath()
        {
            TestBody((repo, packDirPath) =>
            {
                PackBuilderResults results = repo.ObjectDatabase.PackAll(packDirPath);
            });
        }

        [Fact]
        public void TestCommitsPerBranch()
        {
            TestBody((repo, packDirPath) =>
            {
                using (PackBuilder builder = new PackBuilder(repo))
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

                    builder.WritePackTo(packDirPath);
                }
            });
        }

        [Fact]
        public void TestCommitsPerBranchIds()
        {
            TestBody((repo, packDirPath) =>
            {
                using (PackBuilder builder = new PackBuilder(repo))
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

                    builder.WritePackTo(packDirPath);
                }
            });
        }

        [Fact]
        public void TestCreatingMultiplePackFilesByType()
        {
            TestBody((repo, packDirPath) =>
            {
                long totalNumberOfWrittenObjects = 0;

                using (PackBuilder builder = new PackBuilder(repo))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        foreach (GitObject obj in repo.ObjectDatabase)
                        {
                            if (i == 0 && obj is Commit)
                                builder.Add(obj.Id);

                            if (i == 1 && obj is Tree)
                                builder.Add(obj.Id);

                            if (i == 2 && obj is Blob)
                                builder.Add(obj.Id);
                        }

                        PackBuilderResults results = builder.WritePackTo(packDirPath);

                        // for reuse to build the next pack file.
                        builder.Reset();

                        // assert the pack file is written 
                        Assert.True(File.Exists(Path.Combine(packDirPath, "pack-" + results.PackHash + ".pack")));
                        Assert.True(File.Exists(Path.Combine(packDirPath, "pack-" + results.PackHash + ".idx")));

                        totalNumberOfWrittenObjects += results.WrittenObjectsCount;
                    }
                }

                // assert total number of written objects count is the same as in objects database
                Assert.Equal(repo.ObjectDatabase.Count(), totalNumberOfWrittenObjects);
            });
        }

        [Fact]
        public void TestCreatingMultiplePackFilesByCount()
        {
            TestBody((repo, packDirPath) =>
            {
                long totalNumberOfWrittenObjects = 0;
                PackBuilderResults results;

                using (PackBuilder packBuilder = new PackBuilder(repo))
                {
                    int currentObject = 0;

                    foreach (GitObject gitObject in repo.ObjectDatabase)
                    {
                        packBuilder.Add(gitObject.Id);

                        if (currentObject++ % 100 == 0)
                        {
                            results = packBuilder.WritePackTo(packDirPath);
                            packBuilder.Reset();

                            // assert the pack file is written 
                            Assert.True(File.Exists(Path.Combine(packDirPath, "pack-" + results.PackHash + ".pack")));
                            Assert.True(File.Exists(Path.Combine(packDirPath, "pack-" + results.PackHash + ".idx")));

                            totalNumberOfWrittenObjects += results.WrittenObjectsCount;
                        }
                    }

                    if (currentObject % 100 != 1)
                    {
                        results = packBuilder.WritePackTo(packDirPath);
                        packBuilder.Reset();

                        // assert the pack file is written 
                        Assert.True(File.Exists(Path.Combine(packDirPath, "pack-" + results.PackHash + ".pack")));
                        Assert.True(File.Exists(Path.Combine(packDirPath, "pack-" + results.PackHash + ".idx")));

                        totalNumberOfWrittenObjects += results.WrittenObjectsCount;
                    }
                }

                // assert total number of written objects count is the same as in objects database
                Assert.Equal(repo.ObjectDatabase.Count(), totalNumberOfWrittenObjects);
            });
        }

        [Fact]
        public void CanWritePackAndIndexFiles()
        {
            using (Repository repo = new Repository(SandboxPackBuilderTestRepo()))
            {
                string path = Path.GetTempPath();

                PackBuilderResults results = repo.ObjectDatabase.PackAll(path);

                Assert.Equal(repo.ObjectDatabase.Count(), results.WrittenObjectsCount);
                Assert.True(File.Exists(Path.Combine(path, "pack-" + results.PackHash + ".pack")));
                Assert.True(File.Exists(Path.Combine(path, "pack-" + results.PackHash + ".idx")));
            }
        }

        [Fact]
        public void TestEmptyPackFile()
        {
            using (Repository repo = new Repository(SandboxPackBuilderTestRepo()))
            {
                string path = Path.GetTempPath();

                using (PackBuilder builder = new PackBuilder(repo))
                {
                    PackBuilderResults results = builder.WritePackTo(path);

                    Assert.True(File.Exists(Path.Combine(path, "pack-" + results.PackHash + ".pack")));
                    Assert.True(File.Exists(Path.Combine(path, "pack-" + results.PackHash + ".idx")));
                }
            }
        }

        [Fact]
        public void TestPackFileForEmptyRepository()
        {
            using (Repository repo = new Repository(InitNewRepository()))
            {
                string path = Path.GetTempPath();

                PackBuilderResults results = repo.ObjectDatabase.PackAll(path);

                Assert.Equal(repo.ObjectDatabase.Count(), results.WrittenObjectsCount);
                Assert.True(File.Exists(Path.Combine(path, "pack-" + results.PackHash + ".pack")));
                Assert.True(File.Exists(Path.Combine(path, "pack-" + results.PackHash + ".idx")));
            }
        }

        [Fact]
        public void ExceptionIfPathDoesNotExistAtPackAll()
        {
            using (Repository repo = new Repository(SandboxPackBuilderTestRepo()))
            {
                Assert.Throws<DirectoryNotFoundException>(() => repo.ObjectDatabase.PackAll("aaaaa"));
            }
        }

        [Fact]
        public void ExceptionIfPathDoesNotExistAtWriteToPack()
        {
            using (Repository repo = new Repository(SandboxPackBuilderTestRepo()))
            using (PackBuilder builder = new PackBuilder(repo))
            {
                Assert.Throws<DirectoryNotFoundException>(() => builder.WritePackTo("aaaaa"));
            }
        }

        [Fact]
        public void ExceptionIfAddNullObjectID()
        {
            using (Repository repo = new Repository(SandboxPackBuilderTestRepo()))
            using (PackBuilder builder = new PackBuilder(repo))
            {
                Assert.Throws<ArgumentNullException>(() => builder.Add(null));
            }
        }

        [Fact]
        public void ExceptionIfAddRecursivelyNullObjectID()
        {
            using (Repository repo = new Repository(SandboxPackBuilderTestRepo()))
            using (PackBuilder builder = new PackBuilder(repo))
            {
                Assert.Throws<ArgumentNullException>(() => builder.AddRecursively(null));
            }
        }

        internal void TestBody(Action<Repository, string> fullPackingAction)
        {
            // read a repo, pack with the provided action, write the pack file in a mirror repo, read new repo, compare

            string orgRepoPath = SandboxPackBuilderTestRepo();
            string mrrRepoPath = SandboxPackBuilderTestRepo();
            string mrrRepoPackDirPath = Path.Combine(mrrRepoPath + "/.git/objects");

            DirectoryHelper.DeleteDirectory(mrrRepoPackDirPath);
            Directory.CreateDirectory(mrrRepoPackDirPath + "/pack");

            using (Repository orgRepo = new Repository(orgRepoPath))
            {
                fullPackingAction(orgRepo, mrrRepoPackDirPath + "/pack");

                // loading the mirror repo from the written pack file and make sure it's identical to the original.
                using (Repository mrrRepo = new Repository(mrrRepoPath))
                {
                    AssertIfNotIdenticalRepositories(orgRepo, mrrRepo);
                }
            }
        }

        internal void AssertIfNotIdenticalRepositories(IRepository repo1, IRepository repo2)
        {
            // make sure the objects of the original repo are the same as the ones in the mirror repo
            // doing that by making sure the count is the same, and the set difference is empty
            Assert.True(repo1.ObjectDatabase.Count() == repo2.ObjectDatabase.Count()
                && !repo2.ObjectDatabase.Except(repo1.ObjectDatabase).Any());

            Assert.Equal(repo1.Commits.Count(), repo2.Commits.Count());
            Assert.Equal(repo1.Branches.Count(), repo2.Branches.Count());
            Assert.Equal(repo1.Refs.Count(), repo2.Refs.Count());
            Assert.Equal(repo1.Tags.Count(), repo2.Tags.Count());
        }
    }
}
