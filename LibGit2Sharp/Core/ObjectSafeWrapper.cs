using System;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class ObjectSafeWrapper : IDisposable
    {
        private readonly GitObjectSafeHandle objectPtr;

        public ObjectSafeWrapper(ObjectId id, RepositorySafeHandle handle)
        {
            Ensure.ArgumentNotNull(id, "id");
            Ensure.ArgumentNotNull(handle, "handle");

            objectPtr = Proxy.git_object_lookup(handle, id, GitObjectType.Any);
        }

        public GitObjectSafeHandle ObjectPtr
        {
            get { return objectPtr; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            objectPtr.SafeDispose();
        }

        ~ObjectSafeWrapper()
        {
            Dispose(false);
        }
    }
}
