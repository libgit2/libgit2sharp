using System;
using System.IO;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class TemporaryRepositoryPath : IDisposable
    {
        private readonly string _tempRepoToDelete;

        public TemporaryRepositoryPath()
        {
            var tempDirectory = Path.Combine(Constants.TemporaryReposPath, Guid.NewGuid().ToString().Substring(0, 8));

            var source = new DirectoryInfo(Constants.TestRepoPath);
            var tempRepository = new DirectoryInfo(Path.Combine(tempDirectory, source.Name));
            _tempRepoToDelete = tempRepository.Parent.FullName;

            DirectoryHelper.CopyFilesRecursively(source, tempRepository);

            RepositoryPath = tempRepository.FullName;
        }

        public string RepositoryPath { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            DirectoryHelper.DeleteIfExists(_tempRepoToDelete);
        }

        #endregion
    }
}