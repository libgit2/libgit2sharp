using System;
using System.IO;
using System.Linq;
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

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, options))
            {
                var st = repo.RetrieveStatus("1/branch_file.txt");
                Assert.Equal(FileStatus.DeletedFromWorkdir, st);
            }
        }

        [Fact]
        public void CanOpenABareRepoAsIfItWasAStandardOneWithANonExisitingIndexFile()
        {
            var options = new RepositoryOptions { WorkingDirectoryPath = newWorkdir, IndexPath = newIndex };

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, options))
            {
                var st = repo.RetrieveStatus("1/branch_file.txt");
                Assert.Equal(FileStatus.DeletedFromIndex, st);
            }
        }

        [Fact]
        public void CanOpenABareRepoWithOptions()
        {
            var options = new RepositoryOptions { GlobalConfigurationLocation = null };

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, options))
            {
                Assert.True(repo.Info.IsBare);
            }
        }

        [Fact]
        public void CanProvideADifferentWorkDirToAStandardRepo()
        {
            var path1 = SandboxStandardTestRepo();
            using (var repo = new Repository(path1))
            {
                Assert.Equal(FileStatus.Unaltered, repo.RetrieveStatus("1/branch_file.txt"));
            }

            var options = new RepositoryOptions { WorkingDirectoryPath = newWorkdir };

            var path2 = SandboxStandardTestRepo();
            using (var repo = new Repository(path2, options))
            {
                Assert.Equal(FileStatus.DeletedFromWorkdir, repo.RetrieveStatus("1/branch_file.txt"));
            }
        }

        [Fact]
        public void CanProvideADifferentIndexToAStandardRepo()
        {
            var path1 = SandboxStandardTestRepo();
            using (var repo = new Repository(path1))
            {
                Assert.Equal(FileStatus.NewInWorkdir, repo.RetrieveStatus("new_untracked_file.txt"));

                repo.Stage("new_untracked_file.txt");

                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus("new_untracked_file.txt"));

                File.Copy(Path.Combine(repo.Info.Path, "index"), newIndex);
            }

            var options = new RepositoryOptions { IndexPath = newIndex };

            var path2 = SandboxStandardTestRepo();
            using (var repo = new Repository(path2, options))
            {
                Assert.Equal(FileStatus.NewInIndex, repo.RetrieveStatus("new_untracked_file.txt"));
            }
        }

        [Fact]
        public void OpeningABareRepoWithoutProvidingBothWorkDirAndIndexThrows()
        {
            string path = SandboxBareTestRepo();
            Assert.Throws<ArgumentException>(() => new Repository(path, new RepositoryOptions {IndexPath = newIndex}));
            Assert.Throws<ArgumentException>(() => new Repository(path, new RepositoryOptions {WorkingDirectoryPath = newWorkdir}));
        }

        [Fact]
        public void CanSneakAdditionalCommitsIntoAStandardRepoWithoutAlteringTheWorkdirOrTheIndex()
        {
            string path = SandboxStandardTestRepo();
            using (var repo = new Repository(path))
            {
                Branch head = repo.Head;

                Assert.Equal(FileStatus.Nonexistent, repo.RetrieveStatus("zomg.txt"));

                string commitSha = MeanwhileInAnotherDimensionAnEvilMastermindIsAtWork(path);

                Branch newHead = repo.Head;

                Assert.NotEqual(head.Tip.Sha, newHead.Tip.Sha);
                Assert.Equal(commitSha, newHead.Tip.Sha);

                Assert.Equal(FileStatus.DeletedFromIndex, repo.RetrieveStatus("zomg.txt"));
            }
        }

        private string MeanwhileInAnotherDimensionAnEvilMastermindIsAtWork(string workingDirectoryPath)
        {
            var options = new RepositoryOptions { WorkingDirectoryPath = newWorkdir, IndexPath = newIndex };

            using (var sneakyRepo = new Repository(workingDirectoryPath, options))
            {
                Assert.Equal(Path.GetFullPath(newWorkdir) + Path.DirectorySeparatorChar, Path.GetFullPath(sneakyRepo.Info.WorkingDirectory));

                sneakyRepo.Reset(ResetMode.Mixed, sneakyRepo.Head.Tip.Sha);

                const string filename = "zomg.txt";
                Touch(sneakyRepo.Info.WorkingDirectory, filename, "I'm being sneaked in!\n");

                sneakyRepo.Stage(filename);
                return sneakyRepo.Commit("Tadaaaa!", Constants.Signature, Constants.Signature).Sha;
            }
        }

        [Fact]
        public void CanProvideDifferentConfigurationFilesToARepository()
        {
            string globalLocation = Path.Combine(newWorkdir, "my-global-config");
            string xdgLocation = Path.Combine(newWorkdir, "my-xdg-config");
            string systemLocation = Path.Combine(newWorkdir, "my-system-config");

            const string name = "Adam 'aroben' Roben";
            const string email = "adam@github.com";

            StringBuilder sb = new StringBuilder()
                .AppendLine("[user]")
                .AppendFormat("name = {0}{1}", name, Environment.NewLine)
                .AppendFormat("email = {0}{1}", email, Environment.NewLine);

            File.WriteAllText(globalLocation, sb.ToString());
            File.WriteAllText(systemLocation, string.Empty);
            File.WriteAllText(xdgLocation, string.Empty);

            var options = new RepositoryOptions {
                GlobalConfigurationLocation = globalLocation,
                XdgConfigurationLocation = xdgLocation,
                SystemConfigurationLocation = systemLocation,
            };

            string path = SandboxBareTestRepo();
            using (var repo = new Repository(path, options))
            {
                Assert.True(repo.Config.HasConfig(ConfigurationLevel.Global));
                Assert.Equal(name, repo.Config.Get<string>("user.name").Value);
                Assert.Equal(email, repo.Config.Get<string>("user.email").Value);

                repo.Config.Set("xdg.setting", "https://twitter.com/libgit2sharp", ConfigurationLevel.Xdg);
                repo.Config.Set("help.link", "https://twitter.com/xpaulbettsx/status/205761932626636800", ConfigurationLevel.System);
            }

            AssertValueInConfigFile(systemLocation, "xpaulbettsx");
        }

        [Fact]
        public void CanCommitOnBareRepository()
        {
            string repoPath = InitNewRepository(true);
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            string workPath = Path.Combine(scd.RootedDirectoryPath, "work");
            Directory.CreateDirectory(workPath);

            var repositoryOptions = new RepositoryOptions
            {
                WorkingDirectoryPath = workPath,
                IndexPath = Path.Combine(scd.RootedDirectoryPath, "index")
            };

            using (var repo = new Repository(repoPath, repositoryOptions))
            {
                const string relativeFilepath = "test.txt";
                Touch(repo.Info.WorkingDirectory, relativeFilepath, "test\n");
                repo.Stage(relativeFilepath);

                Assert.NotNull(repo.Commit("Initial commit", Constants.Signature, Constants.Signature));
                Assert.Equal(1, repo.Head.Commits.Count());
                Assert.Equal(1, repo.Commits.Count());
            }
        }
    }
}
