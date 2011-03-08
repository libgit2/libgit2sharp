using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Wrapper
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct git_reference
    {
        public IntPtr owner;

        [MarshalAs(UnmanagedType.LPStr)]
        public string name;

        public uint type;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct git_reference_oid
    {
        public git_reference @ref;
        public git_oid oid;
        public git_oid peel_target;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct git_reference_symbolic
    {
        public git_reference @ref;

        [MarshalAs(UnmanagedType.LPStr)]
        public string target;
    }

    internal enum git_rtype
    {
        GIT_REF_INVALID = 0, /** Invalid reference */
        GIT_REF_OID = 1, /** A reference which points at an object id */
        GIT_REF_SYMBOLIC = 2, /** A reference which points at another reference */
    }
}