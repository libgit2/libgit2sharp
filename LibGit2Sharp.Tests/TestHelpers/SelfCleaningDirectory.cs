using System;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class SelfCleaningDirectory : IDisposable
    {
        private readonly string path;

        public SelfCleaningDirectory(string path)
        {
            this.path = path;
            DirectoryHelper.DeleteIfExists(path);
        }

        #region IDisposable Members

        public void Dispose()
        {
            DirectoryHelper.DeleteIfExists(path);
        }

        #endregion
    }
}