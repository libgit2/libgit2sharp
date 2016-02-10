using System;
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class FilterSubstitutionCipherFixture : BaseFixture
    {
        [Fact]
        public void SmugdeIsNotCalledForFileWhichDoesNotMatchAnAttributeEntry()
        {
            const string decodedInput = "This is a substitution cipher";
            const string encodedInput = "Guvf vf n fhofgvghgvba pvcure";

            var attributes = new List<FilterAttributeEntry> { new FilterAttributeEntry("rot13") };
            var filter = new SubstitutionCipherFilter("cipher-filter", attributes);
            var filterRegistration = GlobalSettings.RegisterFilter(filter);

            string repoPath = InitNewRepository();
            string fileName = Guid.NewGuid() + ".rot13";
            string configPath = CreateConfigurationWithDummyUser(Constants.Identity);
            var repositoryOptions = new RepositoryOptions { GlobalConfigurationLocation = configPath };
            using (var repo = new Repository(repoPath, repositoryOptions))
            {
                CreateAttributesFile(repo, "*.rot13 filter=rot13");

                var blob = CommitOnBranchAndReturnDatabaseBlob(repo, fileName, decodedInput);
                var textDetected = blob.GetContentText();

                Assert.Equal(encodedInput, textDetected);
                Assert.Equal(1, filter.CleanCalledCount);
                Assert.Equal(0, filter.SmudgeCalledCount);

                var branch = repo.CreateBranch("delete-files");
                repo.Checkout(branch.FriendlyName);

                DeleteFile(repo, fileName);

                repo.Checkout("master");

                var fileContents = ReadTextFromFile(repo, fileName);
                Assert.Equal(1, filter.SmudgeCalledCount);
                Assert.Equal(decodedInput, fileContents);
            }

            GlobalSettings.DeregisterFilter(filterRegistration);
        }

        [Fact]
        public void CorrectlyEncodesAndDecodesInput()
        {
            const string decodedInput = "This is a substitution cipher";
            const string encodedInput = "Guvf vf n fhofgvghgvba pvcure";

            var attributes = new List<FilterAttributeEntry> { new FilterAttributeEntry("rot13") };
            var filter = new SubstitutionCipherFilter("cipher-filter", attributes);
            var filterRegistration = GlobalSettings.RegisterFilter(filter);

            string repoPath = InitNewRepository();
            string fileName = Guid.NewGuid() + ".rot13";
            string configPath = CreateConfigurationWithDummyUser(Constants.Identity);
            var repositoryOptions = new RepositoryOptions { GlobalConfigurationLocation = configPath };
            using (var repo = new Repository(repoPath, repositoryOptions))
            {
                CreateAttributesFile(repo, "*.rot13 filter=rot13");

                var blob = CommitOnBranchAndReturnDatabaseBlob(repo, fileName, decodedInput);
                var textDetected = blob.GetContentText();

                Assert.Equal(encodedInput, textDetected);
                Assert.Equal(1, filter.CleanCalledCount);
                Assert.Equal(0, filter.SmudgeCalledCount);

                var branch = repo.CreateBranch("delete-files");
                repo.Checkout(branch.FriendlyName);

                DeleteFile(repo, fileName);

                repo.Checkout("master");

                var fileContents = ReadTextFromFile(repo, fileName);
                Assert.Equal(1, filter.SmudgeCalledCount);
                Assert.Equal(decodedInput, fileContents);
            }

            GlobalSettings.DeregisterFilter(filterRegistration);
        }

        [Theory]
        [InlineData("*.txt", ".bat", 0, 0)]
        [InlineData("*.txt", ".txt", 1, 0)]
        public void WhenStagedFileDoesNotMatchPathSpecFileIsNotFiltered(string pathSpec, string fileExtension, int cleanCount, int smudgeCount)
        {
            const string filterName = "rot13";
            const string decodedInput = "This is a substitution cipher";
            string attributeFileEntry = string.Format("{0} filter={1}", pathSpec, filterName);

            var filterForAttributes = new List<FilterAttributeEntry> { new FilterAttributeEntry(filterName) };
            var filter = new SubstitutionCipherFilter("cipher-filter", filterForAttributes);

            var filterRegistration = GlobalSettings.RegisterFilter(filter);

            string repoPath = InitNewRepository();
            string fileName = Guid.NewGuid() + fileExtension;

            string configPath = CreateConfigurationWithDummyUser(Constants.Identity);
            var repositoryOptions = new RepositoryOptions { GlobalConfigurationLocation = configPath };
            using (var repo = new Repository(repoPath, repositoryOptions))
            {
                CreateAttributesFile(repo, attributeFileEntry);

                CommitOnBranchAndReturnDatabaseBlob(repo, fileName, decodedInput);

                Assert.Equal(cleanCount, filter.CleanCalledCount);
                Assert.Equal(smudgeCount, filter.SmudgeCalledCount);
            }

            GlobalSettings.DeregisterFilter(filterRegistration);
        }

        [Theory]
        [InlineData("rot13", "*.txt filter=rot13", 1)]
        [InlineData("rot13", "*.txt filter=fake", 0)]
        [InlineData("rot13", "*.bat filter=rot13", 0)]
        [InlineData("rot13", "*.txt filter=fake", 0)]
        [InlineData("fake", "*.txt filter=fake", 1)]
        [InlineData("fake", "*.bat filter=fake", 0)]
        [InlineData("rot13", "*.txt filter=rot13 -crlf", 1)]
        public void CleanIsCalledIfAttributeEntryMatches(string filterAttribute, string attributeEntry, int cleanCount)
        {
            const string decodedInput = "This is a substitution cipher";

            var filterForAttributes = new List<FilterAttributeEntry> { new FilterAttributeEntry(filterAttribute) };
            var filter = new SubstitutionCipherFilter("cipher-filter", filterForAttributes);

            var filterRegistration = GlobalSettings.RegisterFilter(filter);

            string repoPath = InitNewRepository();
            string fileName = Guid.NewGuid() + ".txt";

            string configPath = CreateConfigurationWithDummyUser(Constants.Identity);
            var repositoryOptions = new RepositoryOptions { GlobalConfigurationLocation = configPath };
            using (var repo = new Repository(repoPath, repositoryOptions))
            {
                CreateAttributesFile(repo, attributeEntry);

                CommitOnBranchAndReturnDatabaseBlob(repo, fileName, decodedInput);

                Assert.Equal(cleanCount, filter.CleanCalledCount);
            }

            GlobalSettings.DeregisterFilter(filterRegistration);
        }

        [Theory]

        [InlineData("rot13", "*.txt filter=rot13", 1)]
        [InlineData("rot13", "*.txt filter=fake", 0)]
        [InlineData("rot13", "*.txt filter=rot13 -crlf", 1)]
        public void SmudgeIsCalledIfAttributeEntryMatches(string filterAttribute, string attributeEntry, int smudgeCount)
        {
            const string decodedInput = "This is a substitution cipher";

            var filterForAttributes = new List<FilterAttributeEntry> { new FilterAttributeEntry(filterAttribute) };
            var filter = new SubstitutionCipherFilter("cipher-filter", filterForAttributes);

            var filterRegistration = GlobalSettings.RegisterFilter(filter);

            string repoPath = InitNewRepository();
            string fileName = Guid.NewGuid() + ".txt";

            string configPath = CreateConfigurationWithDummyUser(Constants.Identity);
            var repositoryOptions = new RepositoryOptions { GlobalConfigurationLocation = configPath };
            using (var repo = new Repository(repoPath, repositoryOptions))
            {
                CreateAttributesFile(repo, attributeEntry);

                CommitOnBranchAndReturnDatabaseBlob(repo, fileName, decodedInput);

                var branch = repo.CreateBranch("delete-files");
                repo.Checkout(branch.FriendlyName);

                DeleteFile(repo, fileName);

                repo.Checkout("master");

                Assert.Equal(smudgeCount, filter.SmudgeCalledCount);
            }

            GlobalSettings.DeregisterFilter(filterRegistration);

        }

        private static string ReadTextFromFile(Repository repo, string fileName)
        {
            return File.ReadAllText(Path.Combine(repo.Info.WorkingDirectory, fileName));
        }

        private static void DeleteFile(Repository repo, string fileName)
        {
            File.Delete(Path.Combine(repo.Info.WorkingDirectory, fileName));
            repo.Stage(fileName);
            repo.Commit("remove file", Constants.Signature, Constants.Signature);
        }

        private static Blob CommitOnBranchAndReturnDatabaseBlob(Repository repo, string fileName, string input)
        {
            Touch(repo.Info.WorkingDirectory, fileName, input);
            repo.Stage(fileName);

            var commit = repo.Commit("new file", Constants.Signature, Constants.Signature);

            var blob = (Blob)commit.Tree[fileName].Target;
            return blob;
        }
    }
}
