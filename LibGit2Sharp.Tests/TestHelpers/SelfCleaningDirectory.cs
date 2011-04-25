using System;
using System.IO;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class SelfCleaningDirectory : IDisposable
    {
        private readonly string path;

        public SelfCleaningDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                throw new InvalidOperationException("Directory '{0}' already exists.");
            }

            this.path = path;
        }

        protected string DirectoryPath
        {
            get { return path; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (!Directory.Exists(path))
            {
                throw new InvalidOperationException("Directory '{0}' doesn't exist any longer.");
            }

            DirectoryHelper.DeleteDirectory(path);
        }

        #endregion
    }
}