using System;
using System.IO;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Base class for all custom managed backends for the libgit2 reference database.
    /// </summary>
    public abstract class RefdbBackend
    {
        /// <summary>
        ///  Requests the repository configured for this backend.
        /// </summary>
        protected abstract Repository Repository
        {
            get;
        }

        /// <summary>
        ///   Requests that the backend provide all optional operations that are supported.
        /// </summary>
        protected abstract RefdbBackendOperations SupportedOperations
        {
            get;
        }

        /// <summary>
        ///  Queries the backend for whether a reference exists.
        /// </summary>
        /// <param name="referenceName">Name of the reference to query</param>
        /// <returns>True if the reference exists in the backend, false otherwise.</returns>
        public abstract bool Exists(string referenceName);

        /// <summary>
        ///  Queries the backend for the given reference
        /// </summary>
        /// <param name="referenceName">Name of the reference to query</param>
        /// <param name="type">Type of the reference returned</param>
        /// <param name="oid">Object ID of the reference returned if type is a direct reference</param>
        /// <param name="symbolic">Target of the reference returned if type is a symbolic reference</param>
        /// <returns>True if the reference exists, false otherwise</returns>
        public abstract bool Lookup(string referenceName, out ReferenceType type, out ObjectId oid, out string symbolic);

        /// <summary>
        ///  Iterates the references in this backend.
        /// </summary>
        /// <param name="types">The types to enumerate</param>
        /// <param name="callback">The callback to execute for each reference</param>
        /// <returns>The return code from the callback</returns>
        public abstract int Foreach(ReferenceType types, ForeachCallback callback);

        /// <summary>
        ///  Iterates the references in this backend.
        /// </summary>
        /// <param name="types">The types to enumerate</param>
        /// <param name="glob">The glob pattern reference names must match</param>
        /// <param name="callback">The callback to execute for each reference</param>
        /// <returns>The return code from the callback</returns>
        public abstract int ForeachGlob(ReferenceType types, string glob, ForeachCallback callback);

        /// <summary>
        ///  The signature of the callback method provided to the reference iterators.
        /// </summary>
        /// <param name="referenceName">The name of the reference in the backend</param>
        /// <returns>0 if enumeration should continue, any other value on error</returns>
        public delegate int ForeachCallback(string referenceName);
        
        /// <summary>
        ///  Write the given reference to the backend.
        /// </summary>
        /// <param name="reference">The reference to write</param>
        public abstract void Write(Reference reference);

        /// <summary>
        ///  Delete the given reference from the backend.
        /// </summary>
        /// <param name="reference">The reference to delete</param>
        public abstract void Delete(Reference reference);

        /// <summary>
        ///  Compress the backend in an implementation-specific way.
        /// </summary>
        public abstract void Compress();

        /// <summary>
        ///  Free any data associated with this backend.
        /// </summary>
        public abstract void Free();

        private IntPtr nativeBackendPointer;

        internal IntPtr GitRefdbBackendPointer
        {
            get
            {
                if (IntPtr.Zero == nativeBackendPointer)
                {
                    var nativeBackend = new GitRefdbBackend();
                    nativeBackend.Version = 1;

                    // The "free" entry point is always provided.
                    nativeBackend.Exists = BackendEntryPoints.ExistsCallback;
                    nativeBackend.Lookup = BackendEntryPoints.LookupCallback;
                    nativeBackend.Foreach = BackendEntryPoints.ForeachCallback;
                    nativeBackend.Write = BackendEntryPoints.WriteCallback;
                    nativeBackend.Delete = BackendEntryPoints.DeleteCallback;
                    nativeBackend.Free = BackendEntryPoints.FreeCallback;

                    var supportedOperations = this.SupportedOperations;

                    if ((supportedOperations & RefdbBackendOperations.ForeachGlob) != 0)
                    {
                        nativeBackend.ForeachGlob = BackendEntryPoints.ForeachGlobCallback;
                    }

                    if ((supportedOperations & RefdbBackendOperations.Compress) != 0)
                    {
                        nativeBackend.Compress = BackendEntryPoints.CompressCallback;
                    }

                    nativeBackend.GCHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
                    nativeBackendPointer = Marshal.AllocHGlobal(Marshal.SizeOf(nativeBackend));
                    Marshal.StructureToPtr(nativeBackend, nativeBackendPointer, false);
                }

                return nativeBackendPointer;
            }
        }

        private static class BackendEntryPoints
        {
            // Because our GitOdbBackend structure exists on the managed heap only for a short time (to be marshaled
            // to native memory with StructureToPtr), we need to bind to static delegates. If at construction time
            // we were to bind to the methods directly, that's the same as newing up a fresh delegate every time.
            // Those delegates won't be rooted in the object graph and can be collected as soon as StructureToPtr finishes.
            public static readonly GitRefdbBackend.exists_callback ExistsCallback = Exists;
            public static readonly GitRefdbBackend.lookup_callback LookupCallback = Lookup;
            public static readonly GitRefdbBackend.foreach_callback ForeachCallback = Foreach;
            public static readonly GitRefdbBackend.foreach_glob_callback ForeachGlobCallback = ForeachGlob;
            public static readonly GitRefdbBackend.write_callback WriteCallback = Write;
            public static readonly GitRefdbBackend.delete_callback DeleteCallback = Delete;
            public static readonly GitRefdbBackend.compress_callback CompressCallback = Compress;
            public static readonly GitRefdbBackend.free_callback FreeCallback = Free;

            private static int Exists(
                out IntPtr exists,
                IntPtr backend,
                IntPtr namePtr)
            {
                RefdbBackend refdbBackend = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitRefdbBackend.GCHandleOffset)).Target as RefdbBackend;

                exists = (IntPtr)0;

                if (refdbBackend != null)
                {
                    try
                    {
                        string referenceName = Utf8Marshaler.FromNative(namePtr);

                        if (refdbBackend.Exists(referenceName))
                            exists = (IntPtr)1;

                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static int Lookup(
                out IntPtr referencePtr,
                IntPtr backend,
                IntPtr namePtr)
            {
                RefdbBackend refdbBackend = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitRefdbBackend.GCHandleOffset)).Target as RefdbBackend;
                ReferenceDatabaseSafeHandle refdbHandle = refdbBackend.Repository.ReferenceDatabase.Handle;

                ReferenceType type;
                ObjectId oid;
                string symbolic;
                referencePtr = (IntPtr)0;

                if (refdbBackend != null)
                {
                    try
                    {
                        string referenceName = Utf8Marshaler.FromNative(namePtr);

                        if (!refdbBackend.Lookup(referenceName, out type, out oid, out symbolic))
                            return (int)GitErrorCode.NotFound;

                        if (type == ReferenceType.Symbolic)
                        {
                            referencePtr = Proxy.git_reference__alloc(refdbHandle, referenceName, null, symbolic);
                        }
                        else
                        {
                            referencePtr = Proxy.git_reference__alloc(refdbHandle, referenceName, oid, null);
                        }

                        if (referencePtr != (IntPtr)0)
                            return 0;
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static int Foreach(
                IntPtr backend,
                UIntPtr list_flags,
                GitRefdbBackend.foreach_callback_callback callback,
                IntPtr data)
            {
                RefdbBackend refdbBackend = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitRefdbBackend.GCHandleOffset)).Target as RefdbBackend;

                if (refdbBackend != null)
                {
                    try
                    {
                        return refdbBackend.Foreach((ReferenceType)list_flags, new ForeachState(callback, data).ManagedCallback);
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static int ForeachGlob(
                IntPtr backend,
                IntPtr globPtr,
                UIntPtr list_flags,
                GitRefdbBackend.foreach_callback_callback callback,
                IntPtr data)
            {
                RefdbBackend refdbBackend = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitRefdbBackend.GCHandleOffset)).Target as RefdbBackend;

                if (refdbBackend != null)
                {
                    try
                    {
                        string glob = Utf8Marshaler.FromNative(globPtr);
                        return refdbBackend.ForeachGlob((ReferenceType)list_flags, glob, new ForeachState(callback, data).ManagedCallback);
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static int Write(
                IntPtr backend,
                IntPtr referencePtr)
            {
                RefdbBackend refdbBackend = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitRefdbBackend.GCHandleOffset)).Target as RefdbBackend;
                ReferenceSafeHandle referenceHandle = new ReferenceSafeHandle(referencePtr, false);
                Reference reference = Reference.BuildFromPtr<Reference>(referenceHandle, refdbBackend.Repository);

                if (refdbBackend != null)
                {
                    try
                    {
                        refdbBackend.Write(reference);
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static int Delete(
                IntPtr backend,
                IntPtr referencePtr)
            {
                RefdbBackend refdbBackend = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitRefdbBackend.GCHandleOffset)).Target as RefdbBackend;
                ReferenceSafeHandle referenceHandle = new ReferenceSafeHandle(referencePtr, false);
                Reference reference = Reference.BuildFromPtr<Reference>(referenceHandle, refdbBackend.Repository);

                if (refdbBackend != null)
                {
                    try
                    {
                        refdbBackend.Delete(reference);
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static int Compress(IntPtr backend)
            {
                RefdbBackend refdbBackend = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitRefdbBackend.GCHandleOffset)).Target as RefdbBackend;

                if (refdbBackend != null)
                {
                    try
                    {
                        refdbBackend.Compress();
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static void Free(IntPtr backend)
            {
                RefdbBackend refdbBackend = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitRefdbBackend.GCHandleOffset)).Target as RefdbBackend;
                refdbBackend.Free();
            }

            private class ForeachState
            {
                public ForeachState(GitRefdbBackend.foreach_callback_callback cb, IntPtr data)
                {
                    this.cb = cb;
                    this.data = data;
                    this.ManagedCallback = CallbackMethod;
                }

                private int CallbackMethod(string name)
                {
                    IntPtr namePtr = Utf8Marshaler.FromManaged(name);

                    return cb(namePtr, data);
                }

                public readonly ForeachCallback ManagedCallback;

                private readonly GitRefdbBackend.foreach_callback_callback cb;
                private readonly IntPtr data;
            }
        }

        /// <summary>
        ///   Flags used by subclasses of RefdbBackend to indicate which operations they support.
        /// </summary>
        [Flags]
        protected enum RefdbBackendOperations
        {
            /// <summary>
            ///   This RefdbBackend declares that it supports the Compress method.
            /// </summary>
            Compress = 1,

            /// <summary>
            ///   This RefdbBackend declares that it supports the ForeachGlob method.
            /// </summary>
            ForeachGlob = 2,
        }
    }
}
