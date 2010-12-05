using System;
using System.Runtime.InteropServices;

namespace libgit2sharp.Wrapper
{
    internal static class LibGit2Api
    {
        private const string Libgit2 = "libgit2wrap.dll";

        [DllImport(Libgit2)]
        public static extern OperationResult wrapped_git_repository_open(out IntPtr repoPtr, [In] string path);

        [DllImport(Libgit2)]
        public static extern OperationResult wrapped_git_repository_open2(out IntPtr repoPtr, [In] string gitDir, [In] string gitObjectDirectory, [In] string gitIndexFile, [In] string gitWorkTree);

        [DllImport(Libgit2)]
        public static extern void wrapped_git_repository_free([In] IntPtr repository);

        [DllImport(Libgit2)]
        public static extern bool wrapped_git_odb_exists([In] IntPtr repository, [In] string objectId);

        [DllImport(Libgit2)]
        public static extern OperationResult wrapped_git_odb_read_header(out git_rawobj rawObject, [In] IntPtr repository, [In] string objectId);

        [DllImport(Libgit2)]
        public static extern OperationResult wrapped_git_odb_read(out git_rawobj rawObject, [In] IntPtr repository, [In] string objectId);
    }
}