using System;
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class RawContentStream : UnmanagedMemoryStream
    {
        private readonly ObjectHandle handle;
        private readonly ICollection<IDisposable> linkedResources;

        internal unsafe RawContentStream(
            ObjectHandle handle,
            Func<ObjectHandle, IntPtr> bytePtrProvider,
            Func<ObjectHandle, long> sizeProvider,
            ICollection<IDisposable> linkedResources = null)
            : base((byte*)Wrap(handle, bytePtrProvider, linkedResources).ToPointer(),
            Wrap(handle, sizeProvider, linkedResources))
        {
            this.handle = handle;
            this.linkedResources = linkedResources;
        }

        private static T Wrap<T>(
            ObjectHandle handle,
            Func<ObjectHandle, T> provider,
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
            ObjectHandle handle,
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
