using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal class GitRebaseOptions
    {
        public uint version = 1;

        public int quiet;

        public int inmemory;

        public IntPtr rewrite_notes_ref;

        public GitMergeOpts merge_options = new GitMergeOpts { Version = 1 };

        public GitCheckoutOpts checkout_options = new GitCheckoutOpts { version = 1 };

        private IntPtr padding; // TODO: add git_commit_create_cb

        public NativeMethods.commit_signing_callback signing_callback;
    }
}
