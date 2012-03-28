using System;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class ObjectSafeWrapper : IDisposable
    {
        private readonly GitObjectSafeHandle objectPtr;

        public ObjectSafeWrapper(ObjectId id, Repository repo)
        {
            Ensure.ArgumentNotNull(id, "id");
            Ensure.ArgumentNotNull(repo, "repo");

            GitOid oid = id.Oid;
            int res = NativeMethods.git_object_lookup(out objectPtr, repo.Handle, ref oid, GitObjectType.Any);
            Ensure.Success(res);
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
