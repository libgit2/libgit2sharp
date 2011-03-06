using System.Runtime.InteropServices;

namespace LibGit2Sharp.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct git_oid
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.GIT_OID_RAWSZ, ArraySubType = UnmanagedType.U1)]
        public byte[] id;
    }
}