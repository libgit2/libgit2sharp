using System;
using System.Globalization;
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
        /// The optional operations this backed supports
        /// </summary>
        protected abstract RefdbBackendOperations SupportedOperations
        {
            get;
        }

        /// <summary>
        /// Queries the backend for whether a reference exists.
        /// </summary>
        /// <param name="referenceName">Name of the reference to query</param>
        /// <returns>True if the reference exists in the backend, false otherwise.</returns>
        public abstract bool Exists(string referenceName);

        /// <summary>
        /// Queries the backend for the given reference
        /// </summary>
        /// <param name="referenceName">Name of the reference to query</param>
        /// <param name="isSymbolic"> True if the returned reference is a symbolic reference, 
        /// False if the returned reference is a direct reference. </param>
        /// <param name="oid">Object ID of the returned reference. Valued when <paramref name="isSymbolic"/> is false.</param>
        /// <param name="symbolic">Target of the returned reference. Valued when <paramref name="isSymbolic"/> is false</param>
        /// <returns>True if the reference exists, false otherwise</returns>
        public abstract bool Lookup(string referenceName, out bool isSymbolic, out ObjectId oid, out string symbolic);

        /// <summary>
        /// Generate the ref iterator.
        /// </summary>
        /// <param name="glob"></param>
        /// <returns></returns>
        public abstract RefdbIterator GenerateRefIterator(string glob);

        /// <summary>
        /// Write the given direct reference to the backend.
        /// </summary>
        /// <param name="referenceCanonicalName">The reference to write</param>
        /// <param name="target">The <see cref="ObjectId"/> of the target <see cref="GitObject"/>.</param>
        /// <param name="force"></param>
        public abstract void WriteDirectReference(string referenceCanonicalName, ObjectId target, bool force);

        /// <summary>
        ///  Write the given symbolic reference to the backend.
        /// </summary>
        /// <param name="referenceCanonicalName">The reference to write</param>
        /// <param name="targetCanonicalName">The target of the symbolic reference</param>
        /// <param name="force"></param>
        public abstract void WriteSymbolicReference(string referenceCanonicalName, string targetCanonicalName, bool force);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="referenceName"></param>
        /// <param name="newReferenceName"></param>
        /// <param name="force"></param>
        /// <param name="isSymbolic"></param>
        /// <param name="oid"></param>
        /// <param name="symbolic"></param>
        public abstract void RenameReference(string referenceName, string newReferenceName, bool force,
                                             out bool isSymbolic, out ObjectId oid, out string symbolic);

        /// <summary>
        ///  Delete the given reference from the backend.
        /// </summary>
        /// <param name="referenceCanonicalName">The reference to delete</param>
        public abstract void Delete(string referenceCanonicalName);

        /// <summary>
        ///  Compress the backend in an implementation-specific way.
        /// </summary>
        public abstract void Compress();

        /// <summary>
        ///  Free any data associated with this backend.
        /// </summary>
        public abstract void Free();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="refName"></param>
        /// <returns></returns>
        public abstract bool HasReflog(string refName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="refName"></param>
        public abstract void EnsureReflog(string refName);

        /// <summary>
        /// 
        /// </summary>
        public abstract void ReadReflog();

        /// <summary>
        /// 
        /// </summary>
        public abstract void WriteReflog();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oldName"></param>
        /// <param name="newName"></param>
        public abstract void RenameReflog(string oldName, string newName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="refName"></param>
        /// <returns></returns>
        public abstract void LockReference(string refName);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="refname"></param>
        /// <returns></returns>
        public abstract void UnlockReference(string refname);

        private IntPtr nativeBackendPointer;

        internal IntPtr GitRefdbBackendPointer
        {
            get
            {
                if (IntPtr.Zero == nativeBackendPointer)
                {
                    var nativeBackend = new GitRefDbBackend();
                    nativeBackend.Version = 1;

                    // The "free" entry point is always provided.
                    nativeBackend.Exists = BackendEntryPoints.ExistsCallback;
                    nativeBackend.Lookup = BackendEntryPoints.LookupCallback;
                    nativeBackend.Iter = BackendEntryPoints.IterCallback;
                    nativeBackend.Write = BackendEntryPoints.WriteCallback;
                    nativeBackend.Rename = BackendEntryPoints.RenameCallback;
                    nativeBackend.Delete = BackendEntryPoints.DeleteCallback;
                    nativeBackend.Compress = BackendEntryPoints.CompressCallback;
                    nativeBackend.HasLog = BackendEntryPoints.HasLogCallback;
                    nativeBackend.EnsureLog = BackendEntryPoints.EnsureLogCallback;
                    nativeBackend.FreeBackend = BackendEntryPoints.FreeCallback;
                    nativeBackend.ReflogWrite = BackendEntryPoints.ReflogWriteCallback;
                    nativeBackend.ReflogRead = BackendEntryPoints.ReflogReadCallback;
                    nativeBackend.ReflogRename = BackendEntryPoints.ReflogRenameCallback;
                    nativeBackend.ReflogDelete = BackendEntryPoints.ReflogDeleteCallback;
                    nativeBackend.RefLock = BackendEntryPoints.RefLockCallback;
                    nativeBackend.RefUnlock = BackendEntryPoints.RefUnlockCallback;

                    var supportedOperations = this.SupportedOperations;

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
            public static readonly GitRefDbBackend.exists_callback ExistsCallback = Exists;
            public static readonly GitRefDbBackend.lookup_callback LookupCallback = Lookup;

            public static readonly GitRefDbBackend.iterator_callback IterCallback = GetIterator;

            public static readonly GitRefDbBackend.write_callback WriteCallback = Write;
            public static readonly GitRefDbBackend.rename_callback RenameCallback = Rename;
            public static readonly GitRefDbBackend.delete_callback DeleteCallback = Delete;

            public static readonly GitRefDbBackend.compress_callback CompressCallback = Compress;
            public static readonly GitRefDbBackend.free_callback FreeCallback = Free;

            public static readonly GitRefDbBackend.has_log_callback HasLogCallback = HasLog;
            public static readonly GitRefDbBackend.ensure_log_callback EnsureLogCallback = EnsureLog;

            public static readonly GitRefDbBackend.reflog_write_callback ReflogWriteCallback = ReflogWrite;
            public static readonly GitRefDbBackend.reflog_read_callback ReflogReadCallback = ReflogRead;
            public static readonly GitRefDbBackend.reflog_rename_callback ReflogRenameCallback = ReflogRename;
            public static readonly GitRefDbBackend.reflog_delete_callback ReflogDeleteCallback = ReflogDelete;

            public static readonly GitRefDbBackend.ref_lock_callback RefLockCallback = LockRef;
            public static readonly GitRefDbBackend.ref_unlock_callback RefUnlockCallback = UnlockRef;

            private static RefdbBackend MarshalRefdbBackend(IntPtr backend)
            {
                var intPtr = Marshal.ReadIntPtr(backend, GitRefDbBackend.GCHandleOffset);
                var handle = GCHandle.FromIntPtr(intPtr).Target as RefdbBackend;

                if (handle == null)
                {
                    throw new Exception("Cannot retrieve the RefdbBackend handle.");
                }

                return handle;
            }

            private static bool TryMarshalRefdbBackend(out RefdbBackend refdbBackend, IntPtr backend)
            {
                refdbBackend = null;

                var intPtr = Marshal.ReadIntPtr(backend, GitRefDbBackend.GCHandleOffset);
                var handle = GCHandle.FromIntPtr(intPtr).Target as RefdbBackend;

                if (handle == null)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Reference, "Cannot retrieve the RefdbBackend handle.");
                    return false;
                }

                refdbBackend = handle;
                return true;
            }

            private static int ErrorMarshalingRefDbBacked()
            {
                Proxy.giterr_set_str(GitErrorCategory.Reference, "Cannot retrieve the RefdbBackend handle.");
                return (int)GitErrorCode.Error;
            }

            private static GitErrorCode Exists(
                out bool exists,
                IntPtr backend,
                IntPtr refNamePtr)
            {
                GitErrorCode res;

                try
                {
                    RefdbBackend refdbBackend = MarshalRefdbBackend(backend);
                    string refName = LaxUtf8Marshaler.FromNative(refNamePtr);

                    exists = refdbBackend.Exists(refName);

                    res = GitErrorCode.Ok;
                }
                catch (Exception ex)
                {
                    exists = false;

                    Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    res = GitErrorCode.Error;
                }

                return res;
            }

            private static GitErrorCode Lookup(
                out IntPtr referencePtr,
                IntPtr backend,
                IntPtr refNamePtr)
            {
                referencePtr = IntPtr.Zero;
                GitErrorCode res;
                try
                {
                    RefdbBackend refdbBackend = MarshalRefdbBackend(backend);

                    string refName = LaxUtf8Marshaler.FromNative(refNamePtr);

                    bool isSymbolic;
                    ObjectId oid;
                    string symbolic;

                    // REVIEW: should .Lookup method throw or return false on not found...
                    if (refdbBackend.Lookup(refName, out isSymbolic, out oid, out symbolic))
                    {
                        referencePtr = AllocNativeRef(refName, isSymbolic, oid, symbolic);
                        res = GitErrorCode.Ok;
                    }
                    else
                    {
                        res = GitErrorCode.NotFound;
                    }
                }
                catch (Exception ex)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    res = GitErrorCode.Error;
                }

                return res;
            }

            private static GitErrorCode GetIterator(
                out IntPtr iterPtr,
                IntPtr backend,
                IntPtr globPtr)
            {
                iterPtr = IntPtr.Zero;
                GitErrorCode res;

                try
                {
                    RefdbBackend refdbBackend = MarshalRefdbBackend(backend);
                    string glob = LaxUtf8Marshaler.FromNative(globPtr);

                    RefdbIterator refIter = refdbBackend.GenerateRefIterator(glob);
                    iterPtr = refIter.GitRefdbIteratorPtr;

                    res = GitErrorCode.Ok;
                }
                catch (Exception ex)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    res = GitErrorCode.Error;
                }

                return res;
            }

            private static GitErrorCode Write(
                IntPtr backend,
                IntPtr referencePtr,
                bool force,
                IntPtr who,
                IntPtr messagePtr,
                IntPtr oidPtr,
                IntPtr oldTargetPtr)
            {
                GitErrorCode res;

                try
                {
                    RefdbBackend refdbBackend = MarshalRefdbBackend(backend);

                    var referenceHandle = new NotOwnedReferenceSafeHandle(referencePtr);
                    string name = Proxy.git_reference_name(referenceHandle);
                    GitReferenceType type = Proxy.git_reference_type(referenceHandle);

                    // TODO: Marshal this correctly
                    if (oidPtr != IntPtr.Zero)
                    {
                        GitOid oid = new GitOid();
                        Marshal.Copy(oidPtr, oid.Id, 0, 20);
                    }

                    string message = LaxUtf8Marshaler.FromNative(messagePtr);
                    string oldTarget = LaxUtf8Marshaler.FromNative(oldTargetPtr);

                    switch (type)
                    {
                        case GitReferenceType.Oid:
                            ObjectId targetOid = Proxy.git_reference_target(referenceHandle);
                            refdbBackend.WriteDirectReference(name, targetOid, force);
                            break;

                        case GitReferenceType.Symbolic:
                            string targetIdentifier = Proxy.git_reference_symbolic_target(referenceHandle);
                            refdbBackend.WriteSymbolicReference(name, targetIdentifier, force);
                            break;

                        default:
                            throw new LibGit2SharpException(
                                String.Format(CultureInfo.InvariantCulture,
                                    "Unable to build a new reference from a type '{0}'.", type));
                    }

                    res = GitErrorCode.Ok;
                }
                catch (NameConflictException ex)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    res = GitErrorCode.Exists;
                }
                catch (Exception ex)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    res = GitErrorCode.Error;
                }

                return res;
            }

            private static GitErrorCode Rename(
                out IntPtr reference,
                IntPtr backend,
                IntPtr oldNamePtr,
                IntPtr newNamePtr,
                bool force,
                IntPtr who,
                IntPtr messagePtr)
            {
                GitErrorCode res;

                try
                {
                    RefdbBackend refdbBackend = MarshalRefdbBackend(backend);

                    string oldName = LaxUtf8Marshaler.FromNative(oldNamePtr);
                    string newName = LaxUtf8Marshaler.FromNative(newNamePtr);

                    bool isSymbolic;
                    ObjectId oid;
                    string symbolic;

                    // TODO: verify that old / new name is not null
                    refdbBackend.RenameReference(oldName, newName, force,
                                                 out isSymbolic, out oid, out symbolic);

                    reference = AllocNativeRef(newName, isSymbolic, oid, symbolic);
                    res = GitErrorCode.Ok;
                }
                catch (NameConflictException ex)
                {
                    reference = IntPtr.Zero;
                    Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    res = GitErrorCode.Exists;
                }
                catch (Exception ex)
                {
                    reference = IntPtr.Zero;
                    Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    res = GitErrorCode.Error;
                }

                return res;
            }

            private static GitErrorCode Delete(
                IntPtr backend,
                IntPtr refNamePtr,
                IntPtr oldId,
                IntPtr oldTargetNamePtr)
            {
                GitErrorCode res;

                try
                {
                    RefdbBackend refdbBackend = MarshalRefdbBackend(backend);
                    string refName = LaxUtf8Marshaler.FromNative(refNamePtr);

                    refdbBackend.Delete(refName);

                    res = GitErrorCode.Ok;
                }
                catch (Exception ex)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    res = GitErrorCode.Error;
                }

                return res;
            }

            private static GitErrorCode Compress(IntPtr backend)
            {
                GitErrorCode res;

                try
                {
                    RefdbBackend refdbBackend = MarshalRefdbBackend(backend);
                    refdbBackend.Compress();

                    res = GitErrorCode.Ok;
                }
                catch (Exception ex)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    res = GitErrorCode.Error;
                }

                return res;
            }

            private static void Free(IntPtr backend)
            {
                RefdbBackend refdbBackend;
                if (!TryMarshalRefdbBackend(out refdbBackend, backend))
                {
                    return;
                }

                refdbBackend.Free();
            }

            private static GitErrorCode ReflogRead(out IntPtr reflogPtr, IntPtr backendPtr, IntPtr refNamePtr)
            {
                reflogPtr = IntPtr.Zero;
                Proxy.giterr_set_str(GitErrorCategory.Reference, "Not implemented");
                return GitErrorCode.Error;
            }

            public static GitErrorCode ReflogWrite(
                IntPtr backend, // git_refdb_backend *
                IntPtr git_reflog // git_reflog *
                )
            {
                Proxy.giterr_set_str(GitErrorCategory.Reference, "Not implemented");
                return GitErrorCode.Error;
            }

            public static GitErrorCode ReflogRename(
                IntPtr backend, // git_refdb_backend
                IntPtr oldNamePtr, // const char *
                IntPtr newNamePtr // const char *
                )
            {
                Proxy.giterr_set_str(GitErrorCategory.Reference, "Not implemented");
                return GitErrorCode.Error;
            }

            public static GitErrorCode ReflogDelete(
                IntPtr backend, // git_refdb_backend
                IntPtr namePtr // const char *
                )
            {
                Proxy.giterr_set_str(GitErrorCategory.Reference, "Not implemented");
                return GitErrorCode.Error;
            }

            public static GitErrorCode HasLog(
                IntPtr backend, // git_refdb_backend *
                IntPtr refNamePtr // const char *
                )
            {
                Proxy.giterr_set_str(GitErrorCategory.Reference, "Not implemented");
                return GitErrorCode.Error;
            }

            public static GitErrorCode EnsureLog(
                IntPtr backend, // git_refdb_backend *
                IntPtr refNamePtr // const char *
                )
            {
                Proxy.giterr_set_str(GitErrorCategory.Reference, "Not implemented");
                return GitErrorCode.Error;
            }

            public static GitErrorCode LockRef(
                IntPtr payload, // void **
                IntPtr backend, // git_refdb_backend
                IntPtr namePtr // const char *
                )
            {
                GitErrorCode res;

                try
                {
                    RefdbBackend refdbBackend = MarshalRefdbBackend(backend);
                    string refName = LaxUtf8Marshaler.FromNative(namePtr);
                    refdbBackend.LockReference(refName);

                    res = GitErrorCode.Ok;
                }
                catch (Exception ex)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    res = GitErrorCode.Error;
                }

                return res;
            }

            public static GitErrorCode UnlockRef(
                IntPtr backend, // git_refdb_backend
                IntPtr payload,
                [MarshalAs(UnmanagedType.Bool)] bool force,
                [MarshalAs(UnmanagedType.Bool)] bool update_reflog,
                IntPtr referencePtr, // const git_reference *
                IntPtr who, // const git_signature *
                IntPtr messagePtr // const char *
                )
            {
                GitErrorCode res;

                try
                {
                    RefdbBackend refdbBackend = MarshalRefdbBackend(backend);

                    var referenceHandle = new NotOwnedReferenceSafeHandle(referencePtr);
                    string refName = Proxy.git_reference_name(referenceHandle);
                    GitReferenceType type = Proxy.git_reference_type(referenceHandle);

                    refdbBackend.UnlockReference(refName);

                    res = GitErrorCode.Ok;
                }
                catch (Exception ex)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    res = GitErrorCode.Error;
                }

                return res;
            }

            private static IntPtr AllocNativeRef(string refName, bool isSymbolic, ObjectId oid, string symbolic)
            {
                return isSymbolic ?
                    Proxy.git_reference__alloc_symbolic(refName, symbolic) :
                    Proxy.git_reference__alloc(refName, oid);
            }
        }

        /// <summary>
        ///   Flags used by subclasses of RefdbBackend to indicate which operations they support.
        /// </summary>
        [Flags]
        public enum RefdbBackendOperations
        {
            /// <summary>
            ///   This RefdbBackend declares that it supports the Compress method.
            /// </summary>
            Compress = 1,

            /// <summary>
            /// The RefdbBackend declares that it supports Reflog operations
            /// </summary>
            Reflog = 2,
        }
    }
}
