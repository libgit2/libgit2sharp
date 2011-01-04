using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace libgit2sharp.Tests
{
    public class ReadWriteRepositoryFixtureBase : RepositoryToBeCreatedFixtureBase
    {
        private const string readOnlyGitRepository = "../../Resources/testrepo.git";

        private string _pathToRepository;
        
        protected string PathToRepository { get { return _pathToRepository; } }

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            var source = new DirectoryInfo(readOnlyGitRepository);
            var tempRepository = new DirectoryInfo(Path.Combine(PathToTempDirectory, source.Name));

            CopyFilesRecursively(source, tempRepository);

            _pathToRepository = tempRepository.FullName;
        }

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            // From http://stackoverflow.com/questions/58744/best-way-to-copy-the-entire-contents-of-a-directory-in-c/58779#58779

            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
        }
    }
}