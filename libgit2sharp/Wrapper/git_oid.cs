using System.Runtime.InteropServices;

namespace libgit2sharp.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct git_oid
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Constants.GIT_OID_RAWSZ, ArraySubType = UnmanagedType.U1)]
        public byte[] id;
    }
}