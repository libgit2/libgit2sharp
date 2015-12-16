using System;

namespace LibGit2Sharp.Core.Handles
{
    internal unsafe class GitReferenceHandle : IDisposable
    {
        git_reference* ptr;

        internal GitReferenceHandle(git_reference* refPtr)
        {
            this.ptr = refPtr;
        }

        internal GitReferenceHandle(IntPtr refPtr)
        {
            this.ptr = (git_reference*) refPtr.ToPointer();
        }

        ~GitReferenceHandle()
        {
            Dispose();
        }

        internal git_reference* ToPointer()
        {
            return ptr;
        }

        internal bool IsNull
        {
            get
            {
                return ptr == null;
            }
        }

        public void Dispose()
        {
            NativeMethods.git_reference_free(ptr);
            ptr = null;
        }
    }
}
