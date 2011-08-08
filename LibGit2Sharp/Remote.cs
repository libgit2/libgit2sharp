using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class Remote
    {
        public Remote(Configuration configuration, string name)
        {
            RemoteSafeHandle handle;
            Ensure.Success(NativeMethods.git_remote_get(out handle, configuration.Handle, name));
            using (handle)
            {
                var ptr = NativeMethods.git_remote_name(handle);
                Name = Marshal.PtrToStringAnsi(ptr);

                ptr = NativeMethods.git_remote_url(handle);
                Url = Marshal.PtrToStringAnsi(ptr);
            }
        }

        public string Name { get; private set; }
        public string Url { get; private set; }
    }
}