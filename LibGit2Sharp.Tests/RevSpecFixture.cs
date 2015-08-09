using System;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class RevSpecFixture : BaseFixture
    {
        private const string FirstCommit = "32eab9cb1f450b5fe7ab663462b77d7f4b703344"; // Add "1.txt" file beside "1" folder
        private const string PreviousCommit = "592d3c869dbc4127fc57c189cb94f2794fa84e7e"; // add more test files
        private const string SpecificCommit1 = "4c062a6361ae6959e06292c1fa5e2822d9c96345"; // directory was added
        private const string SpecificCommit2 = "9fd738e8f7967c078dceed8190330fc8648ee56a"; // a fourth commit
        private const string SpecificCommit3 = "5b5b025afb0b4c913b4c338a42934a3863bf3644"; // another commit
        private const string OriginCommit = "580c2111be43802dab11328176d94c391f1deae9"; // Remote-only commit 2

        [Theory]
        [InlineData(FirstCommit, FirstCommit, null, RevSpecType.Single)]
        [InlineData(SpecificCommit2 + "..HEAD", SpecificCommit2, FirstCommit, RevSpecType.Range)]
        [InlineData(SpecificCommit2 + ".." + SpecificCommit1, SpecificCommit2, SpecificCommit1, RevSpecType.Range)]
        [InlineData("HEAD^1..HEAD", PreviousCommit, FirstCommit, RevSpecType.Range)]
        [InlineData("HEAD~6..HEAD", SpecificCommit3, FirstCommit, RevSpecType.Range)]
        [InlineData("origin..HEAD", OriginCommit, FirstCommit, RevSpecType.Range)]
        public void TestFromPreviousCommit(string spec, string expectedFromSha, string expectedToSha, RevSpecType expectedType)
        {
            var path = SandboxStandardTestRepoGitDir();
            using (var repo = new Repository(path))
            {
                var result = RevSpec.Parse(repo, spec);
                if (expectedFromSha == null)
                {
                    Assert.Null(result.From);
                }
                else
                {
                    Assert.NotNull(result.From);
                    Assert.IsAssignableFrom<Commit>(result.From);
                    Assert.Equal(expectedFromSha, result.From.Sha);
                }

                if (expectedToSha == null)
                {
                    Assert.Null(result.To);
                }
                else
                {
                    Assert.NotNull(result.To);
                    Assert.IsAssignableFrom<Commit>(result.To);
                    Assert.Equal(expectedToSha, result.To.Sha);
                }

                Assert.Equal(expectedType, result.Type);
            }
        }
    }
}