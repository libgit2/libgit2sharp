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
            if (Directory.Exists(Constants.TestRepoWithWorkingDirPath))
                DirectoryHelper.DeleteDirectory(Constants.TestRepoWithWorkingDirPath);
            Directory.Move(Constants.TestRepoWithWorkingDirPathDotGit, Constants.TestRepoWithWorkingDirPath);
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
        public void CanEnumerateModifiedFiles()
        {
        }

        [Test]
        public void CanEnumerateUntrackedFiles()
        {
        }

        [Test]
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
        public void CanStageFile()
        {
        }

        [Test]
        public void CanUnStageFile()
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
        }

        [Test]
        public void UnStageFileWithBadParamsFails()
        {
        }
    }
}