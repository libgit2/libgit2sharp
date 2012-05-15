using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitError
    {
        public IntPtr Message;
        public int Klass;
    }
}