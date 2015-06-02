using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitRefDbBackend
    {
        static GitRefDbBackend()
        {
            GCHandleOffset = Marshal.OffsetOf(typeof(GitRefDbBackend), "GCHandle").ToInt32();
        }

        public uint Version;

        public exists_callback Exists;
        public lookup_callback Lookup;
        public iterator_callback Iter;
        public write_callback Write;
        public rename_callback Rename;
        public delete_callback Delete;
        public compress_callback Compress;
        public has_log_callback HasLog;
        public ensure_log_callback EnsureLog;
        public free_callback FreeBackend;
        public reflog_write_callback ReflogWrite;
        public reflog_read_callback ReflogRead;
        public reflog_rename_callback ReflogRename;
        public reflog_delete_callback ReflogDelete;
        public ref_lock_callback RefLock;
        public ref_unlock_callback RefUnlock;

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        public IntPtr GCHandle;

        public static int GCHandleOffset;

        /// Queries the refdb backend to determine if the given ref_name
        /// A refdb implementation must provide this function.
        [return: MarshalAs(UnmanagedType.I4)]
        public delegate GitErrorCode exists_callback(
            [MarshalAs(UnmanagedType.Bool)] out bool exists,
            IntPtr backend,
            IntPtr refNamePtr);

        /// Queries the refdb backend for a given reference.  A refdb
        /// implementation must provide this function.
        [return: MarshalAs(UnmanagedType.I4)]
        public delegate GitErrorCode lookup_callback(
            out IntPtr git_reference,
            IntPtr backend,
            IntPtr refNamePtr);

        /// <summary>
        /// Allocate an iterator object for the backend.
        /// A refdb implementation must provide this function.
        /// </summary>
        [return: MarshalAs(UnmanagedType.I4)]
        public delegate GitErrorCode iterator_callback(
            out IntPtr iter,
            IntPtr backend,
            IntPtr globPtr);

        /// Writes the given reference to the refdb.  A refdb implementation
        /// must provide this function.
        [return: MarshalAs(UnmanagedType.I4)]
        public delegate GitErrorCode write_callback(
            IntPtr backend,
            IntPtr reference, // const git_reference *
            [MarshalAs(UnmanagedType.Bool)] bool force,
            IntPtr who, // const git_signature *
            IntPtr messagePtr, // const char *
            IntPtr oid, // const git_oid *
            IntPtr old_target // const char *
            );

        [return: MarshalAs(UnmanagedType.I4)]
        public delegate GitErrorCode rename_callback(
            out IntPtr reference, // git_reference **
            IntPtr backend, // git_refdb_backend *
            IntPtr old_name, // const char *
            IntPtr new_name, // const char *
            [MarshalAs(UnmanagedType.Bool)] bool force,
            IntPtr who, // const git_signature *
            IntPtr message // const char *
            );

        [return: MarshalAs(UnmanagedType.I4)]
        public delegate GitErrorCode delete_callback(
            IntPtr backend, // git_refdb_backend *
            IntPtr ref_name, // const char *
            IntPtr oldId, // const git_oid *
            IntPtr old_target // const char *
            );

        [return: MarshalAs(UnmanagedType.I4)]
        public delegate GitErrorCode compress_callback(
            IntPtr backend // git_refdb_backend *
            );

        public delegate GitErrorCode has_log_callback(
            IntPtr backend, // git_refdb_backend *
            IntPtr refNamePtr // const char *
            );

        public delegate GitErrorCode ensure_log_callback(
            IntPtr backend, // git_refdb_backend *
            IntPtr refNamePtr // const char *
            );

        public delegate void free_callback(
            IntPtr backend // git_refdb_backend *
            );

        public delegate GitErrorCode reflog_read_callback(
            out IntPtr git_reflog, // git_reflog **
            IntPtr backend, // git_refdb_backend *
            IntPtr refNamePtr // const char *
            );

        public delegate GitErrorCode reflog_write_callback(
            IntPtr backend, // git_refdb_backend *
            IntPtr git_reflog // git_reflog *
            );

        public delegate GitErrorCode reflog_rename_callback(
            IntPtr backend, // git_refdb_backend
            IntPtr oldNamePtr, // const char *
            IntPtr newNamePtr // const char *
            );

        public delegate GitErrorCode reflog_delete_callback(
            IntPtr backend, // git_refdb_backend
            IntPtr namePtr // const char *
            );

        public delegate GitErrorCode ref_lock_callback(
            IntPtr payload, // void **
            IntPtr backend, // git_refdb_backend
            IntPtr namePtr // const char *
            );

        public delegate GitErrorCode ref_unlock_callback(
            IntPtr backend, // git_refdb_backend
            IntPtr payload,
            [MarshalAs(UnmanagedType.Bool)] bool force,
            [MarshalAs(UnmanagedType.Bool)] bool update_reflog,
            IntPtr refNamePtr, // const char *
            IntPtr who, // const git_signature *
            IntPtr messagePtr // const char *
            );
    }
}
