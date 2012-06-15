﻿using System;
using System.IO;
using System.Text;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class RepositoryOptionsFixture : BaseFixture
    {
        private readonly string newWorkdir;
        private readonly string newIndex;

        //TODO: Add facts ensuring the correct opening of a workdir/index through relative and absolute paths

        public RepositoryOptionsFixture()
        {
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();

            newWorkdir = Path.Combine(scd.DirectoryPath, "wd");
            Directory.CreateDirectory(newWorkdir);

            newIndex = Path.Combine(scd.DirectoryPath, "my-index");
        }

        [Fact]
        public void CanOpenABareRepoAsIfItWasAStandardOne()
        {
            File.Copy(Path.Combine(StandardTestRepoPath, "index"), newIndex);

            var options = new RepositoryOptions { WorkingDirectoryPath = newWorkdir, IndexPath = newIndex };

            using (var repo = new Repository(BareTestRepoPath, options))
            {
                var st = repo.Index.RetrieveStatus("1/branch_file.txt");
                Assert.Equal(FileStatus.Missing, st);
            }
        }

        [Fact]
        public void CanOpenABareRepoAsIfItWasAStandardOneWithANonExisitingIndexFile()
        {
            var options = new RepositoryOptions { WorkingDirectoryPath = newWorkdir, IndexPath = newIndex };

            using (var repo = new Repository(BareTestRepoPath, options))
            {
                var st = repo.Index.RetrieveStatus("1/branch_file.txt");
                Assert.Equal(FileStatus.Removed, st);
            }
        }

        [Fact]
        public void CanProvideADifferentWorkDirToAStandardRepo()
        {
            var scd = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);

            using (var repo = new Repository(scd.DirectoryPath))
            {
                Assert.Equal(FileStatus.Unaltered, repo.Index.RetrieveStatus("1/branch_file.txt"));
            }

            var options = new RepositoryOptions { WorkingDirectoryPath = newWorkdir };

            scd = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(scd.DirectoryPath, options))
            {
                Assert.Equal(FileStatus.Missing, repo.Index.RetrieveStatus("1/branch_file.txt"));
            }
        }

        [Fact]
        public void CanProvideADifferentIndexToAStandardRepo()
        {
            var scd = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);

            using (var repo = new Repository(scd.DirectoryPath))
            {
                Assert.Equal(FileStatus.Untracked, repo.Index.RetrieveStatus("new_untracked_file.txt"));

                repo.Index.Stage("new_untracked_file.txt");

                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus("new_untracked_file.txt"));

                File.Copy(Path.Combine(repo.Info.Path, "index"), newIndex);
            }

            var options = new RepositoryOptions { IndexPath = newIndex };

            scd = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);
            using (var repo = new Repository(scd.DirectoryPath, options))
            {
                Assert.Equal(FileStatus.Added, repo.Index.RetrieveStatus("new_untracked_file.txt"));
            }
        }

        [Fact]
        public void OpeningABareRepoWithoutProvidingBothWorkDirAndIndexThrows()
        {
            Repository repo;

            Assert.Throws<ArgumentException>(() => repo = new Repository(BareTestRepoPath, new RepositoryOptions { IndexPath = newIndex }));
            Assert.Throws<ArgumentException>(() => repo = new Repository(BareTestRepoPath, new RepositoryOptions { WorkingDirectoryPath = newWorkdir }));
        }

        [Fact]
        public void CanSneakAdditionalCommitsIntoAStandardRepoWithoutAlteringTheWorkdirOrTheIndex()
        {
            var scd = BuildTemporaryCloneOfTestRepo(StandardTestRepoWorkingDirPath);

            using (var repo = new Repository(scd.DirectoryPath))
            {
                IBranch head = repo.Head;

                Assert.Equal(FileStatus.Nonexistent, repo.Index.RetrieveStatus("zomg.txt"));

                string commitSha = MeanwhileInAnotherDimensionAnEvilMastermindIsAtWork(scd.DirectoryPath);

                IBranch newHead = repo.Head;

                Assert.NotEqual(head.Tip.Sha, newHead.Tip.Sha);
                Assert.Equal(commitSha, newHead.Tip.Sha);

                Assert.Equal(FileStatus.Removed, repo.Index.RetrieveStatus("zomg.txt"));
            }
        }

        private string MeanwhileInAnotherDimensionAnEvilMastermindIsAtWork(string workingDirectoryPath)
        {
            var options = new RepositoryOptions { WorkingDirectoryPath = newWorkdir, IndexPath = newIndex };

            using (var sneakyRepo = new Repository(workingDirectoryPath, options))
            {
                Assert.Equal(Path.GetFullPath(newWorkdir) + Path.DirectorySeparatorChar, Path.GetFullPath(sneakyRepo.Info.WorkingDirectory));

                sneakyRepo.Reset(ResetOptions.Mixed, sneakyRepo.Head.Tip.Sha);

                var filepath = Path.Combine(sneakyRepo.Info.WorkingDirectory, "zomg.txt");
                File.WriteAllText(filepath, "I'm being sneaked in!\n");

                sneakyRepo.Index.Stage(filepath);
                return sneakyRepo.Commit("Tadaaaa!", DummySignature, DummySignature).Sha;
            }
        }

        [Fact]
        public void CanProvideDifferentConfigurationFilesToARepository()
        {
            string globalLocation = Path.Combine(newWorkdir, "my-global-config");
            string systemLocation = Path.Combine(newWorkdir, "my-system-config");

            const string name = "Adam 'aroben' Roben";
            const string email = "adam@github.com";

            StringBuilder sb = new StringBuilder()
                .AppendLine("[user]")
                .AppendFormat("name = {0}{1}", name, Environment.NewLine)
                .AppendFormat("email = {0}{1}", email, Environment.NewLine);

            File.WriteAllText(globalLocation, sb.ToString());

            var options = new RepositoryOptions {
                GlobalConfigurationLocation = globalLocation,
                SystemConfigurationLocation = systemLocation,
            };

            using (var repo = new Repository(BareTestRepoPath, options))
            {
                Assert.True(repo.Config.HasGlobalConfig);
                Assert.Equal(name, repo.Config.Get<string>("user", "name", null));
                Assert.Equal(email, repo.Config.Get<string>("user", "email", null));

                repo.Config.Set("help.link", "https://twitter.com/xpaulbettsx/status/205761932626636800", ConfigurationLevel.System);
            }

            AssertValueInConfigFile(systemLocation, "xpaulbettsx");
        }
    }
}
