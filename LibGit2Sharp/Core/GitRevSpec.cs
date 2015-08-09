using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitRevSpec
    {
        public IntPtr From;

        public IntPtr To;

        public RevSpecType Type;
    }
}