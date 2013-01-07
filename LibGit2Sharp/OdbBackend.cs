using System;
using System.IO;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Base class for all custom managed backends for the libgit2 object database (ODB).
    /// </summary>
    public abstract class OdbBackend
    {
        /// <summary>
        ///   Invoked by libgit2 when this backend is no longer needed.
        /// </summary>
        protected virtual void Dispose()
        {
            if (IntPtr.Zero != nativeBackendPointer)
            {
                GCHandle.FromIntPtr(Marshal.ReadIntPtr(nativeBackendPointer, GitOdbBackend.GCHandleOffset)).Free();
                Marshal.FreeHGlobal(nativeBackendPointer);
                nativeBackendPointer = IntPtr.Zero;
            }
        }

        /// <summary>
        ///   In your subclass, override this member to provide the list of actions your backend supports.
        /// </summary>
        protected abstract OdbBackendOperations SupportedOperations
        {
            get;
        }

        /// <summary>
        ///   Call this method from your implementations of Read and ReadPrefix to allocate a buffer in
        ///   which to return the object's data.
        /// </summary>
        /// <param name="bytes">Number of bytes to allocate</param>
        /// <returns>An Stream for you to write to and then return. Do not dispose this object before returning it.</returns>
        protected unsafe Stream Allocate(long bytes)
        {
            if (bytes < 0 ||
                (UIntPtr.Size == sizeof(int) && bytes > int.MaxValue))
            {
                throw new ArgumentOutOfRangeException("bytes");
            }

            IntPtr buffer = Proxy.git_odb_backend_malloc(this.GitOdbBackendPointer, new UIntPtr((ulong)bytes));

            return new UnmanagedMemoryStream((byte*)buffer, 0, bytes, FileAccess.ReadWrite);
        }

        /// <summary>
        ///   Requests that this backend read an object.
        /// </summary>
        public abstract int Read(byte[] oid,
            out Stream data,
            out GitObjectType objectType);

        /// <summary>
        ///   Requests that this backend read an object. The object ID may not be complete (may be a prefix).
        /// </summary>
        public abstract int ReadPrefix(byte[] shortOid,
            out byte[] oid,
            out Stream data,
            out GitObjectType objectType);

        /// <summary>
        ///   Requests that this backend read an object's header (length and object type) but not its contents.
        /// </summary>
        public abstract int ReadHeader(byte[] oid,
            out int length,
            out GitObjectType objectType);

        /// <summary>
        ///   Requests that this backend write an object to the backing store. The backend may need to compute the object ID
        ///   and return it to the caller.
        /// </summary>
        public abstract int Write(byte[] oid,
            Stream dataStream,
            long length,
            GitObjectType objectType,
            out byte[] finalOid);

        /// <summary>
        ///   Requests that this backend read an object. Returns a stream so that the caller can read the data in chunks.
        /// </summary>
        public abstract int ReadStream(byte[] oid,
            out OdbBackendStream stream);

        /// <summary>
        ///   Requests that this backend write an object to the backing store. Returns a stream so that the caller can write
        ///   the data in chunks.
        /// </summary>
        public abstract int WriteStream(long length,
            GitObjectType objectType,
            out OdbBackendStream stream);

        /// <summary>
        ///   Requests that this backend check if an object ID exists.
        /// </summary>
        public abstract bool Exists(byte[] oid);

        /// <summary>
        ///   Requests that this backend enumerate all items in the backing store.
        /// </summary>
        public abstract int ForEach(ForEachCallback callback);

        /// <summary>
        ///   The signature of the callback method provided to the Foreach method.
        /// </summary>
        /// <param name="oid">The object ID of the object in the backing store.</param>
        /// <returns>A non-negative result indicates the enumeration should continue. Otherwise, the enumeration should stop.</returns>
        public delegate int ForEachCallback(byte[] oid);

        private IntPtr nativeBackendPointer;

        internal IntPtr GitOdbBackendPointer
        {
            get
            {
                if (IntPtr.Zero == nativeBackendPointer)
                {
                    var nativeBackend = new GitOdbBackend();
                    nativeBackend.Version = 1;

                    // The "free" entry point is always provided.
                    nativeBackend.Free = BackendEntryPoints.FreeCallback;

                    var supportedOperations = this.SupportedOperations;

                    if ((supportedOperations & OdbBackendOperations.Read) != 0)
                    {
                        nativeBackend.Read = BackendEntryPoints.ReadCallback;
                    }

                    if ((supportedOperations & OdbBackendOperations.ReadPrefix) != 0)
                    {
                        nativeBackend.ReadPrefix = BackendEntryPoints.ReadPrefixCallback;
                    }

                    if ((supportedOperations & OdbBackendOperations.ReadHeader) != 0)
                    {
                        nativeBackend.ReadHeader = BackendEntryPoints.ReadHeaderCallback;
                    }

                    if ((supportedOperations & OdbBackendOperations.ReadStream) != 0)
                    {
                        nativeBackend.ReadStream = BackendEntryPoints.ReadStreamCallback;
                    }

                    if ((supportedOperations & OdbBackendOperations.Write) != 0)
                    {
                        nativeBackend.Write = BackendEntryPoints.WriteCallback;
                    }

                    if ((supportedOperations & OdbBackendOperations.WriteStream) != 0)
                    {
                        nativeBackend.WriteStream = BackendEntryPoints.WriteStreamCallback;
                    }

                    if ((supportedOperations & OdbBackendOperations.Exists) != 0)
                    {
                        nativeBackend.Exists = BackendEntryPoints.ExistsCallback;
                    }

                    if ((supportedOperations & OdbBackendOperations.ForEach) != 0)
                    {
                        nativeBackend.Foreach = BackendEntryPoints.ForEachCallback;
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
            public static GitOdbBackend.read_callback ReadCallback = new GitOdbBackend.read_callback(Read);
            public static GitOdbBackend.read_prefix_callback ReadPrefixCallback = new GitOdbBackend.read_prefix_callback(ReadPrefix);
            public static GitOdbBackend.read_header_callback ReadHeaderCallback = new GitOdbBackend.read_header_callback(ReadHeader);
            public static GitOdbBackend.readstream_callback ReadStreamCallback = new GitOdbBackend.readstream_callback(ReadStream);
            public static GitOdbBackend.write_callback WriteCallback = new GitOdbBackend.write_callback(Write);
            public static GitOdbBackend.writestream_callback WriteStreamCallback = new GitOdbBackend.writestream_callback(WriteStream);
            public static GitOdbBackend.exists_callback ExistsCallback = new GitOdbBackend.exists_callback(Exists);
            public static GitOdbBackend.foreach_callback ForEachCallback = new GitOdbBackend.foreach_callback(Foreach);
            public static GitOdbBackend.free_callback FreeCallback = new GitOdbBackend.free_callback(Free);

            private unsafe static int Read(
                out IntPtr buffer_p,
                out UIntPtr len_p,
                out GitObjectType type_p,
                IntPtr backend,
                ref GitOid oid)
            {
                buffer_p = IntPtr.Zero;
                len_p = UIntPtr.Zero;
                type_p = GitObjectType.Bad;

                OdbBackend odbBackend = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitOdbBackend.GCHandleOffset)).Target as OdbBackend;

                if (odbBackend != null)
                {
                    Stream dataStream = null;
                    GitObjectType objectType;

                    try
                    {
                        int toReturn = odbBackend.Read(oid.Id, out dataStream, out objectType);

                        if (0 == toReturn)
                        {
                            // Caller is expected to give us back a stream created with the Allocate() method.
                            UnmanagedMemoryStream memoryStream = dataStream as UnmanagedMemoryStream;

                            if (null == memoryStream)
                            {
                                return (int)GitErrorCode.Error;
                            }

                            len_p = new UIntPtr((ulong)memoryStream.Capacity);
                            type_p = objectType;

                            memoryStream.Seek(0, SeekOrigin.Begin);
                            buffer_p = new IntPtr(memoryStream.PositionPointer);
                        }

                        return toReturn;
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Odb, ex);
                    }
                    finally
                    {
                        if (null != dataStream)
                        {
                            dataStream.Dispose();
                        }
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private unsafe static int ReadPrefix(
                out GitOid out_oid,
                out IntPtr buffer_p,
                out UIntPtr len_p,
                out GitObjectType type_p,
                IntPtr backend,
                ref GitOid short_oid,
                UIntPtr len)
            {
                out_oid = default(GitOid);
                buffer_p = IntPtr.Zero;
                len_p = UIntPtr.Zero;
                type_p = GitObjectType.Bad;

                OdbBackend odbBackend = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitOdbBackend.GCHandleOffset)).Target as OdbBackend;

                if (odbBackend != null)
                {
                    byte[] oid;
                    Stream dataStream = null;
                    GitObjectType objectType;

                    try
                    {
                        // The length of short_oid is described in characters (40 per full ID) vs. bytes (20)
                        // which is what we care about.
                        byte[] shortOidArray = new byte[(long)len >> 1];
                        Array.Copy(short_oid.Id, shortOidArray, shortOidArray.Length);

                        int toReturn = odbBackend.ReadPrefix(shortOidArray, out oid, out dataStream, out objectType);

                        if (0 == toReturn)
                        {
                            // Caller is expected to give us back a stream created with the Allocate() method.
                            UnmanagedMemoryStream memoryStream = dataStream as UnmanagedMemoryStream;

                            if (null == memoryStream)
                            {
                                return (int)GitErrorCode.Error;
                            }

                            out_oid.Id = oid;
                            len_p = new UIntPtr((ulong)memoryStream.Capacity);
                            type_p = objectType;

                            memoryStream.Seek(0, SeekOrigin.Begin);
                            buffer_p = new IntPtr(memoryStream.PositionPointer);
                        }

                        return toReturn;
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Odb, ex);
                    }
                    finally
                    {
                        if (null != dataStream)
                        {
                            dataStream.Dispose();
                        }
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static int ReadHeader(
                out UIntPtr len_p,
                out GitObjectType type_p,
                IntPtr backend,
                ref GitOid oid)
            {
                len_p = UIntPtr.Zero;
                type_p = GitObjectType.Bad;

                OdbBackend odbBackend = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitOdbBackend.GCHandleOffset)).Target as OdbBackend;

                if (odbBackend != null)
                {
                    int length;
                    GitObjectType objectType;

                    try
                    {
                        int toReturn = odbBackend.ReadHeader(oid.Id, out length, out objectType);

                        if (0 == toReturn)
                        {
                            len_p = new UIntPtr((uint)length);
                            type_p = objectType;
                        }

                        return toReturn;
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Odb, ex);
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static unsafe int Write(
                ref GitOid oid,
                IntPtr backend,
                IntPtr data,
                UIntPtr len,
                GitObjectType type)
            {
                OdbBackend odbBackend = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitOdbBackend.GCHandleOffset)).Target as OdbBackend;

                if (odbBackend != null &&
                    len.ToUInt64() < (ulong)long.MaxValue)
                {
                    try
                    {
                        using (UnmanagedMemoryStream stream = new UnmanagedMemoryStream((byte*)data, (long)len.ToUInt64()))
                        {
                            byte[] finalOid;

                            int toReturn = odbBackend.Write(oid.Id, stream, (long)len.ToUInt64(), type, out finalOid);

                            if (0 == toReturn)
                            {
                                oid.Id = finalOid;
                            }

                            return toReturn;
                        }
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Odb, ex);
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static int WriteStream(
                out IntPtr stream_out,
                IntPtr backend,
                UIntPtr length,
                GitObjectType type)
            {
                stream_out = IntPtr.Zero;

                OdbBackend odbBackend = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitOdbBackend.GCHandleOffset)).Target as OdbBackend;

                if (odbBackend != null &&
                    length.ToUInt64() < (ulong)long.MaxValue)
                {
                    OdbBackendStream stream;

                    try
                    {
                        int toReturn = odbBackend.WriteStream((long)length.ToUInt64(), type, out stream);

                        if (0 == toReturn)
                        {
                            stream_out = stream.GitOdbBackendStreamPointer;
                        }

                        return toReturn;
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Odb, ex);
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static int ReadStream(
                out IntPtr stream_out,
                IntPtr backend,
                ref GitOid oid)
            {
                stream_out = IntPtr.Zero;

                OdbBackend odbBackend = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitOdbBackend.GCHandleOffset)).Target as OdbBackend;

                if (odbBackend != null)
                {
                    OdbBackendStream stream;

                    try
                    {
                        int toReturn = odbBackend.ReadStream(oid.Id, out stream);

                        if (0 == toReturn)
                        {
                            stream_out = stream.GitOdbBackendStreamPointer;
                        }

                        return toReturn;
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Odb, ex);
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static bool Exists(
                IntPtr backend,
                ref GitOid oid)
            {
                OdbBackend odbBackend = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitOdbBackend.GCHandleOffset)).Target as OdbBackend;

                if (odbBackend != null)
                {
                    try
                    {
                        return odbBackend.Exists(oid.Id);
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Odb, ex);
                    }
                }

                return false;
            }

            private static int Foreach(
                IntPtr backend,
                GitOdbBackend.foreach_callback_callback cb,
                IntPtr data)
            {
                OdbBackend odbBackend = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitOdbBackend.GCHandleOffset)).Target as OdbBackend;

                if (odbBackend != null)
                {
                    try
                    {
                        return odbBackend.ForEach(new ForeachState(cb, data).ManagedCallback);
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Odb, ex);
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static void Free(
                IntPtr backend)
            {
                GCHandle gcHandle = GCHandle.FromIntPtr(Marshal.ReadIntPtr(backend, GitOdbBackend.GCHandleOffset));
                OdbBackend odbBackend = gcHandle.Target as OdbBackend;

                if (odbBackend != null)
                {
                    try
                    {
                        odbBackend.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Odb, ex);
                    }
                }
            }

            private class ForeachState
            {
                public ForeachState(GitOdbBackend.foreach_callback_callback cb, IntPtr data)
                {
                    this.cb = cb;
                    this.data = data;
                    this.ManagedCallback = CallbackMethod;
                }

                private int CallbackMethod(byte[] oid)
                {
                    GitOid gitOid = new GitOid();
                    gitOid.Id = oid;

                    return cb(ref gitOid, data);
                }

                public ForEachCallback ManagedCallback;

                private GitOdbBackend.foreach_callback_callback cb;
                private IntPtr data;                
            }
        }

        /// <summary>
        ///   Flags used by subclasses of OdbBackend to indicate which operations they support.
        /// </summary>
        [Flags]
        protected enum OdbBackendOperations
        {
            /// <summary>
            ///   This OdbBackend declares that it supports the Read method.
            /// </summary>
            Read = 1,

            /// <summary>
            ///   This OdbBackend declares that it supports the ReadPrefix method.
            /// </summary>
            ReadPrefix = 2,

            /// <summary>
            ///   This OdbBackend declares that it supports the ReadHeader method.
            /// </summary>
            ReadHeader = 4,

            /// <summary>
            ///   This OdbBackend declares that it supports the Write method.
            /// </summary>
            Write = 8,

            /// <summary>
            ///   This OdbBackend declares that it supports the ReadStream method.
            /// </summary>
            ReadStream = 16,

            /// <summary>
            ///   This OdbBackend declares that it supports the WriteStream method.
            /// </summary>
            WriteStream = 32,

            /// <summary>
            ///   This OdbBackend declares that it supports the Exists method.
            /// </summary>
            Exists = 64,

            /// <summary>
            ///   This OdbBackend declares that it supports the Foreach method.
            /// </summary>
            ForEach = 128,
        }
    }
}
