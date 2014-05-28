using System;
using System.Linq;
using System.Runtime.InteropServices;

using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// An enumeration of the type of connections which a "smart" subtransport
    /// may be asked to create on behalf of libgit2.
    /// </summary>
    public enum SmartSubtransportAction
    {
        /// <summary>
        ///   For HTTP, this indicates a GET to /info/refs?service=git-upload-pack
        /// </summary>
        UploadPackList = 1,

        /// <summary>
        ///   For HTTP, this indicates a POST to /git-upload-pack
        /// </summary>
        UploadPack = 2,

        /// <summary>
        ///   For HTTP, this indicates a GET to /info/refs?service=git-receive-pack
        /// </summary>
        ReceivePackList = 3,

        /// <summary>
        ///   For HTTP, this indicates a POST to /git-receive-pack
        /// </summary>
        ReceivePack = 4
    }

    /// <summary>
    /// A smart protocol subtransport (git or http).
    /// </summary>
    public abstract class SmartSubtransport
    {
        /// <summary>
        /// When applied to a subclass of SmartSubtransport, indicates that this
        /// custom subtransport has RPC semantics (request/response). HTTP falls into
        /// this category; the git:// protocol, which uses a raw socket, does not.
        /// </summary>
        [AttributeUsage(AttributeTargets.Class)]
        public class RpcSmartSubtransportAttribute : Attribute
        {
        }


        private GitSmartSubtransport nativeSubtransport;
        private IntPtr nativeSubtransportPtr;

        private GitSmartSubtransport.action_callback actionCallback;
        private GitSmartSubtransport.close_callback closeCallback;
        private GitSmartSubtransport.free_callback freeCallback;

        private GitSmartSubtransportDefinition definition;
        private IntPtr definitionPtr;

        internal delegate int SmartSubtransportCallback(out IntPtr subtransportPtr, IntPtr owner);

        /// <summary>
        /// Invoked by libgit2 to create a connection using this subtransport.
        /// </summary>
        /// <param name="url">The endpoint to connect to</param>
        /// <param name="action">The type of connection to create</param>
        /// <returns>A SmartSubtransportStream representing the connection</returns>
        protected abstract SmartSubtransportStream Action(String url, SmartSubtransportAction action);

        /// <summary>
        /// Invoked by libgit2 when this subtransport is no longer needed, but may be re-used in the future.
        /// Override this method to add additional cleanup steps to your subclass. Be sure to call base.Close().
        /// </summary>
        protected virtual void Close()
        {
        }

        /// <summary>
        /// Invoked by libgit2 when this subtransport is being freed. Override this method to add additional
        /// cleanup steps to your subclass.
        /// </summary>
        protected virtual void Free()
        {
        }

        private int ActionCallback(
            out IntPtr streamPtr,
            IntPtr subtransportPtr,
            IntPtr urlPtr,
            SmartSubtransportAction action)
        {
            streamPtr = IntPtr.Zero;

            SmartSubtransport subtransport = GCHandle.FromIntPtr(Marshal.ReadIntPtr(subtransportPtr, GitSmartSubtransport.GCHandleOffset)).Target as SmartSubtransport;
            String url = LaxUtf8Marshaler.FromNative(urlPtr);

            if (subtransport != null && !String.IsNullOrEmpty(url))
            {
                try
                {
                    streamPtr = subtransport.Action(url, action).GitSmartTransportStreamPointer;
                    return 0;
                }
                catch (Exception e)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Net, e);
                }
            }

            return (int)GitErrorCode.Error;
        }

        private int CloseCallback(IntPtr subtransportPtr)
        {
            SmartSubtransport subtransport = GCHandle.FromIntPtr(Marshal.ReadIntPtr(subtransportPtr, GitSmartSubtransport.GCHandleOffset)).Target as SmartSubtransport;

            if (subtransport != null)
            {
                try
                {
                    subtransport.Close();
                    return 0;
                }
                catch (Exception ex)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Net, ex);
                }
            }

            return (int)GitErrorCode.Error;
        }

        private int FreeCallback(IntPtr subtransportPtr)
        {
            SmartSubtransport subtransport = GCHandle.FromIntPtr(Marshal.ReadIntPtr(subtransportPtr, GitSmartSubtransport.GCHandleOffset)).Target as SmartSubtransport;

            if (subtransport != null)
            {
                try
                {
                    subtransport.Free();

                    if (nativeSubtransportPtr != IntPtr.Zero)
                    {
                        GCHandle.FromIntPtr(Marshal.ReadIntPtr(nativeSubtransportPtr, GitSmartSubtransport.GCHandleOffset)).Free();
                        Marshal.FreeHGlobal(nativeSubtransportPtr);
                        nativeSubtransportPtr = IntPtr.Zero;
                    }

                    return 0;
                }
                catch (Exception ex)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Net, ex);
                }
            }

            return (int)GitErrorCode.Error;
        }

        internal IntPtr GetNativeSubtransportPtr()
        {
            if (nativeSubtransportPtr != null)
            {
                actionCallback = new GitSmartSubtransport.action_callback(ActionCallback);
                closeCallback = new GitSmartSubtransport.close_callback(CloseCallback);
                freeCallback = new GitSmartSubtransport.free_callback(FreeCallback);

                nativeSubtransport = new GitSmartSubtransport()
                {
                    Action = actionCallback,
                    Close = closeCallback,
                    Free = freeCallback,
                };

                nativeSubtransport.GCHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
                nativeSubtransportPtr = Marshal.AllocHGlobal(Marshal.SizeOf(nativeSubtransport));
                Marshal.StructureToPtr(nativeSubtransport, nativeSubtransportPtr, false);
            }

            return nativeSubtransportPtr;
        }

        private int CreateNativeSubtransport(out IntPtr subtransportPtr, IntPtr owner)
        {
            subtransportPtr = GetNativeSubtransportPtr();
            return 0;
        }

        internal TransportSafeHandle CreateNativeTransport(RemoteSafeHandle remoteHandle)
        {
            definition = new GitSmartSubtransportDefinition();
            definition.SubtransportCallback = Marshal.GetFunctionPointerForDelegate(new SmartSubtransportCallback(CreateNativeSubtransport));

            if (GetType().GetCustomAttributes(true).Where(s => s.GetType() == typeof(RpcSmartSubtransportAttribute)).FirstOrDefault() != null)
            {
                definition.Rpc = 1;
            }

            definitionPtr = Marshal.AllocHGlobal(Marshal.SizeOf(definition));
            Marshal.StructureToPtr(definition, definitionPtr, false);

            TransportSafeHandle transportHandle = Proxy.git_transport_smart(remoteHandle, definitionPtr);

            if (transportHandle == null)
            {
                Marshal.FreeHGlobal(definitionPtr);
                return null;
            }

            transportHandle.DefinitionPtr = definitionPtr;
            return transportHandle;
        }
    }
}
