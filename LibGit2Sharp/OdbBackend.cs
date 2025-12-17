using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Base class for all custom managed backends for the libgit2 object database (ODB).
    /// <para>
    /// If the derived backend implements <see cref="IDisposable"/>, the <see cref="IDisposable.Dispose"/>
    /// method will be honored and invoked upon the disposal of the repository.
    /// </para>
    /// </summary>
    public abstract class OdbBackend
    {
        /// <summary>
        /// Invoked by libgit2 when this backend is no longer needed.
        /// </summary>
        internal void Free()
        {
            if (nativeBackendPointer == IntPtr.Zero)
            {
                return;
            }

            GCHandle.FromIntPtr(Marshal.ReadIntPtr(nativeBackendPointer, GitOdbBackend.GCHandleOffset)).Free();
            Marshal.FreeHGlobal(nativeBackendPointer);
            nativeBackendPointer = IntPtr.Zero;
        }

        /// <summary>
        /// In your subclass, override this member to provide the list of actions your backend supports.
        /// </summary>
        protected abstract OdbBackendOperations SupportedOperations { get; }

        /// <summary>
        /// Call this method from your implementations of Read and ReadPrefix to allocate a buffer in
        /// which to return the object's data.
        /// </summary>
        /// <param name="bytes">The bytes to be copied to the stream.</param>
        /// <returns>
        /// A Stream already filled with the content of provided the byte array.
        /// Do not dispose this object before returning it.
        /// </returns>
        protected UnmanagedMemoryStream AllocateAndBuildFrom(byte[] bytes)
        {
            var stream = Allocate(bytes.Length);

            stream.Write(bytes, 0, bytes.Length);

            return stream;
        }

        /// <summary>
        /// Call this method from your implementations of Read and ReadPrefix to allocate a buffer in
        /// which to return the object's data.
        /// </summary>
        /// <param name="size">Number of bytes to allocate</param>
        /// <returns>A Stream for you to write to and then return. Do not dispose this object before returning it.</returns>
        protected unsafe UnmanagedMemoryStream Allocate(long size)
        {
            if (size < 0 || (UIntPtr.Size == sizeof(int) && size > int.MaxValue))
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            IntPtr buffer = Proxy.git_odb_backend_malloc(this.GitOdbBackendPointer, new UIntPtr((ulong)size));

            return new UnmanagedMemoryStream((byte*)buffer, 0, size, FileAccess.ReadWrite);
        }

        /// <summary>
        /// Requests that this backend read an object.
        /// </summary>
        public abstract int Read(
            ObjectId id,
            out UnmanagedMemoryStream data,
            out ObjectType objectType);

        /// <summary>
        /// Requests that this backend read an object. The object ID may not be complete (may be a prefix).
        /// </summary>
        public abstract int ReadPrefix(
            string shortSha,
            out ObjectId oid,
            out UnmanagedMemoryStream data,
            out ObjectType objectType);

        /// <summary>
        /// Requests that this backend read an object's header (length and object type) but not its contents.
        /// </summary>
        public abstract int ReadHeader(
            ObjectId id,
            out int length,
            out ObjectType objectType);

        /// <summary>
        /// Requests that this backend write an object to the backing store.
        /// </summary>
        public abstract int Write(
            ObjectId id,
            Stream dataStream,
            long length,
            ObjectType objectType);

        /// <summary>
        /// Requests that this backend read an object. Returns a stream so that the caller can read the data in chunks.
        /// </summary>
        public abstract int ReadStream(
            ObjectId id,
            out OdbBackendStream stream);

        /// <summary>
        /// Requests that this backend write an object to the backing store. Returns a stream so that the caller can write
        /// the data in chunks.
        /// </summary>
        public abstract int WriteStream(
            long length,
            ObjectType objectType,
            out OdbBackendStream stream);

        /// <summary>
        /// Requests that this backend check if an object ID exists.
        /// </summary>
        public abstract bool Exists(ObjectId id);

        /// <summary>
        /// Requests that this backend check if an object ID exists. The object ID may not be complete (may be a prefix).
        /// </summary>
        public abstract int ExistsPrefix(string shortSha, out ObjectId found);

        /// <summary>
        /// Requests that this backend enumerate all items in the backing store.
        /// </summary>
        public abstract int ForEach(ForEachCallback callback);

        /// <summary>
        /// The signature of the callback method provided to the Foreach method.
        /// </summary>
        /// <param name="oid">The object ID of the object in the backing store.</param>
        /// <returns>A non-negative result indicates the enumeration should continue. Otherwise, the enumeration should stop.</returns>
        public delegate int ForEachCallback(ObjectId oid);

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

                    if ((supportedOperations & OdbBackendOperations.ExistsPrefix) != 0)
                    {
                        nativeBackend.ExistsPrefix = BackendEntryPoints.ExistsPrefixCallback;
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
            public static readonly GitOdbBackend.read_callback ReadCallback = Read;
            public static readonly GitOdbBackend.read_prefix_callback ReadPrefixCallback = ReadPrefix;
            public static readonly GitOdbBackend.read_header_callback ReadHeaderCallback = ReadHeader;
            public static readonly GitOdbBackend.readstream_callback ReadStreamCallback = ReadStream;
            public static readonly GitOdbBackend.write_callback WriteCallback = Write;
            public static readonly GitOdbBackend.writestream_callback WriteStreamCallback = WriteStream;
            public static readonly GitOdbBackend.exists_callback ExistsCallback = Exists;
            public static readonly GitOdbBackend.exists_prefix_callback ExistsPrefixCallback = ExistsPrefix;
            public static readonly GitOdbBackend.foreach_callback ForEachCallback = Foreach;
            public static readonly GitOdbBackend.free_callback FreeCallback = Free;

            private static OdbBackend MarshalOdbBackend(IntPtr backendPtr)
            {

                var intPtr = Marshal.ReadIntPtr(backendPtr, GitOdbBackend.GCHandleOffset);
                var odbBackend = GCHandle.FromIntPtr(intPtr).Target as OdbBackend;

                if (odbBackend == null)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Reference, "Cannot retrieve the managed OdbBackend.");
                    return null;
                }

                return odbBackend;
            }

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

                OdbBackend odbBackend = MarshalOdbBackend(backend);
                if (odbBackend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                UnmanagedMemoryStream memoryStream = null;

                try
                {
                    ObjectType objectType;
                    int toReturn = odbBackend.Read(new ObjectId(oid), out memoryStream, out objectType);

                    if (toReturn != 0)
                    {
                        return toReturn;
                    }

                    if (memoryStream == null)
                    {
                        return (int)GitErrorCode.Error;
                    }

                    len_p = new UIntPtr((ulong)memoryStream.Capacity);
                    type_p = objectType.ToGitObjectType();

                    memoryStream.Seek(0, SeekOrigin.Begin);
                    buffer_p = new IntPtr(memoryStream.PositionPointer);

                }
                catch (Exception ex)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Odb, ex);
                    return (int)GitErrorCode.Error;
                }
                finally
                {
                    if (memoryStream != null)
                    {
                        memoryStream.Dispose();
                    }
                }

                return (int)GitErrorCode.Ok;
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

                OdbBackend odbBackend = MarshalOdbBackend(backend);
                if (odbBackend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                UnmanagedMemoryStream memoryStream = null;

                try
                {
                    var shortSha = ObjectId.ToString(short_oid.Id, (int)len);

                    ObjectId oid;
                    ObjectType objectType;

                    int toReturn = odbBackend.ReadPrefix(shortSha, out oid, out memoryStream, out objectType);

                    if (toReturn != 0)
                    {
                        return toReturn;
                    }

                    if (memoryStream == null)
                    {
                        return (int)GitErrorCode.Error;
                    }

                    out_oid.Id = oid.RawId;
                    len_p = new UIntPtr((ulong)memoryStream.Capacity);
                    type_p = objectType.ToGitObjectType();

                    memoryStream.Seek(0, SeekOrigin.Begin);
                    buffer_p = new IntPtr(memoryStream.PositionPointer);
                }
                catch (Exception ex)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Odb, ex);
                    return (int)GitErrorCode.Error;
                }
                finally
                {
                    if (memoryStream != null)
                    {
                        memoryStream.Dispose();
                    }
                }

                return (int)GitErrorCode.Ok;
            }

            private static int ReadHeader(
                out UIntPtr len_p,
                out GitObjectType type_p,
                IntPtr backend,
                ref GitOid oid)
            {
                len_p = UIntPtr.Zero;
                type_p = GitObjectType.Bad;

                OdbBackend odbBackend = MarshalOdbBackend(backend);
                if (odbBackend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    int length;
                    ObjectType objectType;
                    int toReturn = odbBackend.ReadHeader(new ObjectId(oid), out length, out objectType);

                    if (toReturn != 0)
                    {
                        return toReturn;
                    }

                    len_p = new UIntPtr((uint)length);
                    type_p = objectType.ToGitObjectType();
                }
                catch (Exception ex)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Odb, ex);
                    return (int)GitErrorCode.Error;
                }

                return (int)GitErrorCode.Ok;
            }

            private static unsafe int Write(
                IntPtr backend,
                ref GitOid oid,
                IntPtr data,
                UIntPtr len,
                GitObjectType type)
            {
                long length = ConverToLong(len);

                OdbBackend odbBackend = MarshalOdbBackend(backend);
                if (odbBackend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    using (var stream = new UnmanagedMemoryStream((byte*)data, length))
                    {
                        return odbBackend.Write(new ObjectId(oid), stream, length, type.ToObjectType());
                    }
                }
                catch (Exception ex)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Odb, ex);
                    return (int)GitErrorCode.Error;
                }
            }

            private static int WriteStream(
                out IntPtr stream_out,
                IntPtr backend,
                long len,
                GitObjectType type)
            {
                stream_out = IntPtr.Zero;

                OdbBackend odbBackend = MarshalOdbBackend(backend);
                if (odbBackend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                ObjectType objectType = type.ToObjectType();

                try
                {
                    OdbBackendStream stream;
                    int toReturn = odbBackend.WriteStream(len, objectType, out stream);

                    if (toReturn == 0)
                    {
                        stream_out = stream.GitOdbBackendStreamPointer;
                    }

                    return toReturn;
                }
                catch (Exception ex)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Odb, ex);
                    return (int)GitErrorCode.Error;
                }
            }

            private static int ReadStream(
                out IntPtr stream_out,
                IntPtr backend,
                ref GitOid oid)
            {
                stream_out = IntPtr.Zero;

                OdbBackend odbBackend = MarshalOdbBackend(backend);
                if (odbBackend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    OdbBackendStream stream;
                    int toReturn = odbBackend.ReadStream(new ObjectId(oid), out stream);

                    if (toReturn == 0)
                    {
                        stream_out = stream.GitOdbBackendStreamPointer;
                    }

                    return toReturn;
                }
                catch (Exception ex)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Odb, ex);
                    return (int)GitErrorCode.Error;
                }
            }

            private static bool Exists(
                IntPtr backend,
                ref GitOid oid)
            {
                OdbBackend odbBackend = MarshalOdbBackend(backend);
                if (odbBackend == null)
                {
                    return false; // Weird
                }

                try
                {
                    return odbBackend.Exists(new ObjectId(oid));
                }
                catch (Exception ex)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Odb, ex);
                    return false;
                }
            }

            private static int ExistsPrefix(
                ref GitOid found_oid,
                IntPtr backend,
                ref GitOid short_oid,
                UIntPtr len)
            {
                OdbBackend odbBackend = MarshalOdbBackend(backend);
                if (odbBackend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    ObjectId found;
                    var shortSha = ObjectId.ToString(short_oid.Id, (int)len);

                    found_oid.Id = ObjectId.Zero.RawId;
                    int result = odbBackend.ExistsPrefix(shortSha, out found);

                    if (result == (int)GitErrorCode.Ok)
                    {
                        found_oid.Id = found.RawId;
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Odb, ex);
                    return (int)GitErrorCode.Error;
                }
            }

            private static int Foreach(
                IntPtr backend,
                GitOdbBackend.foreach_callback_callback cb,
                IntPtr data)
            {
                OdbBackend odbBackend = MarshalOdbBackend(backend);
                if (odbBackend == null)
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    return odbBackend.ForEach(new ForeachState(cb, data).ManagedCallback);
                }
                catch (Exception ex)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Odb, ex);
                    return (int)GitErrorCode.Error;
                }
            }

            private static void Free(
                IntPtr backend)
            {
                OdbBackend odbBackend = MarshalOdbBackend(backend);
                if (odbBackend == null)
                {
                    return;
                }

                try
                {
                    odbBackend.Free();

                    var disposable = odbBackend as IDisposable;

                    if (disposable == null)
                    {
                        return;
                    }

                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Odb, ex);
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

                private unsafe int CallbackMethod(ObjectId id)
                {
                    var oid = id.RawId;

                    fixed (void* ptr = &oid[0])
                    {
                        return cb(new IntPtr(ptr), data);
                    }
                }

                public readonly ForEachCallback ManagedCallback;

                private readonly GitOdbBackend.foreach_callback_callback cb;
                private readonly IntPtr data;
            }
        }

        internal static long ConverToLong(UIntPtr len)
        {
            if (len.ToUInt64() > long.MaxValue)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                  "Provided length ({0}) exceeds long.MaxValue ({1}).",
                                                                  len.ToUInt64(),
                                                                  long.MaxValue));
            }

            return (long)len.ToUInt64();
        }

        /// <summary>
        /// Flags used by subclasses of OdbBackend to indicate which operations they support.
        /// </summary>
        [Flags]
        protected enum OdbBackendOperations
        {
            /// <summary>
            /// This OdbBackend declares that it supports the Read method.
            /// </summary>
            Read = 1,

            /// <summary>
            /// This OdbBackend declares that it supports the ReadPrefix method.
            /// </summary>
            ReadPrefix = 2,

            /// <summary>
            /// This OdbBackend declares that it supports the ReadHeader method.
            /// </summary>
            ReadHeader = 4,

            /// <summary>
            /// This OdbBackend declares that it supports the Write method.
            /// </summary>
            Write = 8,

            /// <summary>
            /// This OdbBackend declares that it supports the ReadStream method.
            /// </summary>
            ReadStream = 16,

            /// <summary>
            /// This OdbBackend declares that it supports the WriteStream method.
            /// </summary>
            WriteStream = 32,

            /// <summary>
            /// This OdbBackend declares that it supports the Exists method.
            /// </summary>
            Exists = 64,

            /// <summary>
            /// This OdbBackend declares that it supports the ExistsPrefix method.
            /// </summary>
            ExistsPrefix = 128,

            /// <summary>
            /// This OdbBackend declares that it supports the Foreach method.
            /// </summary>
            ForEach = 256,
        }

        /// <summary>
        /// Libgit2 expected backend return codes.
        /// </summary>
        protected enum ReturnCode
        {
            /// <summary>
            /// No error has occured.
            /// </summary>
            GIT_OK = 0,

            /// <summary>
            /// No object matching the oid, or short oid, can be found in the backend.
            /// </summary>
            GIT_ENOTFOUND = -3,

            /// <summary>
            /// The given short oid is ambiguous.
            /// </summary>
            GIT_EAMBIGUOUS = -5,

            /// <summary>
            /// Interruption of the foreach() callback is requested.
            /// </summary>
            GIT_EUSER = -7,
        }
    }
}
