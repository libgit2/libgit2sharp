using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class FileHistoryFixture : BaseFixture
    {
        //Looks like nulltoken deleted the repo this test was using

        //[Theory]
        //[InlineData("https://github.com/nulltoken/follow-test.git")]
        //public void CanDealWithFollowTest(string url)
        //{
        //    var scd = BuildSelfCleaningDirectory();
        //    var clonedRepoPath = Repository.Clone(url, scd.DirectoryPath);

        //    using (var repo = new Repository(clonedRepoPath))
        //    {
        //        // $ git log --follow --format=oneline so-renamed.txt
        //        // 88f91835062161febb46fb270ef4188f54c09767 Update not-yet-renamed.txt AND rename into so-renamed.txt
        //        // ef7cb6a63e32595fffb092cb1ae9a32310e58850 Add not-yet-renamed.txt
        //        var fileHistoryEntries = repo.Commits.QueryBy("so-renamed.txt").ToList();
        //        Assert.Equal(2, fileHistoryEntries.Count());
        //        Assert.Equal("88f91835062161febb46fb270ef4188f54c09767", fileHistoryEntries[0].Commit.Sha);
        //        Assert.Equal("ef7cb6a63e32595fffb092cb1ae9a32310e58850", fileHistoryEntries[1].Commit.Sha);

        //        // $ git log --follow --format=oneline untouched.txt
        //        // c10c1d5f74b76f20386d18674bf63fbee6995061 Initial commit
        //        fileHistoryEntries = repo.Commits.QueryBy("untouched.txt").ToList();
        //        Assert.Single(fileHistoryEntries);
        //        Assert.Equal("c10c1d5f74b76f20386d18674bf63fbee6995061", fileHistoryEntries[0].Commit.Sha);

        //        // $ git log --follow --format=oneline under-test.txt
        //        // 0b5b18f2feb917dee98df1210315b2b2b23c5bec Rename file renamed.txt into under-test.txt
        //        // 49921d463420a892c9547a326632ef6a9ba3b225 Update file renamed.txt
        //        // 70f636e8c64bbc2dfef3735a562bb7e195d8019f Rename file under-test.txt into renamed.txt
        //        // d3868d57a6aaf2ae6ed4887d805ae4bc91d8ce4d Updated file under test
        //        // 9da10ef7e139c49604a12caa866aae141f38b861 Updated file under test
        //        // 599a5d821fb2c0a25855b4233e26d475c2fbeb34 Updated file under test
        //        // 678b086b44753000567aa64344aa0d8034fa0083 Updated file under test
        //        // 8f7d9520f306771340a7c79faea019ad18e4fa1f Updated file under test
        //        // bd5f8ee279924d33be8ccbde82e7f10b9d9ff237 Updated file under test
        //        // c10c1d5f74b76f20386d18674bf63fbee6995061 Initial commit
        //        fileHistoryEntries = repo.Commits.QueryBy("under-test.txt").ToList();
        //        Assert.Equal(10, fileHistoryEntries.Count());
        //        Assert.Equal("0b5b18f2feb917dee98df1210315b2b2b23c5bec", fileHistoryEntries[0].Commit.Sha);
        //        Assert.Equal("49921d463420a892c9547a326632ef6a9ba3b225", fileHistoryEntries[1].Commit.Sha);
        //        Assert.Equal("70f636e8c64bbc2dfef3735a562bb7e195d8019f", fileHistoryEntries[2].Commit.Sha);
        //        Assert.Equal("d3868d57a6aaf2ae6ed4887d805ae4bc91d8ce4d", fileHistoryEntries[3].Commit.Sha);
        //        Assert.Equal("9da10ef7e139c49604a12caa866aae141f38b861", fileHistoryEntries[4].Commit.Sha);
        //        Assert.Equal("599a5d821fb2c0a25855b4233e26d475c2fbeb34", fileHistoryEntries[5].Commit.Sha);
        //        Assert.Equal("678b086b44753000567aa64344aa0d8034fa0083", fileHistoryEntries[6].Commit.Sha);
        //        Assert.Equal("8f7d9520f306771340a7c79faea019ad18e4fa1f", fileHistoryEntries[7].Commit.Sha);
        //        Assert.Equal("bd5f8ee279924d33be8ccbde82e7f10b9d9ff237", fileHistoryEntries[8].Commit.Sha);
        //        Assert.Equal("c10c1d5f74b76f20386d18674bf63fbee6995061", fileHistoryEntries[9].Commit.Sha);
        //    }
        //}

        [Theory]
        [InlineData(null)]
        public void CanFollowBranches(string specificRepoPath)
        {
            var repoPath = specificRepoPath ?? CreateEmptyRepository();
            var path = "Test.txt";

            var dummy = new string('a', 1024);

            using (var repo = new Repository(repoPath))
            {
                var master0 = AddCommitToOdb(repo, "0. Initial commit for this test", path, "Before merge", dummy);
                var fix1 = AddCommitToOdb(repo, "1. Changed on fix", path, "Change on fix branch", dummy, master0);
                var master2 = AddCommitToOdb(repo, "2. Changed on master", path, "Independent change on master branch",
                    dummy, master0);

                path = "New" + path;

                var fix3 = AddCommitToOdb(repo, "3. Changed and renamed on fix", path, "Another change on fix branch",
                    dummy, fix1);
                var master4 = AddCommitToOdb(repo, "4. Changed and renamed on master", path,
                    "Another independent change on master branch", dummy, master2);
                var master5 = AddCommitToOdb(repo, "5. Merged fix into master", path,
                    "Manual resolution of merge conflict", dummy, master4, fix3);
                var master6 = AddCommitToOdb(repo, "6. Changed on master", path, "Change after merge", dummy, master5);
                var nextfix7 = AddCommitToOdb(repo, "7. Changed on next-fix", path, "Change on next-fix branch", dummy,
                    master6);
                var master8 = AddCommitToOdb(repo, "8. Changed on master", path,
                    "Some arbitrary change on master branch", dummy, master6);
                var master9 = AddCommitToOdb(repo, "9. Merged next-fix into master", path,
                    "Another manual resolution of merge conflict", dummy, master8, nextfix7);
                var master10 = AddCommitToOdb(repo, "10. Changed on master", path, "A change on master after merging",
                    dummy, master9);

                repo.CreateBranch("master", master10);
                Commands.Checkout(repo, "master", new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });

                // Test --date-order.
                var timeHistory = repo.Commits.QueryBy(path,
                    new CommitFilter { SortBy = CommitSortStrategies.Time });
                var timeCommits = new List<Commit>
                {
                    master10, // master

                    master8, // master
                    nextfix7, // next-fix
                    master6, // master

                    master4, // master
                    fix3, // fix
                    master2, // master
                    fix1, // fix
                    master0 // master (initial commit)
                };
                Assert.Equal(timeCommits, timeHistory.Select(e => e.Commit));

                // Test --topo-order.
                var topoHistory = repo.Commits.QueryBy(path,
                    new CommitFilter { SortBy = CommitSortStrategies.Topological });
                var topoCommits = new List<Commit>
                {
                    master10, // master

                    nextfix7, // next-fix
                    master8, // master
                    master6, // master

                    fix3, // fix
                    fix1, // fix
                    master4, // master
                    master2, // master
                    master0 // master (initial commit)
                };
                Assert.Equal(topoCommits, topoHistory.Select(e => e.Commit));
            }
        }

        [Fact]
        public void CanTellComplexCommitHistory()
        {
            var repoPath = CreateEmptyRepository();
            const string path1 = "Test1.txt";
            const string path2 = "Test2.txt";

            using (var repo = new Repository(repoPath))
            {
                // Make initial changes.
                var commit1 = MakeAndCommitChange(repo, repoPath, path1, "Hello World");
                MakeAndCommitChange(repo, repoPath, path2, "Second file's contents");
                var commit2 = MakeAndCommitChange(repo, repoPath, path1, "Hello World again");

                // Move the first file to a new directory.
                var newPath1 = Path.Combine(SubFolderPath1, path1).Replace(@"\", "/");
                Commands.Move(repo, path1, newPath1);
                var commit3 = repo.Commit("Moved " + path1 + " to " + newPath1,
                    Constants.Signature, Constants.Signature);

                // Make further changes.
                MakeAndCommitChange(repo, repoPath, path2, "Changed second file's contents");
                var commit4 = MakeAndCommitChange(repo, repoPath, newPath1, "I have done it again!");

                // Perform tests.
                var commitFilter = new CommitFilter() { SortBy = CommitSortStrategies.Topological };
                var fileHistoryEntries = repo.Commits.QueryBy(newPath1, commitFilter).ToList();
                var changedBlobs = fileHistoryEntries.Blobs().Distinct().ToList();

                Assert.Equal(4, fileHistoryEntries.Count());
                Assert.Equal(3, changedBlobs.Count());

                Assert.Equal(2, fileHistoryEntries.Count(e => e.Path == newPath1));
                Assert.Equal(2, fileHistoryEntries.Count(e => e.Path == path1));

                Assert.Equal(commit4, fileHistoryEntries[0].Commit);
                Assert.Equal(commit3, fileHistoryEntries[1].Commit);
                Assert.Equal(commit2, fileHistoryEntries[2].Commit);
                Assert.Equal(commit1, fileHistoryEntries[3].Commit);

                Assert.Equal(commit4.Tree[newPath1].Target, changedBlobs[0]);
                Assert.Equal(commit2.Tree[path1].Target, changedBlobs[1]);
                Assert.Equal(commit1.Tree[path1].Target, changedBlobs[2]);
            }
        }

        [Fact]
        public void CanTellSimpleCommitHistory()
        {
            var repoPath = CreateEmptyRepository();
            const string path1 = "Test1.txt";
            const string path2 = "Test2.txt";

            using (var repo = new Repository(repoPath))
            {
                // Set up repository.
                var commit1 = MakeAndCommitChange(repo, repoPath, path1, "Hello World");
                MakeAndCommitChange(repo, repoPath, path2, "Second file's contents");
                var commit3 = MakeAndCommitChange(repo, repoPath, path1, "Hello World again");

                // Perform tests.
                IEnumerable<LogEntry> history = repo.Commits.QueryBy(path1).ToList();
                var changedBlobs = history.Blobs().Distinct();

                Assert.Equal(2, history.Count());
                Assert.Equal(2, changedBlobs.Count());

                Assert.Equal(commit3, history.ElementAt(0).Commit);
                Assert.Equal(commit1, history.ElementAt(1).Commit);
            }
        }

        [Fact]
        public void CanTellSingleCommitHistory()
        {
            var repoPath = CreateEmptyRepository();

            using (var repo = new Repository(repoPath))
            {
                // Set up repository.
                const string path = "Test.txt";
                var commit = MakeAndCommitChange(repo, repoPath, path, "Hello World");

                // Perform tests.
                IEnumerable<LogEntry> history = repo.Commits.QueryBy(path).ToList();
                var changedBlobs = history.Blobs().Distinct();

                Assert.Single(history);
                Assert.Single(changedBlobs);

                Assert.Equal(path, history.First().Path);
                Assert.Equal(commit, history.First().Commit);
            }
        }

        [Fact]
        public void EmptyRepositoryHasNoHistory()
        {
            var repoPath = CreateEmptyRepository();

            using (var repo = new Repository(repoPath))
            {
                IEnumerable<LogEntry> history = repo.Commits.QueryBy("Test.txt").ToList();
                Assert.Empty(history);
                Assert.Empty(history.Blobs());
            }
        }

        [Fact]
        public void UnsupportedSortStrategyThrows()
        {
            var repoPath = CreateEmptyRepository();

            using (var repo = new Repository(repoPath))
            {
                // Set up repository.
                const string path = "Test.txt";
                MakeAndCommitChange(repo, repoPath, path, "Hello World");

                Assert.Throws<ArgumentException>(() =>
                    repo.Commits.QueryBy(path, new CommitFilter
                    {
                        SortBy = CommitSortStrategies.None
                    }));

                Assert.Throws<ArgumentException>(() =>
                    repo.Commits.QueryBy(path, new CommitFilter
                    {
                        SortBy = CommitSortStrategies.Reverse
                    }));

                Assert.Throws<ArgumentException>(() =>
                    repo.Commits.QueryBy(path, new CommitFilter
                    {
                        SortBy = CommitSortStrategies.Reverse |
                                 CommitSortStrategies.Topological
                    }));

                Assert.Throws<ArgumentException>(() =>
                    repo.Commits.QueryBy(path, new CommitFilter
                    {
                        SortBy = CommitSortStrategies.Reverse |
                                 CommitSortStrategies.Time
                    }));

                Assert.Throws<ArgumentException>(() =>
                    repo.Commits.QueryBy(path, new CommitFilter
                    {
                        SortBy = CommitSortStrategies.Reverse |
                                 CommitSortStrategies.Topological |
                                 CommitSortStrategies.Time
                    }));
            }
        }

        #region Helpers

        private Signature _signature = Constants.Signature;
        private const string SubFolderPath1 = "SubFolder1";

        private Signature GetNextSignature()
        {
            _signature = _signature.TimeShift(TimeSpan.FromMinutes(1));
            return _signature;
        }

        private string CreateEmptyRepository()
        {
            // Create a new empty directory with subfolders.
            var scd = BuildSelfCleaningDirectory();
            Directory.CreateDirectory(Path.Combine(scd.DirectoryPath, SubFolderPath1));

            // Initialize a GIT repository in that directory.
            Repository.Init(scd.DirectoryPath);
            using (var repo = new Repository(scd.DirectoryPath))
            {
                repo.Config.Set("user.name", _signature.Name);
                repo.Config.Set("user.email", _signature.Email);
            }

            // Done.
            return scd.DirectoryPath;
        }

        /// <summary>
        /// Adds a commit to the object database. The tree will have a single text file with the given specific content.
        /// </summary>
        /// <param name="repo">The repository.</param>
        /// <param name="message">The commit message.</param>
        /// <param name="path">The file's path.</param>
        /// <param name="specificContent">The file's content.</param>
        /// <param name="parents">The commit's parents.</param>
        /// <returns>The commit added to the object database.</returns>
        private Commit AddCommitToOdb(Repository repo, string message, string path, string specificContent,
            params Commit[] parents)
        {
            return AddCommitToOdb(repo, message, path, specificContent, null, parents);
        }

        /// <summary>
        /// Adds a commit to the object database. The tree will have a single text file with the given specific content
        /// at the beginning of the file and the given common content at the end of the file.
        /// </summary>
        /// <param name="repo">The repository.</param>
        /// <param name="message">The commit message.</param>
        /// <param name="path">The file's path.</param>
        /// <param name="specificContent">The content specific to that file.</param>
        /// <param name="commonContent">The content shared with other files.</param>
        /// <param name="parents">The commit's parents.</param>
        /// <returns>The commit added to the object database.</returns>
        private Commit AddCommitToOdb(Repository repo, string message, string path, string specificContent,
            string commonContent, params Commit[] parents)
        {
            var content = string.IsNullOrEmpty(commonContent)
                ? specificContent
                : specificContent + Environment.NewLine + commonContent + Environment.NewLine;

            var td = new TreeDefinition();
            td.Add(path, OdbHelper.CreateBlob(repo, content), Mode.NonExecutableFile);
            var t = repo.ObjectDatabase.CreateTree(td);

            var commitSignature = GetNextSignature();

            return repo.ObjectDatabase.CreateCommit(commitSignature, commitSignature, message, t, parents, true);
        }

        private Commit MakeAndCommitChange(Repository repo, string repoPath, string path, string text,
            string message = null)
        {
            Touch(repoPath, path, text);
            Commands.Stage(repo, path);

            var commitSignature = GetNextSignature();
            return repo.Commit(message ?? "Changed " + path, commitSignature, commitSignature);
        }

        #endregion
    }

    /// <summary>
    /// Defines extensions used by <see cref="FileHistoryFixture"/>.
    /// </summary>
    internal static class FileHistoryFixtureExtensions
    {
        /// <summary>
        /// Gets the <see cref="Blob"/> instances contained in each <see cref="LogEntry"/>.
        /// </summary>
        /// <remarks>
        /// Use the <see cref="Enumerable.Distinct{TSource}(IEnumerable{TSource})"/> extension method
        /// to retrieve the changed blobs.
        /// </remarks>
        /// <param name="fileHistory">The file history.</param>
        /// <returns>The collection of <see cref="Blob"/> instances included in the file history.</returns>
        public static IEnumerable<Blob> Blobs(this IEnumerable<LogEntry> fileHistory)
        {
            return fileHistory.Select(entry => entry.Commit.Tree[entry.Path].Target).OfType<Blob>();
        }
    }
}
