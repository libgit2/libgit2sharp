using System;
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class RawContentStream : UnmanagedMemoryStream
    {
        private readonly GitObjectSafeHandle handle;
        private readonly ICollection<IDisposable> linkedResources;

        internal unsafe RawContentStream(
            GitObjectSafeHandle handle,
            Func<GitObjectSafeHandle, IntPtr> bytePtrProvider,
            Func<GitObjectSafeHandle, long> sizeProvider,
            ICollection<IDisposable> linkedResources = null)
            : base((byte*)Wrap(handle, bytePtrProvider, linkedResources).ToPointer(),
            Wrap(handle, sizeProvider, linkedResources))
        {
            this.handle = handle;
            this.linkedResources = linkedResources;
        }

        private static T Wrap<T>(
            GitObjectSafeHandle handle,
            Func<GitObjectSafeHandle, T> provider,
            IEnumerable<IDisposable> linkedResources)
        {
            T value;

            try
            {
                value = provider(handle);
            }
            catch
            {
                Dispose(handle, linkedResources);
                throw;
            }

            return value;
        }

        private static void Dispose(
            GitObjectSafeHandle handle,
            IEnumerable<IDisposable> linkedResources)
        {
            handle.SafeDispose();

            if (linkedResources == null)
            {
                return;
            }

            foreach (IDisposable linkedResource in linkedResources)
            {
                linkedResource.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Dispose(handle, linkedResources);
        }
    }
}
