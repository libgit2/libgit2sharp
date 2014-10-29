using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// A git filter
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitFilter
    {
        public uint version;

        [MarshalAs(UnmanagedType.LPStr)]
        public string attributes;
    }
}