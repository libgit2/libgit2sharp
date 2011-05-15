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
                throw new InvalidOperationException("Directory '{0}' already exists.");
            }

            DirectoryPath = path;
        }

        public string DirectoryPath { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            if (!Directory.Exists(DirectoryPath))
            {
                throw new InvalidOperationException("Directory '{0}' doesn't exist any longer.");
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