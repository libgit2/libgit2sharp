using LibGit2Sharp.Tests.TestHelpers;
using System;
using System.IO;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class EncodingFixture : BaseFixture
    {
        [Fact]
        public void CorruptAuthorNameIssue387()
        {
            string name = "Märtin Woodwärd";

            // Get byte array for UTF8 version of author name.
            byte[] authorNameBytes = System.Text.Encoding.UTF8.GetBytes(name);
            
            var path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                string newFile = "enctest.txt";
                
                Touch(path,newFile,"Some content here");
                
                repo.Index.Stage(newFile);
                
                // Create an author name as if UTF8 bytes were CP-1252
                Commit commit = repo.Commit("Commit from lg2#",
                  new Signature(
                    System.Text.Encoding.GetEncoding(1252)
                      .GetString(authorNameBytes),
                    "martinwo@microsoft.com",
                    DateTimeOffset.Now));

                Assert.Equal(name, commit.Author.Name);
            }

        }

    }
}
