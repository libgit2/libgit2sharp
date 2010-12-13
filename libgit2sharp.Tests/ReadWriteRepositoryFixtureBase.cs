using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace libgit2sharp.Tests
{
    public class ReadWriteRepositoryFixtureBase : ReadOnlyRepositoryFixtureBase
    {
        private const string TestRepositoriesDirectoryName = "TestRepos";
        private static readonly string _testRepositoriesDirectoryPath = RetrieveTestRepositoriesDirectory();
        private string _pathToRepository;
        
        protected override string PathToRepository { get { return _pathToRepository; } }

        [SetUp]
        public void Setup()
        {
            // Create temporary working directory
            string workDirpath = Path.Combine(_testRepositoriesDirectoryPath, this.GetType().Name, Guid.NewGuid().ToString().Substring(0, 8));

            var source = new DirectoryInfo(base.PathToRepository);
            var tempRepository = new DirectoryInfo(Path.Combine(workDirpath, source.Name));

            CopyFilesRecursively(source, tempRepository);

            _pathToRepository = tempRepository.FullName;
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            Directory.Delete(_testRepositoriesDirectoryPath, true);
        }

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            // From http://stackoverflow.com/questions/58744/best-way-to-copy-the-entire-contents-of-a-directory-in-c/58779#58779

            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
        }

        static private string RetrieveTestRepositoriesDirectory()
        {
            return Path.Combine(RetrieveAssemblyDirectory(), TestRepositoriesDirectoryName);
        }

        static private string RetrieveAssemblyDirectory()
        {
            // From http://stackoverflow.com/questions/52797/c-how-do-i-get-the-path-of-the-assembly-the-code-is-in/283917#283917

            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }
}