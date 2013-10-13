using LibGit2Sharp.Tests.TestHelpers;
using System;
using System.IO;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Tests
{
    public class EncodingFixture : BaseFixture
    {
        [Theory]
        //[InlineData("7bc349e5efdb52884cf47d860852e4912b12c244", "MãcRömãñ", 1000)]
        [InlineData("782dced4c3930bc3c291b24c115ba9a231049d80", "Lãtïñ Òñê", 1250)]
        //[InlineData("29b28e7d3e84aef886c0e00ff9de1a8cc1fda352", "Cõdêpâgê850", 850)]
        public void CorruptAuthorNameEncoding(string commitId, string authorName, int commitEncoding)
        {
            string path = CloneEncodingTestRepo();
            
            using (var repo = new Repository(path))
            {
                Commit commit = repo.Lookup<Commit>(commitId);
                Assert.Equal(authorName, commit.Author.Name);
            }
        }

    }
}
