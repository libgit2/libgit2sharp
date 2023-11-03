using System;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Git push options wrapper. Disposable wrapper for <see cref="GitPushOptions"/>.
    /// </summary>
    internal class GitPushOptionsWrapper : IDisposable
    {
        public GitPushOptionsWrapper() : this(new GitPushOptions()) { }

        public GitPushOptionsWrapper(GitPushOptions pushOptions)
        {
            this.Options = pushOptions;
        }

        public GitPushOptions Options { get; private set; }

        #region IDisposable
        private bool disposedValue = false; // To detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
                return;

            this.Options.CustomHeaders.Dispose();
            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
