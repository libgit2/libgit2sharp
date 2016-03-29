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
        /// Requests that the stream write the next length bytes of the stream to the provided Stream object.
        /// </summary>
        public abstract int Read(Stream dataStream, long length, out long bytesRead);

        /// <summary>
        /// Requests that the stream write the first length bytes of the provided Stream object to the stream.
        /// </summary>
        public abstract int Write(Stream dataStream, long length);

        /// <summary>
        /// The smart transport that this stream represents a connection over.
        /// </summary>
        public virtual SmartSubtransport SmartTransport
        {
            get { return this.subtransport; }
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

            private unsafe static int Read(
                IntPtr stream,
                IntPtr buffer,
                UIntPtr buf_size,
                out UIntPtr bytes_read)
            {
                bytes_read = UIntPtr.Zero;

                SmartSubtransportStream transportStream =
                    GCHandle.FromIntPtr(Marshal.ReadIntPtr(stream, GitSmartSubtransportStream.GCHandleOffset)).Target as SmartSubtransportStream;

                if (transportStream != null &&
                    buf_size.ToUInt64() < (ulong)long.MaxValue)
                {
                    using (UnmanagedMemoryStream memoryStream = new UnmanagedMemoryStream((byte*)buffer, 0,
                                                                                          (long)buf_size.ToUInt64(),
                                                                                          FileAccess.ReadWrite))
                    {
                        try
                        {
                            long longBytesRead;

                            int toReturn = transportStream.Read(memoryStream, (long)buf_size.ToUInt64(), out longBytesRead);

                            bytes_read = new UIntPtr((ulong)Math.Max(0, longBytesRead));

                            return toReturn;
                        }
                        catch (Exception ex)
                        {
                            Proxy.giterr_set_str(GitErrorCategory.Net, ex);
                        }
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static unsafe int Write(IntPtr stream, IntPtr buffer, UIntPtr len)
            {
                SmartSubtransportStream transportStream =
                    GCHandle.FromIntPtr(Marshal.ReadIntPtr(stream, GitSmartSubtransportStream.GCHandleOffset)).Target as SmartSubtransportStream;

                if (transportStream != null && len.ToUInt64() < (ulong)long.MaxValue)
                {
                    long length = (long)len.ToUInt64();

                    using (UnmanagedMemoryStream dataStream = new UnmanagedMemoryStream((byte*)buffer, length))
                    {
                        try
                        {
                            return transportStream.Write(dataStream, length);
                        }
                        catch (Exception ex)
                        {
                            Proxy.giterr_set_str(GitErrorCategory.Net, ex);
                        }
                    }
                }

                return (int)GitErrorCode.Error;
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
                        Proxy.giterr_set_str(GitErrorCategory.Net, ex);
                    }
                }
            }
        }
    }
}
