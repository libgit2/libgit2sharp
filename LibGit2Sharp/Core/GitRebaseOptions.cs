using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitRebaseOptions
    {
        public uint version = 1;

        public int quiet;

        public IntPtr rewrite_notes_ref;

        public GitCheckoutOpts checkout_options = new GitCheckoutOpts { version = 1 };
    }
}
