using System;
using System.IO;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// When an OdbBackend implements the WriteStream or ReadStream methods, it returns an OdbBackendStream to libgit2.
    /// Libgit2 then uses the OdbBackendStream to read or write from the backend in a streaming fashion.
    /// </summary>
    public abstract class OdbBackendStream
    {
        /// <summary>
        /// This is to quiet the MetaFixture.TypesInLibGit2SharpMustBeExtensibleInATestingContext test.
        /// Do not use this constructor.
        /// </summary>
        protected internal OdbBackendStream()
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Base constructor for OdbBackendStream. Make sure that your derived class calls this base constructor.
        /// </summary>
        /// <param name="backend">The backend to which this backend stream is attached.</param>
        protected OdbBackendStream(OdbBackend backend)
        {
            this.backend = backend;
        }

        /// <summary>
        /// Invoked by libgit2 when this stream is no longer needed.
        /// </summary>
        protected virtual void Dispose()
        {
            if (IntPtr.Zero != nativeBackendStreamPointer)
            {
                GCHandle.FromIntPtr(Marshal.ReadIntPtr(nativeBackendStreamPointer, GitOdbBackendStream.GCHandleOffset)).Free();
                Marshal.FreeHGlobal(nativeBackendStreamPointer);
                nativeBackendStreamPointer = IntPtr.Zero;
            }
        }

        /// <summary>
        /// If true, then it is legal to call the Read method.
        /// </summary>
        public abstract bool CanRead { get; }

        /// <summary>
        /// If true, then it is legal to call the Write and FinalizeWrite methods.
        /// </summary>
        public abstract bool CanWrite { get; }

        /// <summary>
        /// Requests that the stream write the next length bytes of the stream to the provided Stream object.
        /// </summary>
        public abstract int Read(Stream dataStream, long length);

        /// <summary>
        /// Requests that the stream write the first length bytes of the provided Stream object to the stream.
        /// </summary>
        public abstract int Write(Stream dataStream, long length);

        /// <summary>
        /// After all bytes have been written to the stream, the object ID is provided to FinalizeWrite.
        /// </summary>
        public abstract int FinalizeWrite(ObjectId id);

        /// <summary>
        /// The backend object this stream was created by.
        /// </summary>
        public virtual OdbBackend Backend
        {
            get { return this.backend; }
        }

        private readonly OdbBackend backend;
        private IntPtr nativeBackendStreamPointer;

        internal IntPtr GitOdbBackendStreamPointer
        {
            get
            {
                if (IntPtr.Zero == nativeBackendStreamPointer)
                {
                    var nativeBackendStream = new GitOdbBackendStream();

                    nativeBackendStream.Backend = this.backend.GitOdbBackendPointer;
                    nativeBackendStream.Free = BackendStreamEntryPoints.FreeCallback;

                    if (CanRead)
                    {
                        nativeBackendStream.Read = BackendStreamEntryPoints.ReadCallback;

                        nativeBackendStream.Mode |= GitOdbBackendStreamMode.Read;
                    }

                    if (CanWrite)
                    {
                        nativeBackendStream.Write = BackendStreamEntryPoints.WriteCallback;
                        nativeBackendStream.FinalizeWrite = BackendStreamEntryPoints.FinalizeWriteCallback;

                        nativeBackendStream.Mode |= GitOdbBackendStreamMode.Write;
                    }

                    nativeBackendStream.GCHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
                    nativeBackendStreamPointer = Marshal.AllocHGlobal(Marshal.SizeOf(nativeBackendStream));
                    Marshal.StructureToPtr(nativeBackendStream, nativeBackendStreamPointer, false);
                }

                return nativeBackendStreamPointer;
            }
        }

        private static class BackendStreamEntryPoints
        {
            // Because our GitOdbBackendStream structure exists on the managed heap only for a short time (to be marshaled
            // to native memory with StructureToPtr), we need to bind to static delegates. If at construction time
            // we were to bind to the methods directly, that's the same as newing up a fresh delegate every time.
            // Those delegates won't be rooted in the object graph and can be collected as soon as StructureToPtr finishes.
            public static readonly GitOdbBackendStream.read_callback ReadCallback = Read;
            public static readonly GitOdbBackendStream.write_callback WriteCallback = Write;
            public static readonly GitOdbBackendStream.finalize_write_callback FinalizeWriteCallback = FinalizeWrite;
            public static readonly GitOdbBackendStream.free_callback FreeCallback = Free;

            private unsafe static int Read(IntPtr stream, IntPtr buffer, UIntPtr len)
            {
                OdbBackendStream odbBackendStream = GCHandle.FromIntPtr(Marshal.ReadIntPtr(stream, GitOdbBackendStream.GCHandleOffset)).Target as OdbBackendStream;

                if (odbBackendStream != null)
                {
                    long length = OdbBackend.ConverToLong(len);

                    using (UnmanagedMemoryStream memoryStream = new UnmanagedMemoryStream((byte*)buffer, 0, length, FileAccess.ReadWrite))
                    {
                        try
                        {
                            return odbBackendStream.Read(memoryStream, length);
                        }
                        catch (Exception ex)
                        {
                            Proxy.giterr_set_str(GitErrorCategory.Odb, ex);
                        }
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static unsafe int Write(IntPtr stream, IntPtr buffer, UIntPtr len)
            {
                OdbBackendStream odbBackendStream = GCHandle.FromIntPtr(Marshal.ReadIntPtr(stream, GitOdbBackendStream.GCHandleOffset)).Target as OdbBackendStream;

                if (odbBackendStream != null)
                {
                    long length = OdbBackend.ConverToLong(len);

                    using (UnmanagedMemoryStream dataStream = new UnmanagedMemoryStream((byte*)buffer, length))
                    {
                        try
                        {
                            return odbBackendStream.Write(dataStream, length);
                        }
                        catch (Exception ex)
                        {
                            Proxy.giterr_set_str(GitErrorCategory.Odb, ex);
                        }
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static int FinalizeWrite(IntPtr stream, ref GitOid oid)
            {
                OdbBackendStream odbBackendStream = GCHandle.FromIntPtr(Marshal.ReadIntPtr(stream, GitOdbBackendStream.GCHandleOffset)).Target as OdbBackendStream;

                if (odbBackendStream != null)
                {
                    try
                    {
                        return odbBackendStream.FinalizeWrite(new ObjectId(oid));
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Odb, ex);
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static void Free(IntPtr stream)
            {
                OdbBackendStream odbBackendStream = GCHandle.FromIntPtr(Marshal.ReadIntPtr(stream, GitOdbBackendStream.GCHandleOffset)).Target as OdbBackendStream;

                if (odbBackendStream != null)
                {
                    try
                    {
                        odbBackendStream.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Odb, ex);
                    }
                }
            }
        }
    }
}
