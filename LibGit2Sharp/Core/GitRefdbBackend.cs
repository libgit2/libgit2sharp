using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitRefdbBackend
    {
        static GitRefdbBackend()
        {
            GCHandleOffset = Marshal.OffsetOf(typeof(GitRefdbBackend), "GCHandle").ToInt32();
        }

        public uint Version;

        public exists_callback Exists;
        public lookup_callback Lookup;
        public foreach_callback Foreach;
        public foreach_glob_callback ForeachGlob;
        public write_callback Write;
        public delete_callback Delete;
        public compress_callback Compress;
        public free_callback Free;

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        public IntPtr GCHandle;

        /* The following static fields are not part of the structure definition. */

        public static int GCHandleOffset;

        /// <summary>
        ///   Queries the backend to determine if the given referenceName
        ///   exists.
        /// </summary>
        /// <param name="exists">[out] If the call is successful, the backend will set this to 1 if the reference exists, 0 otherwise.</param>
        /// <param name="backend">[in] A pointer to the backend which is being queried.</param>
        /// <param name="referenceName">[in] The reference name to look up.</param>
        /// <returns>0 if successful; an error code otherwise.</returns>
        public delegate int exists_callback(
            out IntPtr exists,
            IntPtr backend,
            IntPtr referenceName);

        /// <summary>
        ///   Queries the backend for the given reference.
        /// </summary>
        /// <param name="reference">[out] If the call is successful, the backend will set this to the reference.</param>
        /// <param name="backend">[in] A pointer to the backend which is being queried.</param>
        /// <param name="referenceName">[in] The reference name to look up.</param>
        /// <returns>0 if successful; GIT_EEXISTS or an error code otherwise.</returns>
        public delegate int lookup_callback(
            out IntPtr reference,
            IntPtr backend,
            IntPtr referenceName);

        /// <summary>
        ///   Iterates each reference that matches list_flags, calling back to the given callback.
        /// </summary>
        /// <param name="backend">[in] A pointer to the backend to query.</param>
        /// <param name="list_flags">[in] The references to list.</param>
        /// <param name="cb">[in] The callback function to invoke.</param>
        /// <param name="data">[in] An arbitrary parameter to pass through to the callback</param>
        /// <returns>0 if successful; GIT_EUSER or an error code otherwise.</returns>
        public delegate int foreach_callback(
            IntPtr backend,
            GitReferenceType list_flags,
            foreach_callback_callback cb,
            IntPtr data);

        /// <summary>
        ///   Iterates each reference that matches the glob pattern and the list_flags, calling back to the given callback.
        /// </summary>
        /// <param name="backend">[in] A pointer to the backend to query.</param>
        /// <param name="glob">[in] A glob pattern.</param>
        /// <param name="list_flags">[in] The references to list.</param>
        /// <param name="cb">[in] The callback function to invoke.</param>
        /// <param name="data">[in] An arbitrary parameter to pass through to the callback</param>
        /// <returns>0 if successful; GIT_EUSER or an error code otherwise.</returns>
        public delegate int foreach_glob_callback(
            IntPtr backend,
            IntPtr glob,
            GitReferenceType list_flags,
            foreach_callback_callback cb,
            IntPtr data);

        /// <summary>
        ///   Writes the given reference.
        /// </summary>
        /// <param name="backend">[in] A pointer to the backend to write to.</param>
        /// <param name="referencePtr">[in] The reference to write.</param>
        /// <returns>0 if successful; an error code otherwise.</returns>
        public delegate int write_callback(
            IntPtr backend,
            IntPtr referencePtr);

        /// <summary>
        ///   Deletes the given reference.
        /// </summary>
        /// <param name="backend">[in] A pointer to the backend to delete.</param>
        /// <param name="referencePtr">[in] The reference to delete.</param>
        /// <returns>0 if successful; an error code otherwise.</returns>
        public delegate int delete_callback(
            IntPtr backend,
            IntPtr referencePtr);

        /// <summary>
        ///   Compresses the contained references, if possible.  The backend is free to implement this in any implementation-defined way; or not at all.
        /// </summary>
        /// <param name="backend">[in] A pointer to the backend to compress.</param>
        /// <returns>0 if successful; an error code otherwise.</returns>
        public delegate int compress_callback(
            IntPtr backend);

        /// <summary>
        /// The owner of this backend is finished with it. The backend is asked to clean up and shut down.
        /// </summary>
        /// <param name="backend">[in] A pointer to the backend which is being freed.</param>
        public delegate void free_callback(
            IntPtr backend);

        /// <summary>
        /// A callback for the backend's implementation of foreach.
        /// </summary>
        /// <param name="referenceName">The reference name.</param>
        /// <param name="data">Pointer to payload data passed to the caller.</param>
        /// <returns>A zero result indicates the enumeration should continue. Otherwise, the enumeration should stop.</returns>
        public delegate int foreach_callback_callback(
            IntPtr referenceName,
            IntPtr data);
    }
}
