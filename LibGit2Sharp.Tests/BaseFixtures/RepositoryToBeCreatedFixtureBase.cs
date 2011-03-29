using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    public class RepositoryToBeCreatedFixtureBase
    {
        private const string testRepositoriesDirectoryName = "TestRepos";
        private static readonly string TestRepositoriesDirectoryPath = RetrieveTestRepositoriesDirectory();

        protected string PathToTempDirectory { get; private set; }

        private static void DeleteDirectory(string directoryPath)
        {
            // From http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/329502#329502

            string[] files = Directory.GetFiles(directoryPath);
            string[] dirs = Directory.GetDirectories(directoryPath);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            File.SetAttributes(directoryPath, FileAttributes.Normal);
            Directory.Delete(directoryPath, false);
        }

        private static string RetrieveAssemblyDirectory()
        {
            // From http://stackoverflow.com/questions/52797/c-how-do-i-get-the-path-of-the-assembly-the-code-is-in/283917#283917

            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        private static string RetrieveTestRepositoriesDirectory()
        {
            return Path.Combine(RetrieveAssemblyDirectory(), testRepositoriesDirectoryName);
        }

        [TestFixtureSetUp]
        public virtual void Setup()
        {
            string workDirpath = Path.Combine(Path.Combine(TestRepositoriesDirectoryPath, GetType().Name), Guid.NewGuid().ToString().Substring(0, 8));

            Directory.CreateDirectory(workDirpath);

            PathToTempDirectory = workDirpath;
        }

        [TestFixtureTearDown]
        public virtual void TestFixtureTearDown()
        {
            DeleteDirectory(TestRepositoriesDirectoryPath);
        }
    }
}