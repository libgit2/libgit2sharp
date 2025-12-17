using System;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class ObjectSafeWrapper : IDisposable
    {
        private readonly ObjectHandle objectPtr;

        public unsafe ObjectSafeWrapper(ObjectId id, RepositoryHandle handle, bool allowNullObjectId = false, bool throwIfMissing = false)
        {
            Ensure.ArgumentNotNull(handle, "handle");

            if (allowNullObjectId && id == null)
            {
                objectPtr = new ObjectHandle(null, false);
            }
            else
            {
                Ensure.ArgumentNotNull(id, "id");
                objectPtr = Proxy.git_object_lookup(handle, id, GitObjectType.Any);
            }

            if (objectPtr == null && throwIfMissing)
            {
                throw new NotFoundException($"No valid git object identified by '{id}' exists in the repository.");
            }
        }

        public ObjectHandle ObjectPtr => objectPtr;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            objectPtr.SafeDispose();
        }
    }
}
