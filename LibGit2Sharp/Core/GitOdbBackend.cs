using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitOdbBackend
    {
        static GitOdbBackend()
        {
            GCHandleOffset = Marshal.OffsetOf<GitOdbBackend>(nameof(GCHandle)).ToInt32();
        }

        public uint Version;

#pragma warning disable 169

        /// <summary>
        /// This field is populated by libgit2 at backend addition time, and exists for its
        /// use only. From this side of the interop, it is unreferenced.
        /// </summary>
        private readonly IntPtr Odb;

#pragma warning restore 169

        public read_callback Read;
        public read_prefix_callback ReadPrefix;
        public read_header_callback ReadHeader;
        public write_callback Write;
        public writestream_callback WriteStream;
        public readstream_callback ReadStream;
        public exists_callback Exists;
        public exists_prefix_callback ExistsPrefix;
        public IntPtr Refresh;
        public foreach_callback Foreach;
        public IntPtr WritePack;
        public IntPtr WriteMidx;
        public IntPtr Freshen;
        public free_callback Free;

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        public IntPtr GCHandle;

        /* The following static fields are not part of the structure definition. */

        public static int GCHandleOffset;

        /// <summary>
        /// The backend is passed an OID. From that data the backend is expected to return a pointer to the
        /// data for that object, the size of the data, and the type of the object.
        /// </summary>
        /// <param name="buffer_p">[out] If the call is successful, the backend will write the address of a buffer containing the object contents here.</param>
        /// <param name="len_p">[out] If the call is successful, the backend will write the length of the buffer containing the object contents here.</param>
        /// <param name="type_p">[out] If the call is successful, the backend will write the type of the object here.</param>
        /// <param name="backend">[in] A pointer to the backend which is being asked to perform the task.</param>
        /// <param name="oid">[in] The OID which the backend is being asked to look up.</param>
        /// <returns>0 if successful; an error code otherwise.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int read_callback(
            out IntPtr buffer_p,
            out UIntPtr len_p,
            out GitObjectType type_p,
            IntPtr backend,
            ref GitOid oid);

        /// <summary>
        /// The backend is passed a short OID and the number of characters in that short OID.
        /// From that data the backend is expected to return the full OID (in out_oid), a pointer
        /// to the data (in buffer_p), the size of the buffer returned in buffer_p (in len_p),
        /// and the object type (in type_p). The short OID might not be long enough to resolve
        /// to just one object. In that case the backend should return GIT_EAMBIGUOUS.
        /// </summary>
        /// <param name="out_oid">[out] If the call is successful, the backend will write the full OID if the object here.</param>
        /// <param name="buffer_p">[out] If the call is successful, the backend will write the address of a buffer containing the object contents here.</param>
        /// <param name="len_p">[out] If the call is successful, the backend will write the length of the buffer containing the object contents here.</param>
        /// <param name="type_p">[out] If the call is successful, the backend will write the type of the object here.</param>
        /// <param name="backend">[in] A pointer to the backend which is being asked to perform the task.</param>
        /// <param name="short_oid">[in] The short-form OID which the backend is being asked to look up.</param>
        /// <param name="len">[in] The length of the short-form OID (short_oid).</param>
        /// <returns>0 if successful; an error code otherwise.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int read_prefix_callback(
            out GitOid out_oid,
            out IntPtr buffer_p,
            out UIntPtr len_p,
            out GitObjectType type_p,
            IntPtr backend,
            ref GitOid short_oid,
            UIntPtr len);

        /// <summary>
        /// The backend is passed an OID. From that data the backend is expected to return the size of the
        /// data for that OID, and the type of that OID.
        /// </summary>
        /// <param name="len_p">[out] If the call is successful, the backend will write the length of the data for the OID here.</param>
        /// <param name="type_p">[out] If the call is successful, the backend will write the type of the object here.</param>
        /// <param name="backend">[in] A pointer to the backend which is being asked to perform the task.</param>
        /// <param name="oid">[in] The OID which the backend is being asked to look up.</param>
        /// <returns>0 if successful; an error code otherwise.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int read_header_callback(
            out UIntPtr len_p,
            out GitObjectType type_p,
            IntPtr backend,
            ref GitOid oid);

        /// <summary>
        /// The backend is passed an OID, the type of the object, and its contents. The backend is asked to write
        /// that data to the backing store.
        /// </summary>
        /// <param name="oid">[in] The OID which the backend is being asked to write.</param>
        /// <param name="backend">[in] A pointer to the backend which is being asked to perform the task.</param>
        /// <param name="data">[in] A pointer to the data for this object.</param>
        /// <param name="len">[in] The length of the buffer pointed to by data.</param>
        /// <param name="type">[in] The type of the object.</param>
        /// <returns>0 if successful; an error code otherwise.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int write_callback(
            IntPtr backend,
            ref GitOid oid,
            IntPtr data,
            UIntPtr len,
            GitObjectType type);

        /// <summary>
        /// The backend is passed an OID, the type of the object, and the length of its contents. The backend is
        /// asked to return a stream object which the caller can use to write the contents of the object to the
        /// backing store.
        /// </summary>
        /// <param name="stream_out">[out] The stream object which the caller will use to write the contents for this object.</param>
        /// <param name="backend">[in] A pointer to the backend which is being asked to perform the task.</param>
        /// <param name="length">[in] The length of the object's contents.</param>
        /// <param name="type">[in] The type of the object being written.</param>
        /// <returns>0 if successful; an error code otherwise.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int writestream_callback(
            out IntPtr stream_out,
            IntPtr backend,
            Int64 length,
            GitObjectType type);

        /// <summary>
        /// The backend is passed an OID. The backend is asked to return a stream object which the caller can use
        /// to read the contents of this object from the backing store.
        /// </summary>
        /// <param name="stream_out">[out] The stream object which the caller will use to read the contents of this object.</param>
        /// <param name="backend">[in] A pointer to the backend which is being asked to perform the task.</param>
        /// <param name="oid">[in] The object ID that the caller is requesting.</param>
        /// <returns>0 if successful; an error code otherwise.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int readstream_callback(
            out IntPtr stream_out,
            IntPtr backend,
            ref GitOid oid);

        /// <summary>
        /// The backend is passed an OID. The backend is asked to return a value that indicates whether or not
        /// the object exists in the backing store.
        /// </summary>
        /// <param name="backend">[in] A pointer to the backend which is being asked to perform the task.</param>
        /// <param name="oid">[in] The object ID that the caller is requesting.</param>
        /// <returns>True if the object exists; false otherwise</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool exists_callback(
            IntPtr backend,
            ref GitOid oid);

        /// <summary>
        /// The backend is passed a short OID and the number of characters in that short OID.
        /// The backend is asked to return a value that indicates whether or not
        /// the object exists in the backing store. The short OID might not be long enough to resolve
        /// to just one object. In that case the backend should return GIT_EAMBIGUOUS.
        /// </summary>
        /// <param name="found_oid">[out] If the call is successful, the backend will write the full OID if the object here.</param>
        /// <param name="backend">[in] A pointer to the backend which is being asked to perform the task.</param>
        /// <param name="short_oid">[in] The short-form OID which the backend is being asked to look up.</param>
        /// <param name="len">[in] The length of the short-form OID (short_oid).</param>
        /// <returns>1 if the object exists, 0 if the object doesn't; an error code otherwise.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int exists_prefix_callback(
            ref GitOid found_oid,
            IntPtr backend,
            ref GitOid short_oid,
            UIntPtr len);

        /// <summary>
        /// The backend is passed a callback function and a void* to pass through to the callback. The backend is
        /// asked to iterate through all objects in the backing store, invoking the callback for each item.
        /// </summary>
        /// <param name="backend">[in] A pointer to the backend which is being asked to perform the task.</param>
        /// <param name="cb">[in] The callback function to invoke.</param>
        /// <param name="data">[in] An arbitrary parameter to pass through to the callback</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int foreach_callback(
            IntPtr backend,
            foreach_callback_callback cb,
            IntPtr data);

        /// <summary>
        /// The owner of this backend is finished with it. The backend is asked to clean up and shut down.
        /// </summary>
        /// <param name="backend">[in] A pointer to the backend which is being freed.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void free_callback(
            IntPtr backend);

        /// <summary>
        /// A callback for the backend's implementation of foreach.
        /// </summary>
        /// <param name="oid">The oid of each object in the backing store.</param>
        /// <param name="data">The arbitrary parameter given to foreach_callback.</param>
        /// <returns>A non-negative result indicates the enumeration should continue. Otherwise, the enumeration should stop.</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int foreach_callback_callback(
            IntPtr oid,
            IntPtr data);
    }
}
