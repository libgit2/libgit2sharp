using System;
using System.IO;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class RawContentStream : UnmanagedMemoryStream
    {
        private readonly ObjectSafeWrapper wrapper;

        internal RawContentStream(ObjectId id, RepositorySafeHandle repo,
            Func<GitObjectSafeHandle, IntPtr> bytePtrProvider, long length)
            : this(new ObjectSafeWrapper(id, repo), bytePtrProvider, length)
        {
        }

        unsafe RawContentStream(ObjectSafeWrapper wrapper,
            Func<GitObjectSafeHandle, IntPtr> bytePtrProvider, long length)
            : base((byte*)bytePtrProvider(wrapper.ObjectPtr).ToPointer(), length)
        {
            this.wrapper = wrapper;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            wrapper.SafeDispose();
        }
    }
}
