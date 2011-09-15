using System;
using System.IO;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class SelfCleaningDirectory : IDisposable
    {
        public SelfCleaningDirectory() : this(BuildTempPath())
        {
        }

        public SelfCleaningDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                throw new InvalidOperationException(string.Format("Directory '{0}' already exists.", path));
            }

            DirectoryPath = path;
            RootedDirectoryPath = Path.GetFullPath(path);
        }

        public string DirectoryPath { get; private set; }
        public string RootedDirectoryPath { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            if (!Directory.Exists(DirectoryPath))
            {
                throw new InvalidOperationException(string.Format("Directory '{0}' doesn't exist any longer.", DirectoryPath));
            }

            DirectoryHelper.DeleteDirectory(DirectoryPath);
        }

        #endregion

        protected static string BuildTempPath()
        {
            return Path.Combine(Constants.TemporaryReposPath, Guid.NewGuid().ToString().Substring(0, 8));
        }
    }
}
