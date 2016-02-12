using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// An enumeration of the type of connections which a "smart" subtransport
    /// may be asked to create on behalf of libgit2.
    /// </summary>
    public enum GitSmartSubtransportAction
    {
        /// <summary>
        /// For HTTP, this indicates a GET to /info/refs?service=git-upload-pack
        /// </summary>
        UploadPackList = 1,

        /// <summary>
        /// For HTTP, this indicates a POST to /git-upload-pack
        /// </summary>
        UploadPack = 2,

        /// <summary>
        /// For HTTP, this indicates a GET to /info/refs?service=git-receive-pack
        /// </summary>
        ReceivePackList = 3,

        /// <summary>
        /// For HTTP, this indicates a POST to /git-receive-pack
        /// </summary>
        ReceivePack = 4
    }

    /// <summary>
    /// Base class for custom RPC-based subtransports that use the standard
    /// "smart" git protocol.  RPC-based subtransports perform work over
    /// multiple requests, like the http transport.
    /// </summary>
    public abstract class RpcSmartSubtransport : SmartSubtransport
    {
    }

    /// <summary>
    /// Base class for typical custom subtransports for the "smart"
    /// transport that work over a single connection, like the git and ssh
    /// transports.
    /// </summary>
    public abstract class SmartSubtransport
    {
        /// <summary>
        /// Invoked by libgit2 to create a connection using this subtransport.
        /// </summary>
        /// <param name="url">The endpoint to connect to</param>
        /// <param name="action">The type of connection to create</param>
        /// <returns>A SmartSubtransportStream representing the connection</returns>
        protected abstract SmartSubtransportStream Action(String url, GitSmartSubtransportAction action);

        /// <summary>
        /// Invoked by libgit2 when this subtransport is no longer needed, but may be re-used in the future.
        /// Override this method to add additional cleanup steps to your subclass. Be sure to call base.Close().
        /// </summary>
        protected virtual void Close()
        { }

        /// <summary>
        /// Invoked by libgit2 when this subtransport is being freed. Override this method to add additional
        /// cleanup steps to your subclass. Be sure to call base.Dispose().
        /// </summary>
        protected virtual void Dispose()
        {
            Close();

            if (IntPtr.Zero != nativeSubtransportPointer)
            {
                GCHandle.FromIntPtr(Marshal.ReadIntPtr(nativeSubtransportPointer, GitSmartSubtransport.GCHandleOffset)).Free();
                Marshal.FreeHGlobal(nativeSubtransportPointer);
                nativeSubtransportPointer = IntPtr.Zero;
            }
        }

        private IntPtr nativeSubtransportPointer;

        internal IntPtr GitSmartSubtransportPointer
        {
            get
            {
                if (IntPtr.Zero == nativeSubtransportPointer)
                {
                    var nativeTransport = new GitSmartSubtransport();

                    nativeTransport.Action = EntryPoints.ActionCallback;
                    nativeTransport.Close = EntryPoints.CloseCallback;
                    nativeTransport.Free = EntryPoints.FreeCallback;

                    nativeTransport.GCHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
                    nativeSubtransportPointer = Marshal.AllocHGlobal(Marshal.SizeOf(nativeTransport));
                    Marshal.StructureToPtr(nativeTransport, nativeSubtransportPointer, false);
                }

                return nativeSubtransportPointer;
            }
        }

        private static class EntryPoints
        {
            // Because our GitSmartSubtransport structure exists on the managed heap only for a short time (to be marshaled
            // to native memory with StructureToPtr), we need to bind to static delegates. If at construction time
            // we were to bind to the methods directly, that's the same as newing up a fresh delegate every time.
            // Those delegates won't be rooted in the object graph and can be collected as soon as StructureToPtr finishes.
            public static GitSmartSubtransport.action_callback ActionCallback = new GitSmartSubtransport.action_callback(Action);
            public static GitSmartSubtransport.close_callback CloseCallback = new GitSmartSubtransport.close_callback(Close);
            public static GitSmartSubtransport.free_callback FreeCallback = new GitSmartSubtransport.free_callback(Free);

            private static int Action(
                out IntPtr stream,
                IntPtr subtransport,
                IntPtr url,
                GitSmartSubtransportAction action)
            {
                stream = IntPtr.Zero;

                SmartSubtransport t = GCHandle.FromIntPtr(Marshal.ReadIntPtr(subtransport, GitSmartSubtransport.GCHandleOffset)).Target as SmartSubtransport;
                String urlAsString = LaxUtf8Marshaler.FromNative(url);

                if (null != t &&
                    !String.IsNullOrEmpty(urlAsString))
                {
                    try
                    {
                        stream = t.Action(urlAsString, action).GitSmartTransportStreamPointer;

                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Net, ex);
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static int Close(IntPtr subtransport)
            {
                SmartSubtransport t = GCHandle.FromIntPtr(Marshal.ReadIntPtr(subtransport, GitSmartSubtransport.GCHandleOffset)).Target as SmartSubtransport;

                if (null != t)
                {
                    try
                    {
                        t.Close();

                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Proxy.giterr_set_str(GitErrorCategory.Net, ex);
                    }
                }

                return (int)GitErrorCode.Error;
            }

            private static void Free(IntPtr subtransport)
            {
                SmartSubtransport t = GCHandle.FromIntPtr(Marshal.ReadIntPtr(subtransport, GitSmartSubtransport.GCHandleOffset)).Target as SmartSubtransport;

                if (null != t)
                {
                    try
                    {
                        t.Dispose();
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
