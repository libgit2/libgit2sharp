using System;

namespace LibGit2Sharp.Core
{
    public class ObjectSafeWrapper : IDisposable
    {
        private IntPtr _obj;
        public IntPtr Obj
        {
            get { return _obj; }
        }

        public ObjectSafeWrapper(ObjectId id, Repository repo)
        {
            var oid = id.Oid;
            var res = NativeMethods.git_object_lookup(out _obj, repo.Handle, ref oid, GitObjectType.Any);
            Ensure.Success(res);
        }

        public void Dispose()
        {
            if (_obj == IntPtr.Zero) return;
            NativeMethods.git_object_close(_obj);
            _obj = IntPtr.Zero;
        }
    }
}