using System.IO;
using NUnit.Framework;

namespace LibGit2Sharp.Tests
{
    public class ReadWriteRepositoryFixtureBase : RepositoryToBeCreatedFixtureBase
    {
        protected string PathToReadWriteRepository { get; private set; }

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            // From http://stackoverflow.com/questions/58744/best-way-to-copy-the-entire-contents-of-a-directory-in-c/58779#58779

            foreach (var dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (var file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
        }

        [TestFixtureSetUp]
        public override void Setup()
        {
            base.Setup();

            var source = new DirectoryInfo(Constants.TestRepoPath);
            var tempRepository = new DirectoryInfo(Path.Combine(PathToTempDirectory, source.Name));

            CopyFilesRecursively(source, tempRepository);

            PathToReadWriteRepository = tempRepository.FullName;
        }
    }
}