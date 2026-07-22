using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    /// <summary>
    /// Managed mirror of libgit2's <c>git_buf</c> for APIs that still pass a
    /// sequential-layout class through the runtime marshaller.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="GitBufNative"/> for new or Native AOT-sensitive call
    /// sites. Under Native AOT, class-based sequential marshalling is effectively
    /// [In]-only, so native writes to <c>ptr</c>/<c>size</c> never appear in
    /// managed code.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal class GitBuf : IDisposable
    {
        public IntPtr ptr;
        public UIntPtr asize;
        public UIntPtr size;

        public void Dispose()
        {
            Proxy.git_buf_dispose(this);
        }
    }

    /// <summary>
    /// Blittable <c>git_buf</c> for P/Invoke via <c>ref</c>. Required for Native AOT
    /// so that libgit2 can write <c>ptr</c>/<c>asize</c>/<c>size</c> back to managed
    /// memory (class marshalling does not).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitBufNative
    {
        public IntPtr ptr;
        public UIntPtr asize;
        public UIntPtr size;
    }
}
