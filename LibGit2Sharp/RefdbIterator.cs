using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class RefdbIterator
    {
        /// <summary>
        /// 
        /// </summary>
        public abstract bool Next(out string referenceName, out bool isSymbolic, out ObjectId oid, out string symbolic);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public abstract string NextName();

        private IntPtr nativeBackendPointer;

        internal IntPtr GitRefdbIteratorPtr
        {
            get
            {
                if (IntPtr.Zero == nativeBackendPointer)
                {
                    var nativeBackend = new GitRefdbIterator();

                    // The "free" entry point is always provided.
                    nativeBackend.next = ReferenceIteratorEntryPoints.NextCallback;
                    nativeBackend.next_name = ReferenceIteratorEntryPoints.NextNameCallback;
                    nativeBackend.free = ReferenceIteratorEntryPoints.FreeCallback;

                    nativeBackend.GCHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
                    nativeBackendPointer = Marshal.AllocHGlobal(Marshal.SizeOf(nativeBackend));
                    Marshal.StructureToPtr(nativeBackend, nativeBackendPointer, false);
                }

                return nativeBackendPointer;
            }
        }

        private static class ReferenceIteratorEntryPoints
        {
            public static readonly GitRefdbIterator.ref_db_next NextCallback = Next;
            public static readonly GitRefdbIterator.ref_db_next_name NextNameCallback = NextName;
            public static readonly GitRefdbIterator.ref_db_free FreeCallback = FreeIter;
            
            private static bool TryMarshalRefdbIterator(out RefdbIterator refdbiter, IntPtr refDbIterPtr)
            {
                refdbiter = null;

                var intPtr = Marshal.ReadIntPtr(refDbIterPtr, GitRefdbIterator.GCHandleOffset);
                var handle = GCHandle.FromIntPtr(intPtr).Target as RefdbIterator;

                if (handle == null)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Reference, "Cannot retrieve the RefDbIter handle.");
                    return false;
                }

                refdbiter = handle;
                return true;
            }

            public static int Next(out IntPtr referencePtr, IntPtr refDbIterPtr)
            {
                referencePtr = IntPtr.Zero;
                RefdbIterator refIter;

                if(!TryMarshalRefdbIterator(out refIter, refDbIterPtr))
                {
                    return (int)GitErrorCode.Error;
                }

                string refName;
                bool isSymbolic;
                ObjectId oid;
                string symbolic;

                if (!refIter.Next(out refName, out isSymbolic, out oid, out symbolic))
                {
                    return (int)GitErrorCode.IterOver;
                }

                referencePtr = isSymbolic ?
                    Proxy.git_reference__alloc_symbolic(refName, symbolic) :
                    Proxy.git_reference__alloc(refName, oid);

                return (int)GitErrorCode.Ok;
            }

            public static int NextName(out IntPtr refNamePtr, IntPtr refDbIterPtr)
            {
                refNamePtr = IntPtr.Zero;
                RefdbIterator refIter;

                if (!TryMarshalRefdbIterator(out refIter, refDbIterPtr))
                {
                    return (int)GitErrorCode.Error;
                }

                string refName;

                if ((refName = refIter.NextName()) == null)
                {
                    return (int)GitErrorCode.IterOver;
                }

                // Marshal the string to the global heap
                refNamePtr = AllocRefNameOnHeap(refName, refIter);

                return (int)GitErrorCode.Ok;
            }

            private static IntPtr AllocRefNameOnHeap(string refName, RefdbIterator iter)
            {
                IntPtr refNamePtr = StrictUtf8Marshaler.FromManaged(refName);

                IntPtr offset = Marshal.OffsetOf(typeof(GitRefdbIterator), "RefNamePtr");
                Marshal.WriteIntPtr(iter.GitRefdbIteratorPtr, (int) offset, refNamePtr);
                return refNamePtr;
            }

            public static void FreeIter(IntPtr refDbIterPtr)
            {
                RefdbIterator refIter;
                if (!TryMarshalRefdbIterator(out refIter, refDbIterPtr))
                {
                    return;
                }

                IntPtr offset = Marshal.OffsetOf(typeof(GitRefdbIterator), "RefNamePtr");

                IntPtr refNamePtr = Marshal.ReadIntPtr(refIter.GitRefdbIteratorPtr, (int)offset);

                if (refNamePtr != IntPtr.Zero)
                {
                    StrictUtf8Marshaler.Cleanup(refNamePtr);
                    Marshal.WriteIntPtr(refIter.GitRefdbIteratorPtr, (int)offset, IntPtr.Zero);
                }
            }
        }
    }
}
