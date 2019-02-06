using System;
using System.Runtime.InteropServices;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

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
        internal IntPtr Transport { get; set; }

        /// <summary>
        /// Call the certificate check callback
        /// </summary>
        /// <param name="cert">The certificate to send</param>
        /// <param name="valid">Whether we consider the certificate to be valid</param>
        /// <param name="hostname">The hostname we connected to</param>
        public int CertificateCheck(Certificate cert, bool valid, string hostname)
        {
            CertificateSsh sshCert = cert as CertificateSsh;
            CertificateX509 x509Cert = cert as CertificateX509;

            if (sshCert == null && x509Cert == null)
            {
                throw new InvalidOperationException("Unsupported certificate type");
            }

            int ret;
            if (sshCert != null)
            {
                var certPtr = sshCert.ToPointer();
                ret = NativeMethods.git_transport_smart_certificate_check(Transport, certPtr, valid ? 1 : 0, hostname);
                Marshal.FreeHGlobal(certPtr);
            } else {
                IntPtr certPtr, dataPtr;
                certPtr = x509Cert.ToPointers(out dataPtr);
                ret = NativeMethods.git_transport_smart_certificate_check(Transport, certPtr, valid ? 1 : 0, hostname);
                Marshal.FreeHGlobal(dataPtr);
                Marshal.FreeHGlobal(certPtr);
            }

            return ret;
        }

        public int AcquireCredentials(out Credentials cred, string user, params Type[] methods)
        {
            // Convert the user-provided types to libgit2's flags
            int allowed = 0;
            foreach (var method in methods)
            {
                if (method == typeof(UsernamePasswordCredentials))
                {
                    allowed |= (int)GitCredentialType.UserPassPlaintext;
                }
                else
                {
                    throw new InvalidOperationException("Unknown type passes as allowed credential");
                }
            }

            IntPtr credHandle = IntPtr.Zero;
            int res = Proxy.git_transport_smart_credentials(out credHandle, Transport, user, allowed);
            if (res != 0)
            {
                cred = null;
                return res;
            }

            if (credHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("creditals callback indicated success but returned no credentials");
            }

            unsafe
            {
                var baseCred = (GitCredential*) credHandle;
                switch (baseCred->credtype)
                {
                    case GitCredentialType.UserPassPlaintext:
                        cred = UsernamePasswordCredentials.FromNative((GitCredentialUserpass*) credHandle);
                        return 0;
                    default:
                        throw new InvalidOperationException("User returned an unkown credential type");
                }
            }
        }

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
                        Proxy.git_error_set_str(GitErrorCategory.Net, ex);
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
                        Proxy.git_error_set_str(GitErrorCategory.Net, ex);
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
                        Proxy.git_error_set_str(GitErrorCategory.Net, ex);
                    }
                }
            }
        }
    }
}
