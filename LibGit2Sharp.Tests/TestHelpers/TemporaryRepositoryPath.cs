using System;
using System.IO;

namespace LibGit2Sharp.Tests
{
    public class TemporaryRepositoryPath : IDisposable
    {
        public TemporaryRepositoryPath()
        {
            var tempDirectory = Path.Combine(Constants.TemporaryReposPath, Guid.NewGuid().ToString().Substring(0, 8));

            var source = new DirectoryInfo(Constants.TestRepoPath);
            var tempRepository = new DirectoryInfo(Path.Combine(tempDirectory, source.Name));

            CopyFilesRecursively(source, tempRepository);

            RepositoryPath = tempRepository.FullName;
        }

        public string RepositoryPath { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            DirectoryHelper.DeleteIfExists(RepositoryPath);
        }

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            // From http://stackoverflow.com/questions/58744/best-way-to-copy-the-entire-contents-of-a-directory-in-c/58779#58779

            foreach (var dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (var file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
        }

        #endregion
    }
}