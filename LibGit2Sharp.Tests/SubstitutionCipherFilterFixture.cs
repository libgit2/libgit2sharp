using System;
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class SubstitutionCipherFilterFixture : BaseFixture
    {
        [Fact]
        public void CorrectlyEncodesAndDecodesInput()
        {
            const string decodedInput = "This is a substitution cipher";
            const string encodedInput = "Guvf vf n fhofgvghgvba pvcure";

            var attributes = new List<string> { ".rot13" };
            var filter = new SubstitutionCipherFilter("ROT13", attributes);
            GlobalSettings.RegisterFilter(filter);

            string repoPath = InitNewRepository();
            string fileName = Guid.NewGuid() + ".rot13";
            string configPath = CreateConfigurationWithDummyUser(Constants.Signature);
            var repositoryOptions = new RepositoryOptions { GlobalConfigurationLocation = configPath };
            using (var repo = new Repository(repoPath, repositoryOptions))

            {
                var blob = CommitOnBranchAndReturnDatabaseBlob(repo, fileName, decodedInput);
                var textDetected = blob.GetContentText();

                Assert.Equal(encodedInput, textDetected);
                Assert.Equal(1, filter.CleanCalledCount);
                Assert.Equal(0, filter.SmudgeCalledCount);

                var branch = repo.CreateBranch("delete-files");
                repo.Checkout(branch.Name);

                DeleteFile(repo, fileName);

                repo.Checkout("master");

                var fileContents = ReadTextFromFile(repo, fileName);
                Assert.Equal(1, filter.SmudgeCalledCount);
                Assert.Equal(decodedInput, fileContents);
            }

            GlobalSettings.DeregisterFilter(filter.Name);
        }

        [Fact]
        public void WhenAttributesDoNotMatchFileIsNotFilterd()
        {
            const string decodedInput = "This is a substitution cipher";

            var attributes = new List<string> { ".rot13" };
            var filter = new SubstitutionCipherFilter("ROT13", attributes);
            GlobalSettings.RegisterFilter(filter);

            string repoPath = InitNewRepository();
            string fileName = Guid.NewGuid() + ".rot131";

            string configPath = CreateConfigurationWithDummyUser(Constants.Signature);
            var repositoryOptions = new RepositoryOptions { GlobalConfigurationLocation = configPath };
            using (var repo = new Repository(repoPath, repositoryOptions))
            {
                CommitOnBranchAndReturnDatabaseBlob(repo, fileName, decodedInput);

                Assert.Equal(0, filter.CleanCalledCount);
                Assert.Equal(0, filter.SmudgeCalledCount);
                Assert.Equal(1, filter.CheckCalledCount);
            }

            GlobalSettings.DeregisterFilter(filter.Name);
        }

        private static string ReadTextFromFile(Repository repo, string fileName)
        {
            return File.ReadAllText(Path.Combine(repo.Info.WorkingDirectory, fileName));
        }

        private static void DeleteFile(Repository repo, string fileName)
        {
            File.Delete(Path.Combine(repo.Info.WorkingDirectory, fileName));
            repo.Stage(fileName);
            repo.Commit("remove file");
        }

        private static Blob CommitOnBranchAndReturnDatabaseBlob(Repository repo, string fileName, string input)
        {
            Touch(repo.Info.WorkingDirectory, fileName, input);
            repo.Stage(fileName);

            var commit = repo.Commit("new file");

            var blob = (Blob)commit.Tree[fileName].Target;
            return blob;
        }
    }
}