using System;

namespace LibGit2Sharp.Core.Handles
{
    internal unsafe abstract class Libgit2Object : IDisposable
    {
        protected void* ptr;

        internal void* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        internal unsafe Libgit2Object(void* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;
        }

        internal unsafe Libgit2Object(IntPtr ptr, bool owned)
        {
            this.ptr = ptr.ToPointer();
            this.owned = owned;
        }

        ~Libgit2Object()
        {
            Dispose(false);
        }

        internal bool IsNull
        {
            get
            {
                return ptr == null;
            }
        }

        internal IntPtr AsIntPtr()
        {
            return new IntPtr(ptr);
        }

        public abstract void Free();

        void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (owned)
                {
                    Free();
                }

                ptr = null;
            }

            disposed = true;
        }

            public void Dispose()
        {
            Dispose(true);
        }
    }
}

