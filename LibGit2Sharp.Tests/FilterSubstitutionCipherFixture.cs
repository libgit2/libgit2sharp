using System;
using System.IO;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class FilterSubstitutionCipherFixture : BaseFixture
    {
        private const string AttrAny = "*";
        private const string AttrValA = "real";
        private const string AttrValB = "fake";
        private const string DecodedInput = "This is a substitution cipher";
        private const string EncodedInput = "Guvf vf n fhofgvghgvba pvcure";
        private const string PathValA = ".yes";
        private const string FileValB = ".not";
        private const string FilterName = "cipher-filter";
        private const string PathAny = "*";
        private const string PathMatchA = "*" + PathValA;
        private const string PathMatchB = "*" + FileValB;

        [Theory]
        [InlineData(AttrValA, AttrValA, PathValA, PathMatchA, false, 1)]
        [InlineData(AttrValA, AttrValA, PathValA, PathMatchA, true, 1)]
        [InlineData(AttrValA, AttrValA, PathValA, PathMatchB, false, 0)]
        [InlineData(AttrValA, AttrValA, PathValA, PathMatchB, true, 0)]
        [InlineData(AttrValA, AttrValB, PathValA, PathMatchA, false, 0)]
        [InlineData(AttrValA, AttrValB, PathValA, PathMatchA, true, 0)]
        [InlineData(AttrValA, AttrValB, PathValA, PathMatchB, false, 0)]
        [InlineData(AttrValA, AttrValB, PathValA, PathMatchB, true, 0)]
        [InlineData(AttrValA, AttrValA, PathValA, PathAny, false, 1)]
        [InlineData(AttrValA, AttrValA, PathValA, PathAny, true, 1)]
        [InlineData(AttrValA, AttrValB, PathValA, PathAny, false, 0)]
        [InlineData(AttrValA, AttrValB, PathValA, PathAny, true, 0)]
        [InlineData(AttrAny, AttrValA, PathValA, PathMatchA, false, 1)]
        [InlineData(AttrAny, AttrValA, PathValA, PathMatchA, true, 1)]
        [InlineData(AttrAny, AttrValA, PathValA, PathMatchB, false, 0)]
        [InlineData(AttrAny, AttrValA, PathValA, PathMatchB, true, 0)]
        [InlineData(AttrAny, AttrValB, PathValA, PathMatchA, false, 1)]
        [InlineData(AttrAny, AttrValB, PathValA, PathMatchA, true, 1)]
        [InlineData(AttrAny, AttrValB, PathValA, PathMatchB, false, 0)]
        [InlineData(AttrAny, AttrValB, PathValA, PathMatchB, true, 0)]
        [InlineData(AttrAny, AttrValA, PathValA, PathAny, false, 1)]
        [InlineData(AttrAny, AttrValA, PathValA, PathAny, true, 1)]
        public void CleanIsCalledWhenMatches(string attrValue, string attrMatch, string pathValue, string pathMatch, bool clrfTaint, int expectedCount)
        {
            SubstitutionCipherFilter.Clear();

            var filterRegistration = GlobalSettings.RegisterFilter<SubstitutionCipherFilter>(FilterName, attrValue);

            try
            {
                string repoPath = InitNewRepository();
                string fileName = Guid.NewGuid() + pathValue;
                string configPath = CreateConfigurationWithDummyUser(Constants.Identity);
                var repositoryOptions = new RepositoryOptions { GlobalConfigurationLocation = configPath };

                using (var repo = new Repository(repoPath, repositoryOptions))
                {
                    string format = clrfTaint
                        ? "{0} filter={1} TEXT"
                        : "{0} filter={1}";

                    string attributeEntry = String.Format(format, pathMatch, attrMatch);

                    CreateAttributesFile(repo, attributeEntry);

                    Blob blob = CommitOnBranchAndReturnDatabaseBlob(repo, fileName, DecodedInput);
                    Assert.Equal(expectedCount == 0 ? DecodedInput : EncodedInput, blob.GetContentText());

                    Assert.Equal(expectedCount, SubstitutionCipherFilter.CleanCalledCount);
                    Assert.Equal(expectedCount * 0, SubstitutionCipherFilter.SmudgeCalledCount);
                }
            }
            finally
            {
                GlobalSettings.UnregisterFilter(filterRegistration);
            }
        }

        [Theory]
        [InlineData(AttrValA, AttrValA, PathValA, PathMatchA, false, 1)]
        [InlineData(AttrValA, AttrValA, PathValA, PathMatchA, true, 1)]
        [InlineData(AttrValA, AttrValA, PathValA, PathMatchB, false, 0)]
        [InlineData(AttrValA, AttrValA, PathValA, PathMatchB, true, 0)]
        [InlineData(AttrValA, AttrValB, PathValA, PathMatchA, false, 0)]
        [InlineData(AttrValA, AttrValB, PathValA, PathMatchA, true, 0)]
        [InlineData(AttrValA, AttrValB, PathValA, PathMatchB, false, 0)]
        [InlineData(AttrValA, AttrValB, PathValA, PathMatchB, true, 0)]
        [InlineData(AttrValA, AttrValA, PathValA, PathAny, false, 1)]
        [InlineData(AttrValA, AttrValA, PathValA, PathAny, true, 1)]
        [InlineData(AttrValA, AttrValB, PathValA, PathAny, false, 0)]
        [InlineData(AttrValA, AttrValB, PathValA, PathAny, true, 0)]
        [InlineData(AttrAny, AttrValA, PathValA, PathMatchA, false, 1)]
        [InlineData(AttrAny, AttrValA, PathValA, PathMatchA, true, 1)]
        [InlineData(AttrAny, AttrValA, PathValA, PathMatchB, false, 0)]
        [InlineData(AttrAny, AttrValA, PathValA, PathMatchB, true, 0)]
        [InlineData(AttrAny, AttrValB, PathValA, PathMatchA, false, 1)]
        [InlineData(AttrAny, AttrValB, PathValA, PathMatchA, true, 1)]
        [InlineData(AttrAny, AttrValB, PathValA, PathMatchB, false, 0)]
        [InlineData(AttrAny, AttrValB, PathValA, PathMatchB, true, 0)]
        [InlineData(AttrAny, AttrValA, PathValA, PathAny, false, 1)]
        [InlineData(AttrAny, AttrValA, PathValA, PathAny, true, 1)]
        public void SmudgeIsCalledWhenMatches(string attrValue, string attrMatch, string pathValue, string pathMatch, bool clrfTaint, int expectedCount)
        {
            SubstitutionCipherFilter.Clear();

            var filterRegistration = GlobalSettings.RegisterFilter<SubstitutionCipherFilter>(FilterName, attrValue);

            try
            {
                string repoPath = InitNewRepository();
                string fileName = Guid.NewGuid() + pathValue;
                string format = clrfTaint
                    ? "{0} filter={1} TEXT"
                    : "{0} filter={1}";

                string attributeEntry = String.Format(format, pathMatch, attrMatch);

                string configPath = CreateConfigurationWithDummyUser(Constants.Identity);
                var repositoryOptions = new RepositoryOptions { GlobalConfigurationLocation = configPath };
                using (var repo = new Repository(repoPath, repositoryOptions))
                {
                    CreateAttributesFile(repo, attributeEntry);

                    CommitOnBranchAndReturnDatabaseBlob(repo, fileName, DecodedInput);

                    var branch = repo.CreateBranch("delete-files");
                    repo.Checkout(branch.FriendlyName);

                    DeleteFile(repo, fileName);

                    repo.Checkout("master");

                    Assert.True(SubstitutionCipherFilter.CleanCalledCount >= expectedCount);
                    Assert.Equal(expectedCount, SubstitutionCipherFilter.SmudgeCalledCount);
                }
            }
            finally
            {
                GlobalSettings.UnregisterFilter(filterRegistration);
            }

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
