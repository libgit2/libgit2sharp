using NUnit.Framework;
using LibGit2Sharp.Tests.TestHelpers;

using System.IO;
using System.Collections.Generic;

namespace LibGit2Sharp.Tests
{
    [TestFixture]
    public class RepositoryStatusFixture : BaseFixture
    {
        private List<string> m_directories = new List<string>();

        [Test]
        public void CanGetNativeFileStatusPaths()
        {
            // Create a new repository
            SelfCleaningDirectory scd = BuildSelfCleaningDirectory();
            string dir = Repository.Init(scd.DirectoryPath, false);

            string filename = "Testfile.txt";

            // Create a file and insert some content
            string filePath = Path.Combine(scd.RootedDirectoryPath, filename);

            FileStream fs = File.Create(filePath);
            StreamWriter sw = new StreamWriter(fs);

            sw.WriteLine("Anybody out there?");
            sw.Flush();
            sw.Close();
            fs.Close();

            // Open the repository
            Repository repo = new Repository(dir);

            // Add the file to the index
            repo.Index.Stage(filePath);

            // Get the repository status
            RepositoryStatus repoStatus = repo.Index.RetrieveStatus();

            foreach (string relpath in repoStatus.Added)
            {
                string fullpath = Path.Combine(scd.RootedDirectoryPath, relpath);

                // There is only one file
                // => So we can stop here
                Assert.IsTrue(fullpath == filePath);

            }

        }

    }
}
