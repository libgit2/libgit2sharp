using System;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class ObjectSafeWrapper : IDisposable
    {
        private readonly GitObjectSafeHandle objectPtr;

        // TODO: this constructor should be dropped
        public ObjectSafeWrapper(ObjectId id, Repository repo)
            : this(id, repo.Handle)
        { }

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
