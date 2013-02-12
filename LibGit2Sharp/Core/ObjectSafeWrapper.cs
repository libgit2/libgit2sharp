﻿using System;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class ObjectSafeWrapper : IDisposable
    {
        private readonly GitObjectSafeHandle objectPtr;

        public ObjectSafeWrapper(ObjectId id, RepositorySafeHandle handle, bool allowNullObjectId = false)
        {
            Ensure.ArgumentNotNull(handle, "handle");

            if (allowNullObjectId && id == null)
            {
                objectPtr = new NullGitObjectSafeHandle();
            }
            else
            {
                Ensure.ArgumentNotNull(id, "id");
                objectPtr = Proxy.git_object_lookup(handle, id, GitObjectType.Any);
            }
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
    }
}
