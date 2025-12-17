using System;

namespace LibGit2Sharp.Core
{
    internal class GitProxyOptionsWrapper : IDisposable
    {
        public GitProxyOptionsWrapper() : this(new GitProxyOptions()) { }

        public GitProxyOptionsWrapper(GitProxyOptions fetchOptions)
        {
            Options = fetchOptions;
        }

        public GitProxyOptions Options { get; private set; }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
                return;

            EncodingMarshaler.Cleanup(Options.Url);
            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
