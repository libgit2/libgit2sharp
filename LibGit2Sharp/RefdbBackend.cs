using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    /// Reference database backend.
    /// </summary>
    public abstract class RefdbBackend
    {
        /// <summary>
        /// Minimum supported operations.
        /// </summary>
        [Flags]
        public enum SupportedOperations
        {
            /// <summary>
            /// At a minimum: Exists, Lookup, Iterate, Write, Delete, Rename
            /// </summary>
            /// <remarks>
            /// libgit2 wants us to also support all the reflog operations, but unless
            /// reflog is enabled on the repository, we shouldn't have any issues stubbing those out.
            /// </remarks>
            Minimum = 0,

            /// <summary>
            /// Compress operation.
            /// </summary>
            Compress = 1,

            /// <summary>
            /// Lock and unlock (transactional) operations.
            /// </summary>
            LockUnlock = 2,

            /// <summary>
            /// Reflog operations.
            /// </summary>
            Reflog = 4
        }

        private IntPtr nativePointer;

        /// <summary>
        /// Gets the supported operations.
        /// </summary>
        protected abstract SupportedOperations OperationsSupported { get; }

        /// <summary>
        /// Gets the repository.
        /// </summary>
        protected Repository Repository { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RefdbBackend"/> class.
        /// </summary>
        /// <param name="repository">Repository that this refdb is attached to.</param>
        protected RefdbBackend(Repository repository)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            this.Repository = repository;
        }

        /// <summary>
        /// Checks to see if a reference exists.
        /// </summary>
        public abstract bool Exists(string refName);

        /// <summary>
        /// Attempts to look up a reference.
        /// </summary>
        /// <returns>False if the reference doesn't exist.</returns>
        public abstract bool Lookup(string refName, out ReferenceData data);

        /// <summary>
        /// Iterates all references (if glob is null) or only references matching glob (if not null.)
        /// </summary>
        public abstract IEnumerable<ReferenceData> Iterate(string glob);

        /// <summary>
        /// Writes a reference to the database.
        /// </summary>
        /// <param name="newRef">New reference to write.</param>
        /// <param name="oldRef">Old reference (possibly null.)</param>
        /// <param name="force">True if overwrites are allowed.</param>
        /// <param name="signature">User signature.</param>
        /// <param name="message">User message.</param>
        public abstract void Write(ReferenceData newRef, ReferenceData oldRef, bool force, Signature signature, string message);

        /// <summary>
        /// Deletes a reference from the database.
        /// </summary>
        /// <param name="existingRef">Reference to delete.</param>
        public abstract void Delete(ReferenceData existingRef);

        /// <summary>
        /// Renames a reference.
        /// </summary>
        /// <param name="oldName">Old name.</param>
        /// <param name="newName">New name.</param>
        /// <param name="force">Allow overwrites.</param>
        /// <param name="signature">User signature.</param>
        /// <param name="message">User message.</param>
        /// <returns>New reference.</returns>
        public abstract ReferenceData Rename(string oldName, string newName, bool force, Signature signature, string message);

        /// <summary>
        /// Compresses the references.
        /// </summary>
        public virtual void Compress()
        {
            throw RefdbBackendException.NotImplemented("Compress");
        }

        /// <summary>
        /// Locks a single reference, returning a payload object which is passed to unlock it.
        /// </summary>
        public virtual object Lock(string refName)
        {
            throw RefdbBackendException.NotImplemented("Lock");
        }

        /// <summary>
        /// Unlocks a single reference, given its payload object and operational details.
        /// </summary>
        public virtual object Unlock(object payload, ReferenceData reference, Signature sig, string message, bool success, bool updateReflog)
        {
            throw RefdbBackendException.NotImplemented("Unlock");
        }

        /// <summary>
        /// Backend pointer. Accessing this lazily allocates a marshalled GitRefdbBackend, which is freed with Free().
        /// </summary>
        internal IntPtr RefdbBackendPointer
        {
            get
            {
                if (IntPtr.Zero == nativePointer)
                {
                    var nativeBackend = new GitRefdbBackend()
                    {
                        Version = 1,
                        Compress = null,
                        Lock = null,
                        Unlock = null,
                        Exists = BackendEntryPoints.ExistsCallback,
                        Lookup = BackendEntryPoints.LookupCallback,
                        Iterator = BackendEntryPoints.IteratorCallback,
                        Write = BackendEntryPoints.WriteCallback,
                        Rename = BackendEntryPoints.RenameCallback,
                        Del = BackendEntryPoints.DelCallback,
                        HasLog = BackendEntryPoints.HasLogCallback,
                        EnsureLog = BackendEntryPoints.EnsureLogCallback,
                        Free = BackendEntryPoints.FreeCallback,
                        ReflogRead = BackendEntryPoints.ReflogReadCallback,
                        ReflogWrite = BackendEntryPoints.ReflogWriteCallback,
                        ReflogRename = BackendEntryPoints.ReflogRenameCallback,
                        ReflogDelete = BackendEntryPoints.ReflogDeleteCallback,
                        GCHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this))
                    };

                    if (OperationsSupported.HasFlag(SupportedOperations.Compress))
                    {
                        nativeBackend.Compress = BackendEntryPoints.CompressCallback;
                    }

                    if (OperationsSupported.HasFlag(SupportedOperations.LockUnlock))
                    {
                        nativeBackend.Lock = BackendEntryPoints.LockCallback;
                        nativeBackend.Unlock = BackendEntryPoints.UnlockCallback;
                    }

                    nativePointer = Marshal.AllocHGlobal(Marshal.SizeOf(nativeBackend));
                    Marshal.StructureToPtr(nativeBackend, nativePointer, false);
                }

                return nativePointer;
            }
        }

        /// <summary>
        /// Frees the backend pointer, if one has been allocated.
        /// </summary>
        internal void Free()
        {
            if (IntPtr.Zero == nativePointer)
            {
                return;
            }

            GCHandle.FromIntPtr(Marshal.ReadIntPtr(nativePointer, GitRefdbBackend.GCHandleOffset)).Free();
            Marshal.FreeHGlobal(nativePointer);
            nativePointer = IntPtr.Zero;
        }

        /// <summary>
        /// Backend's representation of a reference.
        /// </summary>
        public class ReferenceData
        {
            /// <summary>
            /// Reference name.
            /// </summary>
            public string RefName { get; private set; }

            /// <summary>
            /// True if symbolic; otherwise, false.
            /// </summary>
            public bool IsSymbolic { get; private set; }

            /// <summary>
            /// Object ID, if the ref isn't symbolic.
            /// </summary>
            public ObjectId ObjectId { get; private set; }

            /// <summary>
            /// Target name, if the ref is symbolic.
            /// </summary>
            public string SymbolicTarget { get; private set; }

            /// <summary>
            /// Initializes a direct reference.
            /// </summary>
            public ReferenceData(string refName, ObjectId directTarget)
            {
                this.RefName = refName;
                this.IsSymbolic = false;
                this.ObjectId = directTarget;
                this.SymbolicTarget = null;
            }

            /// <summary>
            /// Initializes a symbolic reference.
            /// </summary>
            public ReferenceData(string refName, string symbolicTarget)
            {
                this.RefName = refName;
                this.IsSymbolic = true;
                this.ObjectId = null;
                this.SymbolicTarget = symbolicTarget;
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                var other = obj as ReferenceData;
                if (other == null)
                {
                    return false;
                }

                return other.RefName == this.RefName
                    && other.IsSymbolic == this.IsSymbolic
                    && other.ObjectId == this.ObjectId
                    && other.SymbolicTarget == this.SymbolicTarget;
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    var accumulator = this.RefName.GetHashCode();
                    accumulator = accumulator * 17 + this.IsSymbolic.GetHashCode();
                    if (this.ObjectId != null)
                    {
                        accumulator = accumulator * 17 + this.ObjectId.GetHashCode();
                    }

                    if (this.SymbolicTarget != null)
                    {
                        accumulator = accumulator * 17 + this.SymbolicTarget.GetHashCode();
                    }

                    return accumulator;
                }
            }

            internal IntPtr MarshalToPtr()
            {
                if (IsSymbolic)
                {
                    return Proxy.git_reference__alloc_symbolic(RefName, SymbolicTarget).AsIntPtr();
                }
                else
                {
                    return Proxy.git_reference__alloc(RefName, ObjectId.Oid).AsIntPtr();
                }
            }

            internal static unsafe ReferenceData MarshalFromPtr(git_reference* ptr)
            {
                var name = Proxy.git_reference_name(ptr);
                var type = Proxy.git_reference_type(ptr);
                switch (type)
                {
                    case GitReferenceType.Oid:
                        var targetOid = Proxy.git_reference_target(ptr);
                        return new ReferenceData(name, targetOid);
                    case GitReferenceType.Symbolic:
                        var targetName = Proxy.git_reference_symbolic_target(ptr);
                        return new ReferenceData(name, targetName);
                    default:
                        throw new LibGit2SharpException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Unable to build a new reference from type '{0}'",
                                type));
                }
            }
        }

        /// <summary>
        /// Exception types that can be thrown from the backend.
        /// Exceptions of this type will be converted to libgit2 error codes.
        /// </summary>
        public sealed class RefdbBackendException : LibGit2SharpException
        {
            private RefdbBackendException(GitErrorCode code, string message)
                : base(message, code, GitErrorCategory.Reference)
            {
                Code = code;
            }

            internal GitErrorCode Code { get; private set; }

            /// <summary>
            /// Reference was not found.
            /// </summary>
            public static RefdbBackendException NotFound(string refName)
            {
                return new RefdbBackendException(GitErrorCode.NotFound, string.Format("could not resolve reference '{0}'", refName));
            }

            /// <summary>
            /// Reference by this name already exists.
            /// </summary>
            public static RefdbBackendException Exists(string refName)
            {
                return new RefdbBackendException(GitErrorCode.Exists, string.Format("will not overwrite reference '{0}' without match or force", refName));
            }

            /// <summary>
            /// Conflict between an expected reference value and the reference's actual value.
            /// </summary>
            public static RefdbBackendException Conflict(string refName)
            {
                return new RefdbBackendException(GitErrorCode.Conflict, string.Format("conflict occurred while writing reference '{0}'", refName));
            }

            /// <summary>
            /// User is not allowed to alter this reference.
            /// </summary>
            /// <param name="message">Arbitrary message.</param>
            public static RefdbBackendException NotAllowed(string message)
            {
                return new RefdbBackendException(GitErrorCode.Auth, message);
            }

            /// <summary>
            /// Operation is not implemented.
            /// </summary>
            /// <param name="operation">Operation that's not implemented.</param>
            public static RefdbBackendException NotImplemented(string operation)
            {
                return new RefdbBackendException(GitErrorCode.User, string.Format("operation '{0}' is unsupported by this refdb backend.", operation));
            }

            internal static int GetCode(Exception ex)
            {
                Proxy.git_error_set_str(GitErrorCategory.Reference, ex);
                var backendException = ex as RefdbBackendException;
                if (backendException == null)
                {
                    return (int)GitErrorCode.Error;
                }

                return (int)backendException.Code;
            }
        }

        private class RefIterator
        {
            private readonly IEnumerator<ReferenceData> enumerator;

            public RefIterator(IEnumerator<ReferenceData> enumerator)
            {
                this.enumerator = enumerator;
            }

            public ReferenceData GetNext()
            {
                if (this.enumerator.MoveNext())
                {
                    return this.enumerator.Current;
                }

                return null;
            }
        }

        private unsafe static class IteratorEntryPoints
        {
            public static readonly GitRefdbIterator.next_callback NextCallback = Next;
            public static readonly GitRefdbIterator.next_name_callback NextNameCallback = NextName;
            public static readonly GitRefdbIterator.free_callback FreeCallback = Free;

            public static int Next(
                out IntPtr referencePtr,
                IntPtr iterator)
            {
                referencePtr = IntPtr.Zero;
                var backend = PtrToBackend(iterator);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                ReferenceData data;
                try
                {
                    data = backend.GetNext();
                }
                catch (Exception ex)
                {
                    return RefdbBackendException.GetCode(ex);
                }

                if (data == null)
                {
                    return (int)GitErrorCode.IterOver;
                }

                referencePtr = data.MarshalToPtr();
                return (int)GitErrorCode.Ok;
            }

            public static int NextName(
                out string refNamePtr,
                IntPtr iterator)
            {
                refNamePtr = null;
                var backend = PtrToBackend(iterator);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                ReferenceData data;
                try
                {
                    data = backend.GetNext();
                }
                catch (Exception ex)
                {
                    return RefdbBackendException.GetCode(ex);
                }

                if (data == null)
                {
                    return (int)GitErrorCode.IterOver;
                }

                refNamePtr = data.RefName;
                return (int)GitErrorCode.Ok;
            }

            public static void Free(IntPtr iterator)
            {
                GCHandle.FromIntPtr(Marshal.ReadIntPtr(iterator, GitRefdbIterator.GCHandleOffset)).Free();
                Marshal.FreeHGlobal(iterator);
            }

            private static RefIterator PtrToBackend(IntPtr pointer)
            {
                var intPtr = Marshal.ReadIntPtr(pointer, GitRefdbIterator.GCHandleOffset);
                var backend = GCHandle.FromIntPtr(intPtr).Target as RefIterator;

                if (backend == null)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Reference, "Cannot retrieve the managed RefIterator");
                }

                return backend;
            }
        }

        private unsafe static class BackendEntryPoints
        {
            public static readonly GitRefdbBackend.exists_callback ExistsCallback = Exists;
            public static readonly GitRefdbBackend.lookup_callback LookupCallback = Lookup;
            public static readonly GitRefdbBackend.iterator_callback IteratorCallback = Iterator;
            public static readonly GitRefdbBackend.write_callback WriteCallback = Write;
            public static readonly GitRefdbBackend.rename_callback RenameCallback = Rename;
            public static readonly GitRefdbBackend.del_callback DelCallback = Del;
            public static readonly GitRefdbBackend.compress_callback CompressCallback = Compress;
            public static readonly GitRefdbBackend.has_log_callback HasLogCallback = HasLog;
            public static readonly GitRefdbBackend.ensure_log_callback EnsureLogCallback = EnsureLog;
            public static readonly GitRefdbBackend.free_callback FreeCallback = Free;
            public static readonly GitRefdbBackend.reflog_read_callback ReflogReadCallback = ReflogRead;
            public static readonly GitRefdbBackend.reflog_write_callback ReflogWriteCallback = ReflogWrite;
            public static readonly GitRefdbBackend.reflog_rename_callback ReflogRenameCallback = ReflogRename;
            public static readonly GitRefdbBackend.reflog_delete_callback ReflogDeleteCallback = ReflogDelete;
            public static readonly GitRefdbBackend.lock_callback LockCallback = Lock;
            public static readonly GitRefdbBackend.unlock_callback UnlockCallback = Unlock;

            public static int Exists(
                ref bool exists,
                IntPtr backendPtr,
                string refName)
            {
                var backend = PtrToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    exists = backend.Exists(refName);
                }
                catch (Exception ex)
                {
                    return RefdbBackendException.GetCode(ex);
                }

                return (int)GitErrorCode.Ok;
            }

            public static int Lookup(
                out IntPtr referencePtr,
                IntPtr backendPtr,
                string refName)
            {
                referencePtr = IntPtr.Zero;
                var backend = PtrToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    ReferenceData data;
                    if (!backend.Lookup(refName, out data))
                    {
                        return (int)GitErrorCode.NotFound;
                    }

                    referencePtr = data.MarshalToPtr();
                }
                catch (Exception ex)
                {
                    return RefdbBackendException.GetCode(ex);
                }

                return (int)GitErrorCode.Ok;
            }

            public static int Iterator(
                out IntPtr iteratorPtr,
                IntPtr backendPtr,
                string glob)
            {
                iteratorPtr = IntPtr.Zero;
                var backend = PtrToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                RefIterator iterator;
                try
                {
                    var enumerator = backend.Iterate(glob).GetEnumerator();
                    iterator = new RefIterator(enumerator);
                }
                catch (Exception ex)
                {
                    return RefdbBackendException.GetCode(ex);
                }

                var nativeIterator = new GitRefdbIterator()
                {
                    Refdb = backendPtr,
                    Next = IteratorEntryPoints.Next,
                    NextName = IteratorEntryPoints.NextName,
                    Free = IteratorEntryPoints.Free,
                    GCHandle = GCHandle.ToIntPtr(GCHandle.Alloc(iterator))
                };

                iteratorPtr = Marshal.AllocHGlobal(Marshal.SizeOf(nativeIterator));
                Marshal.StructureToPtr(nativeIterator, iteratorPtr, false);
                return (int)GitErrorCode.Ok;
            }

            public static int Write(
                IntPtr backendPtr,
                git_reference* reference,
                bool force,
                git_signature* who,
                string message,
                IntPtr old,
                string oldTarget)
            {
                var backend = PtrToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                var signature = new Signature(who);

                // New ref data is constructed directly from the reference pointer.
                var newRef = ReferenceData.MarshalFromPtr(reference);

                // Old ref value is provided as a check, so that the refdb can atomically test the old value
                // and set the new value, thereby preventing write conflicts.
                // If a write conflict is detected, we should return GIT_EMODIFIED.
                // If the ref is brand new, the "old" oid pointer is null.
                ReferenceData oldRef = null;
                if (old != IntPtr.Zero)
                {
                    oldRef = new ReferenceData(oldTarget, ObjectId.BuildFromPtr(old));
                }

                try
                {
                    // If the user returns false, we detected a conflict and aborted the write.
                    backend.Write(newRef, oldRef, force, signature, message);
                }
                catch (Exception ex)
                {
                    return RefdbBackendException.GetCode(ex);
                }

                return (int)GitErrorCode.Ok;
            }

            public static int Rename(
                out IntPtr reference,
                IntPtr backendPtr,
                string oldName,
                string newName,
                bool force,
                git_signature* who,
                string message)
            {
                reference = IntPtr.Zero;
                var backend = PtrToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                var signature = new Signature(who);

                ReferenceData newRef;
                try
                {
                    newRef = backend.Rename(oldName, newName, force, signature, message);
                }
                catch (Exception ex)
                {
                    return RefdbBackendException.GetCode(ex);
                }

                reference = newRef.MarshalToPtr();
                return (int)GitErrorCode.Ok;
            }

            public static int Del(
                IntPtr backendPtr,
                string refName,
                IntPtr oldId,
                string oldTarget)
            {
                var backend = PtrToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                ReferenceData existingRef;
                if (IntPtr.Zero == oldId)
                {
                    existingRef = new ReferenceData(refName, oldTarget);
                }
                else
                {
                    existingRef = new ReferenceData(refName, ObjectId.BuildFromPtr(oldId));
                }

                try
                {
                    backend.Delete(existingRef);
                }
                catch (Exception ex)
                {
                    return RefdbBackendException.GetCode(ex);
                }

                return (int)GitErrorCode.Ok;
            }

            public static int Compress(IntPtr backendPtr)
            {
                var backend = PtrToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    backend.Compress();
                }
                catch (Exception ex)
                {
                    return RefdbBackendException.GetCode(ex);
                }

                return (int)GitErrorCode.Ok;
            }

            public static int HasLog(
                IntPtr backend,
                string refName)
            {
                return (int)GitErrorCode.Error;
            }

            public static int EnsureLog(
                IntPtr backend,
                string refName)
            {
                return (int)GitErrorCode.Error;
            }

            public static void Free(IntPtr backend)
            {
                PtrToBackend(backend).Free();
            }

            public static int ReflogRead(
                out git_reflog* reflog,
                IntPtr backend,
                string name)
            {
                reflog = null;
                return (int)GitErrorCode.Error;
            }

            public static int ReflogWrite(
                IntPtr backend,
                git_reflog* reflog)
            {
                return (int)GitErrorCode.Error;
            }

            public static int ReflogRename(
                IntPtr backend,
                string oldName,
                string newName)
            {
                return (int)GitErrorCode.Error;
            }

            public static int ReflogDelete(
                IntPtr backend,
                string name)
            {
                return (int)GitErrorCode.Error;
            }

            public static int Lock(
                out IntPtr payloadPtr,
                IntPtr backendPtr,
                string refName)
            {
                payloadPtr = IntPtr.Zero;
                var backend = PtrToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                object payload;
                try
                {
                    payload = backend.Lock(refName);
                }
                catch (Exception ex)
                {
                    return RefdbBackendException.GetCode(ex);
                }

                payloadPtr = GCHandle.ToIntPtr(GCHandle.Alloc(payload));
                return (int)GitErrorCode.Ok;
            }

            public static int Unlock(
                IntPtr backendPtr,
                IntPtr payloadPtr,
                bool success,
                bool updateReflog,
                git_reference* referencePtr,
                git_signature* who,
                string message)
            {
                var backend = PtrToBackend(backendPtr);
                if (backend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                var reference = ReferenceData.MarshalFromPtr(referencePtr);
                var signature = new Signature(who);

                var payloadGC = GCHandle.FromIntPtr(payloadPtr);
                var payload = payloadGC.Target;
                payloadGC.Free();

                try
                {
                    backend.Unlock(payload, reference, signature, message, success, updateReflog);
                }
                catch (Exception ex)
                {
                    return RefdbBackendException.GetCode(ex);
                }

                return (int)GitErrorCode.Ok;
            }

            private static RefdbBackend PtrToBackend(IntPtr pointer)
            {
                var intPtr = Marshal.ReadIntPtr(pointer, GitRefdbBackend.GCHandleOffset);
                var backend = GCHandle.FromIntPtr(intPtr).Target as RefdbBackend;

                if (backend == null)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Reference, "Cannot retrieve the managed RefdbBackend");
                }

                return backend;
            }
        }
    }
}
