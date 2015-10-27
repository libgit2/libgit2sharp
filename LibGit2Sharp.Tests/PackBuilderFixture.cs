using System;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using System.Collections.Generic;

namespace LibGit2Sharp.Tests
{
    public class PackBuilderFixture : BaseFixture
    {
        [Fact]
        public void TestDefaultPackDelegate()
        {
            TestBody((repo, options) =>
            {
                PackBuilderResults results = repo.ObjectDatabase.Pack(options);
            });
        }

        [Fact]
        public void TestCommitsPerBranchPackDelegate()
        {
            TestBody((repo, options) =>
            {
                PackBuilderResults results = repo.ObjectDatabase.Pack(options, builder =>
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
                });
            });
        }

        [Fact]
        public void TestCommitsPerBranchIdsPackDelegate()
        {
            TestBody((repo, options) =>
            {
                PackBuilderResults results = repo.ObjectDatabase.Pack(options, builder =>
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
                });
            });
        }

        [Fact]
        public void TestCreatingMultiplePackFilesByType()
        {
            TestBody((repo, options) =>
            {
                long totalNumberOfWrittenObjects = 0;
                PackBuilderResults results;

                for (int i = 0; i < 3; i++)
                {
                    results = repo.ObjectDatabase.Pack(options, b =>
                    {
                        foreach (GitObject obj in repo.ObjectDatabase)
                        {
                            if (i == 0 && obj is Commit)
                                b.Add(obj.Id);
                            if (i == 1 && obj is Tree)
                                b.Add(obj.Id);
                            if (i == 2 && obj is Blob)
                                b.Add(obj.Id);
                        }
                    });

                    // assert the pack file is written 
                    Assert.True(File.Exists(Path.Combine(options.PackDirectoryPath, "pack-" + results.PackHash + ".pack")));
                    Assert.True(File.Exists(Path.Combine(options.PackDirectoryPath, "pack-" + results.PackHash + ".idx")));

                    totalNumberOfWrittenObjects += results.WrittenObjectsCount;
                }

                // assert total number of written objects count is the same as in objects database
                Assert.Equal(repo.ObjectDatabase.Count(), totalNumberOfWrittenObjects);
            });
        }

        [Fact]
        public void TestCreatingMultiplePackFilesByCount()
        {
            TestBody((repo, options) =>
            {
                long totalNumberOfWrittenObjects = 0;
                PackBuilderResults results;

                List<GitObject> objectsList = repo.ObjectDatabase.ToList();
                int totalObjectCount = objectsList.Count;

                int currentObject = 0;

                while (currentObject < totalObjectCount)
                {
                    results = repo.ObjectDatabase.Pack(options, b =>
                    {
                        while (currentObject < totalObjectCount)
                        {
                            b.Add(objectsList[currentObject]);

                            if (currentObject++ % 100 == 0)
                                break;
                        }
                    });

                    // assert the pack file is written 
                    Assert.True(File.Exists(Path.Combine(options.PackDirectoryPath, "pack-" + results.PackHash + ".pack")));
                    Assert.True(File.Exists(Path.Combine(options.PackDirectoryPath, "pack-" + results.PackHash + ".idx")));

                    totalNumberOfWrittenObjects += results.WrittenObjectsCount;
                }

                // assert total number of written objects count is the same as in objects database
                Assert.Equal(repo.ObjectDatabase.Count(), totalNumberOfWrittenObjects);
            });
        }

        [Fact]
        public void CanWritePackAndIndexFiles()
        {
            using (Repository repo = new Repository(SandboxStandardTestRepo()))
            {
                string path = Path.GetTempPath();
                PackBuilderResults results = repo.ObjectDatabase.Pack(new PackBuilderOptions(path));

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
                PackBuilderResults results = repo.ObjectDatabase.Pack(new PackBuilderOptions(path), b =>
                {

                });

                Assert.True(File.Exists(Path.Combine(path, "pack-" + results.PackHash + ".pack")));
                Assert.True(File.Exists(Path.Combine(path, "pack-" + results.PackHash + ".idx")));
            }
        }

        [Fact]
        public void TestPackFileForEmptyRepository()
        {
            using (Repository repo = new Repository(InitNewRepository()))
            {
                string path = Path.GetTempPath();
                PackBuilderResults results = repo.ObjectDatabase.Pack(new PackBuilderOptions(path));

                Assert.True(File.Exists(Path.Combine(path, "pack-" + results.PackHash + ".pack")));
                Assert.True(File.Exists(Path.Combine(path, "pack-" + results.PackHash + ".idx")));
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

        internal void TestBody(Action<IRepository, PackBuilderOptions> fullPackingAction)
        {
            // read a repo, pack with the provided action, write the pack file in a mirror repo, read new repo, compare

            string orgRepoPath = SandboxPackBuilderTestRepo();
            string mrrRepoPath = SandboxPackBuilderTestRepo();
            string mrrRepoPackDirPath = Path.Combine(mrrRepoPath + "/.git/objects");

            DirectoryHelper.DeleteDirectory(mrrRepoPackDirPath);
            Directory.CreateDirectory(mrrRepoPackDirPath + "/pack");

            PackBuilderOptions packBuilderOptions = new PackBuilderOptions(mrrRepoPackDirPath + "/pack");

            using (Repository orgRepo = new Repository(orgRepoPath))
            {
                fullPackingAction(orgRepo, packBuilderOptions);

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
