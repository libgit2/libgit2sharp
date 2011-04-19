using System;

namespace LibGit2Sharp.Core
{
    internal class ObjectSafeWrapper : IDisposable
    {
        private IntPtr objectPtr;

        public ObjectSafeWrapper(ObjectId id, Repository repo)
        {
            var oid = id.Oid;
            var res = NativeMethods.git_object_lookup(out objectPtr, repo.Handle, ref oid, GitObjectType.Any);
            Ensure.Success(res);
        }

        public IntPtr ObjectPtr
        {
            get { return objectPtr; }
        }
        
        public void Dispose()
        {
            if (objectPtr == IntPtr.Zero)
            {
                return;
            }

            NativeMethods.git_object_close(objectPtr);
            objectPtr = IntPtr.Zero;
        }
    }
}