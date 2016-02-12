using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// An object representing the registration of a SmartSubtransport type with libgit2
    /// under a particular scheme (eg "http").
    /// </summary>
    /// <typeparam name="T">The type of SmartSubtransport to register</typeparam>
    public sealed class SmartSubtransportRegistration<T>
        where T : SmartSubtransport, new()
    {
        /// <summary>
        /// Creates a new native registration for a smart protocol transport
        /// in libgit2.
        /// </summary>
        /// <param name="scheme">The URL scheme (eg "http") to register</param>
        internal SmartSubtransportRegistration(string scheme)
        {
            Scheme = scheme;
            RegistrationPointer = CreateRegistrationPointer();
            FunctionPointer = CreateFunctionPointer();
        }

        /// <summary>
        /// The URI scheme (eg "http") for this transport.
        /// </summary>
        public string Scheme { get; private set; }

        internal IntPtr RegistrationPointer { get; private set; }

        internal IntPtr FunctionPointer { get; private set; }

        private IntPtr CreateRegistrationPointer()
        {
            var registration = new GitSmartSubtransportRegistration();

            registration.SubtransportCallback = Marshal.GetFunctionPointerForDelegate(EntryPoints.SubtransportCallback);
            registration.Rpc = typeof(RpcSmartSubtransport).IsAssignableFrom(typeof(T)) ? (uint)1 : (uint)0;

            var registrationPointer = Marshal.AllocHGlobal(Marshal.SizeOf(registration));
            Marshal.StructureToPtr(registration, registrationPointer, false);

            return registrationPointer;
        }

        private IntPtr CreateFunctionPointer()
        {
            return Marshal.GetFunctionPointerForDelegate(EntryPoints.TransportCallback);
        }

        internal void Free()
        {
            Marshal.FreeHGlobal(RegistrationPointer);
            RegistrationPointer = IntPtr.Zero;
        }

        private static class EntryPoints
        {
            // Because our GitSmartSubtransportRegistration structure exists on the managed heap only for a short time (to be marshaled
            // to native memory with StructureToPtr), we need to bind to static delegates. If at construction time
            // we were to bind to the methods directly, that's the same as newing up a fresh delegate every time.
            // Those delegates won't be rooted in the object graph and can be collected as soon as StructureToPtr finishes.
            public static GitSmartSubtransportRegistration.create_callback SubtransportCallback = new GitSmartSubtransportRegistration.create_callback(Subtransport);
            public static NativeMethods.git_transport_cb TransportCallback = new NativeMethods.git_transport_cb(Transport);

            private static int Subtransport(
                out IntPtr subtransport,
                IntPtr transport,
                IntPtr payload)
            {
                subtransport = IntPtr.Zero;

                try
                {
                    subtransport = new T().GitSmartSubtransportPointer;

                    return 0;
                }
                catch (Exception ex)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Net, ex);
                }

                return (int)GitErrorCode.Error;
            }

            private static int Transport(
                out IntPtr transport,
                IntPtr remote,
                IntPtr payload)
            {
                transport = IntPtr.Zero;

                try
                {
                    return NativeMethods.git_transport_smart(out transport, remote, payload);
                }
                catch (Exception ex)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Net, ex);
                }

                return (int)GitErrorCode.Error;
            }
        }
    }
}
