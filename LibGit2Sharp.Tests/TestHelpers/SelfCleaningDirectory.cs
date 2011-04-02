using System;
using System.IO;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class SelfCleaningDirectory : IDisposable
    {
        private readonly string _path;

        public SelfCleaningDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                throw new InvalidOperationException("Directory '{0}' already exists.");
            }
            
            _path = path;
        }

        protected string DirectoryPath { get { return _path; } }

        #region IDisposable Members

        public void Dispose()
        {
            if (!Directory.Exists(_path))
            {
                throw new InvalidOperationException("Directory '{0}' doesn't exist any longer.");
            }

            DirectoryHelper.DeleteDirectory(_path);
        }

        #endregion
    }
}