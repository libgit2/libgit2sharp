using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GitRefdbIterator
    {
        static GitRefdbIterator()
        {
            GCHandleOffset = Marshal.OffsetOf<GitRefdbIterator>(nameof(GCHandle)).ToInt32();
        }

        public IntPtr Refdb;
        public next_callback Next;
        public next_name_callback NextName;
        public free_callback Free;

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        public IntPtr GCHandle;

        /* The following static fields are not part of the structure definition. */

        public static int GCHandleOffset;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int next_callback(
            out IntPtr referencePtr,
            IntPtr iterator);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int next_name_callback(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] out string refName,
            IntPtr iterator);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void free_callback(
            IntPtr iterator);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GitRefdbBackend
    {
        static GitRefdbBackend()
        {
            GCHandleOffset = Marshal.OffsetOf<GitRefdbBackend>(nameof(GCHandle)).ToInt32();
        }

        public uint Version;
        public exists_callback Exists;
        public lookup_callback Lookup;
        public iterator_callback Iterator;
        public write_callback Write;
        public rename_callback Rename;
        public del_callback Del;
        public IntPtr Compress;
        public has_log_callback HasLog;
        public ensure_log_callback EnsureLog;
        public free_callback Free;
        public reflog_read_callback ReflogRead;
        public reflog_write_callback ReflogWrite;
        public reflog_rename_callback ReflogRename;
        public reflog_delete_callback ReflogDelete;
        public IntPtr Lock;
        public IntPtr Unlock;

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        public IntPtr GCHandle;

        /* The following static fields are not part of the structure definition. */

        public static int GCHandleOffset;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int exists_callback(
            [MarshalAs(UnmanagedType.Bool)] ref bool exists,
            IntPtr backend,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string refNamePtr);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int lookup_callback(
            out IntPtr referencePtr,
            IntPtr backend,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string refName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int iterator_callback(
            out IntPtr iteratorPtr,
            IntPtr backend,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string glob);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int write_callback(
            IntPtr backend,
            git_reference* reference,
            [MarshalAs(UnmanagedType.Bool)] bool force,
            git_signature* who,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string message,
            IntPtr oid,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string oldTarget);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int rename_callback(
            git_reference* reference,
            IntPtr backend,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string oldName,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string newName,
            [MarshalAs(UnmanagedType.Bool)] bool force,
            git_signature* who,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int del_callback(
            IntPtr backend,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string refName,
            ref GitOid oldId,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string oldTarget);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int has_log_callback(
            IntPtr backend,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string refName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int ensure_log_callback(
            IntPtr backend,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string refName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void free_callback(IntPtr backend);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int reflog_read_callback(
            out git_reflog* reflog,
            IntPtr backend,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int reflog_write_callback(
            IntPtr backend,
            git_reflog* reflog);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int reflog_rename_callback(
            IntPtr backend,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string oldName,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string newName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int reflog_delete_callback(
            IntPtr backend,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string name);
    }
}
