using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal class RemoteSafeHandle : SafeHandleBase
    {
        internal TransportSafeHandle TransportHandle { get; set; }

        protected override bool ReleaseHandleImpl()
        {
            Proxy.git_remote_free(handle);

            if (TransportHandle != null)
            {
                Marshal.FreeHGlobal(TransportHandle.DefinitionPtr);
                TransportHandle = null;
            }

            return true;
        }
    }
}
