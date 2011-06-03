using System;
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp.Tests.TestHelpers;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class IndexFixture
    {
        private readonly List<string> expectedEntries = new List<string>
                                                            {
                                                                "README",
                                                                "new.txt",
                                                                "branch_file.txt",
                                                                "1/branch_file.txt",
                                                                //"deleted_staged_file.txt", 
                                                                "deleted_unstaged_file.txt",
                                                                "modified_staged_file.txt",
                                                                "modified_unstaged_file.txt",
                                                                "new_tracked_file.txt"
                                                            };

        [TestFixtureSetUp]
        public void Setup()
        {
            const string tempDotGit = "./Resources/testrepo_wd/dot_git";

            bool gitRepoExists = Directory.Exists(Constants.TestRepoWithWorkingDirPath);
            bool dotGitDirExists = Directory.Exists(tempDotGit);

            if (gitRepoExists)
            {
                if (dotGitDirExists)
                {
                    DirectoryHelper.DeleteDirectory(tempDotGit);
                }

                return;
            }

            Directory.Move(tempDotGit, Constants.TestRepoWithWorkingDirPath);
        }

        [Test]
        public void CanCountEntriesInIndex()
        {
            using (var repo = new Repository(Constants.TestRepoWithWorkingDirPath))
            {
                repo.Index.Count.ShouldEqual(expectedEntries.Count);
            }
        }

        [Test]
        public void CanEnumerateIndex()
        {
            using (var repo = new Repository(Constants.TestRepoWithWorkingDirPath))
            {
                foreach (var entry in repo.Index)
                {
                    Assert.IsTrue(expectedEntries.Contains(entry.Path), string.Format("Could not find {0}", entry.Path));
                }
            }
        }

        [Test]
        [Ignore("Not implemented yet.")]
        public void CanEnumerateModifiedFiles()
        {
        }

        [Test]
        [Ignore("Not implemented yet.")]
        public void CanEnumerateUntrackedFiles()
        {
        }

        [Test]
        [Ignore("Not implemented yet.")]
        public void CanEnumeratorStagedFiles()
        {
        }

        [Test]
        public void CanReadIndexEntry()
        {
            using (var repo = new Repository(Constants.TestRepoWithWorkingDirPath))
            {
                var entry = repo.Index["README"];
                entry.Path.ShouldEqual("README");

                var entryWithPath = repo.Index["1/branch_file.txt"];
                entryWithPath.Path.ShouldEqual("1/branch_file.txt");
            }
        }

        [Test]
        [Ignore("Not implemented yet.")]
        public void CanStageAModifiedFile()
        {
        }

        [Test]
        public void CanStageANewFile()
        {
            using (var path = new TemporaryCloneOfTestRepo(Constants.TestRepoWithWorkingDirPath))
            using (var repo = new Repository(path.RepositoryPath))
            {
                var count = repo.Index.Count;
                const string filename = "unit_test.txt";
                File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, filename), "some contents");

                repo.Index.Stage(filename);

                repo.Index.Count.ShouldEqual(count + 1);
                repo.Index[filename].ShouldNotBeNull();
            }
        }

        [Test]
        public void CanStageANewFileInAPersistentManner()
        {
            using (var path = new TemporaryCloneOfTestRepo(Constants.TestRepoWithWorkingDirPath))
            {
                using (var repo = new Repository(path.RepositoryPath))
                {
                    const string filename = "unit_test.txt";
                    File.WriteAllText(Path.Combine(repo.Info.WorkingDirectory, filename), "some contents");

                    repo.Index.Stage(filename);
                    repo.Index[filename].ShouldNotBeNull();
                }

                using (var repo = new Repository(path.RepositoryPath))
                {
                    const string filename = "unit_test.txt";
                    repo.Index[filename].ShouldNotBeNull();
                }
            }
        }

        [Test]
        public void CanStageANewFileWithAFullPath()
        {
            using (var path = new TemporaryCloneOfTestRepo(Constants.TestRepoWithWorkingDirPath))
            using (var repo = new Repository(path.RepositoryPath))
            {
                var count = repo.Index.Count;
                const string filename = "unit_test.txt";
                string fullPath = Path.Combine(repo.Info.WorkingDirectory, filename);
                File.WriteAllText(fullPath, "some contents");

                repo.Index.Stage(fullPath);

                repo.Index.Count.ShouldEqual(count + 1);
                repo.Index[filename].ShouldNotBeNull();
            }
        }

        [Test]
        public void StagingANewFileWithAFullPathWhichEscapesOutOfTheWorkingDirThrows()
        {
            using (var scd = new SelfCleaningDirectory())
            using (var path = new TemporaryCloneOfTestRepo(Constants.TestRepoWithWorkingDirPath))
            using (var repo = new Repository(path.RepositoryPath))
            {
                var di = Directory.CreateDirectory(scd.DirectoryPath);

                const string filename = "unit_test.txt";
                string fullPath = Path.Combine(di.FullName, filename);
                File.WriteAllText(fullPath, "some contents");

                Assert.Throws<ArgumentException>(() => repo.Index.Stage(fullPath));
            }
        }

        [Test]
        [Ignore("Not implemented yet.")]
        public void CanStageAPath()
        {
        }

        [Test]
        public void CanUnstageANewFile()
        {
            using (var repo = new Repository(Constants.TestRepoWithWorkingDirPath))
            {
                var count = repo.Index.Count;
                repo.Index.Stage("new_untracked_file.txt");
                repo.Index.Count.ShouldEqual(count + 1);

                repo.Index.Unstage("new_untracked_file.txt");
                repo.Index.Count.ShouldEqual(count);
            }
        }

        [Test]
        public void UnstagingANonStagedFileThrows()
        {
            using (var repo = new Repository(Constants.TestRepoWithWorkingDirPath))
            {
                Assert.Throws<ApplicationException>(() => repo.Index.Unstage("shadowcopy_of_a_unseen_ghost.txt"));
            }
        }

        [Test]
        [Ignore("Not implemented yet.")]
        public void CanUnstageAModifiedFile()
        {
        }

        [Test]
        public void ReadIndexWithBadParamsFails()
        {
            using (var repo = new Repository(Constants.TestRepoWithWorkingDirPath))
            {
                Assert.Throws<ArgumentNullException>(() => { var entry = repo.Index[null]; });
                Assert.Throws<ArgumentException>(() => { var entry = repo.Index[string.Empty]; });
            }
        }

        [Test]
        public void StageFileWithBadParamsThrows()
        {
            using (var repo = new Repository(Constants.TestRepoWithWorkingDirPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Index.Stage(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Index.Stage(null));
            }
        }

        [Test]
        public void UnstagingFileWithBadParamsThrows()
        {
            using (var repo = new Repository(Constants.TestRepoWithWorkingDirPath))
            {
                Assert.Throws<ArgumentException>(() => repo.Index.Stage(string.Empty));
                Assert.Throws<ArgumentNullException>(() => repo.Index.Stage(null));
            }
        }
    }
}