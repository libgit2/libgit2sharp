using System;
using System.IO;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// A stream that represents a two-way connection (socket) for a SmartSubtransport.
    /// </summary>
    public abstract class SmartSubtransportStream
    {
        /// <summary>
        /// This is to quiet the MetaFixture.TypesInLibGit2SharpMustBeExtensibleInATestingContext test.
        /// Do not use this constructor.
        /// </summary>
        protected internal SmartSubtransportStream()
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Base constructor for SmartTransportStream. Make sure that your derived class calls this base constructor.
        /// </summary>
        /// <param name="subtransport">The subtransport that this stream represents a connection over.</param>
        protected SmartSubtransportStream(SmartSubtransport subtransport)
        {
            this.subtransport = subtransport;
        }

        /// <summary>
        /// Invoked by libgit2 when this stream is no longer needed.
        /// Override this method to add additional cleanup steps to your subclass. Be sure
        /// to call base.Free().
        /// </summary>
        protected virtual void Free()
        {
            if (IntPtr.Zero != nativeStreamPointer)
            {
                GCHandle.FromIntPtr(Marshal.ReadIntPtr(nativeStreamPointer, GitSmartSubtransportStream.GCHandleOffset)).Free();
                Marshal.FreeHGlobal(nativeStreamPointer);
                nativeStreamPointer = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Reads from the transport into the provided <paramref name="dataStream"/> object.
        /// </summary>
        /// <param name="dataStream">The stream to copy the read bytes into.</param>
        /// <param name="length">The number of bytes expected from the underlying transport.</param>
        /// <param name="bytesRead">Receives the number of bytes actually read.</param>
        /// <returns>The error code to propagate back to the native code that requested this operation. 0 is expected, and exceptions may be thrown.</returns>
        public abstract int Read(Stream dataStream, long length, out long bytesRead);

        /// <summary>
        /// Writes the content of a given stream to the transport.
        /// </summary>
        /// <param name="dataStream">The stream with the data to write to the transport.</param>
        /// <param name="length">The number of bytes to read from <paramref name="dataStream"/>.</param>
        /// <returns>The error code to propagate back to the native code that requested this operation. 0 is expected, and exceptions may be thrown.</returns>
        public abstract int Write(Stream dataStream, long length);

        /// <summary>
        /// The smart transport that this stream represents a connection over.
        /// </summary>
        public virtual SmartSubtransport SmartTransport
        {
            get { return this.subtransport; }
        }

        private Exception StoredError { get; set; }

        internal void SetError(Exception ex)
        {
            StoredError = ex;
        }

        private SmartSubtransport subtransport;
        private IntPtr nativeStreamPointer;

        internal IntPtr GitSmartTransportStreamPointer
        {
            get
            {
                if (IntPtr.Zero == nativeStreamPointer)
                {
                    var nativeTransportStream = new GitSmartSubtransportStream();

                    nativeTransportStream.SmartTransport = this.subtransport.GitSmartSubtransportPointer;
                    nativeTransportStream.Read = EntryPoints.ReadCallback;
                    nativeTransportStream.Write = EntryPoints.WriteCallback;
                    nativeTransportStream.Free = EntryPoints.FreeCallback;

                    nativeTransportStream.GCHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
                    nativeStreamPointer = Marshal.AllocHGlobal(Marshal.SizeOf(nativeTransportStream));
                    Marshal.StructureToPtr(nativeTransportStream, nativeStreamPointer, false);
                }

                return nativeStreamPointer;
            }
        }

        private static class EntryPoints
        {
            // Because our GitSmartSubtransportStream structure exists on the managed heap only for a short time (to be marshaled
            // to native memory with StructureToPtr), we need to bind to static delegates. If at construction time
            // we were to bind to the methods directly, that's the same as newing up a fresh delegate every time.
            // Those delegates won't be rooted in the object graph and can be collected as soon as StructureToPtr finishes.
            public static GitSmartSubtransportStream.read_callback ReadCallback = new GitSmartSubtransportStream.read_callback(Read);
            public static GitSmartSubtransportStream.write_callback WriteCallback = new GitSmartSubtransportStream.write_callback(Write);
            public static GitSmartSubtransportStream.free_callback FreeCallback = new GitSmartSubtransportStream.free_callback(Free);

            private static int SetError(SmartSubtransportStream stream, Exception caught)
            {
                Exception ret = (stream.StoredError != null) ? stream.StoredError : caught;
                GitErrorCode errorCode = GitErrorCode.Error;

                if (ret is NativeException)
                {
                    errorCode = ((NativeException)ret).ErrorCode;
                }

                return (int)errorCode;
            }

            private unsafe static int Read(
                IntPtr stream,
                IntPtr buffer,
                UIntPtr buf_size,
                out UIntPtr bytes_read)
            {
                bytes_read = UIntPtr.Zero;

                SmartSubtransportStream transportStream =
                    GCHandle.FromIntPtr(Marshal.ReadIntPtr(stream, GitSmartSubtransportStream.GCHandleOffset)).Target as SmartSubtransportStream;

                if (transportStream == null)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Net, "no transport stream provided");
                    return (int)GitErrorCode.Error;
                }

                if (buf_size.ToUInt64() >= (ulong)long.MaxValue)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Net, "buffer size is too large");
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    using (UnmanagedMemoryStream memoryStream = new UnmanagedMemoryStream((byte*)buffer, 0,
                                                                                          (long)buf_size.ToUInt64(),
                                                                                          FileAccess.ReadWrite))
                    {
                        long longBytesRead;

                        int toReturn = transportStream.Read(memoryStream, (long)buf_size.ToUInt64(), out longBytesRead);

                        bytes_read = new UIntPtr((ulong)Math.Max(0, longBytesRead));

                        return toReturn;
                    }
                }
                catch (Exception ex)
                {
                    return SetError(transportStream, ex);
                }
            }

            private static unsafe int Write(IntPtr stream, IntPtr buffer, UIntPtr len)
            {
                SmartSubtransportStream transportStream =
                    GCHandle.FromIntPtr(Marshal.ReadIntPtr(stream, GitSmartSubtransportStream.GCHandleOffset)).Target as SmartSubtransportStream;

                if (transportStream == null)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Net, "no transport stream provided");
                    return (int)GitErrorCode.Error;
                }

                if (len.ToUInt64() >= (ulong)long.MaxValue)
                {
                    Proxy.git_error_set_str(GitErrorCategory.Net, "write length is too large");
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    long length = (long)len.ToUInt64();

                    using (UnmanagedMemoryStream dataStream = new UnmanagedMemoryStream((byte*)buffer, length))
                    {
                        return transportStream.Write(dataStream, length);
                    }
                }
                catch (Exception ex)
                {
                    return SetError(transportStream, ex);
                }
            }

            private static void Free(IntPtr stream)
            {
                SmartSubtransportStream transportStream =
                    GCHandle.FromIntPtr(Marshal.ReadIntPtr(stream, GitSmartSubtransportStream.GCHandleOffset)).Target as SmartSubtransportStream;

                if (transportStream != null)
                {
                    try
                    {
                        transportStream.Free();
                    }
                    catch (Exception ex)
                    {
                        Proxy.git_error_set_str(GitErrorCategory.Net, ex);
                    }
                }
            }
        }
    }
}
