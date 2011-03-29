using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp
{
    public class GitObject : IDisposable
    {
        public static GitObjectTypeMap TypeToTypeMap =
            new GitObjectTypeMap
                {
                    {typeof (Commit), GitObjectType.Commit},
                    {typeof (Tree), GitObjectType.Tree},
                    {typeof (Blob), GitObjectType.Blob},
                    {typeof (Tag), GitObjectType.Tag},
                    {typeof (GitObject), GitObjectType.Any},
                };

        protected IntPtr Obj = IntPtr.Zero;
        private bool disposed;
        private GitOid? oid;

        private string sha;

        protected GitObject(IntPtr obj, GitOid? oid = null)
        {
            this.oid = oid;
            Obj = obj;
        }

        public GitOid Oid
        {
            get
            {
                if (!oid.HasValue)
                {
                    var ptr = NativeMethods.git_object_id(Obj);
                    oid = (GitOid) Marshal.PtrToStructure(ptr, typeof (GitOid));
                }
                return oid.Value;
            }
        }

        public string Sha
        {
            get { return sha ?? (sha = Oid.ToSha()); }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        internal static GitObject CreateFromPtr(IntPtr obj, GitOid oid, Repository repo)
        {
            var type = NativeMethods.git_object_type(obj);
            switch (type)
            {
                case GitObjectType.Commit:
                    return new Commit(obj, oid);
                case GitObjectType.Tree:
                    return new Tree(obj, oid);
                case GitObjectType.Tag:
                    return new Tag(obj, oid);
                case GitObjectType.Blob:
                    return new Blob(obj, oid);
                default:
                    return new GitObject(obj, oid);
            }
        }

        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                NativeMethods.git_object_close(Obj);

                // Note disposing has been done.
                disposed = true;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (GitObject)) return false;
            return Equals((GitObject) obj);
        }

        public bool Equals(GitObject other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Oid.Equals(Oid);
        }

        ~GitObject()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        public override int GetHashCode()
        {
            return Oid.GetHashCode();
        }
    }
}