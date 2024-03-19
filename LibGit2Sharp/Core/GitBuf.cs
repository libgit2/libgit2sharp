using System;

namespace LibGit2Sharp.Core.Handles
{
    internal struct GitBuf : IDisposable
    {
        public IntPtr ptr;
        public UIntPtr asize;
        public UIntPtr size;

        public void Dispose()
        {
            Proxy.git_buf_dispose(this);
        }
    }
}
