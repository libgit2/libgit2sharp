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
        public static extern OperationResult wrapped_git_repository_lookup(out IntPtr gitObjectPtr, [In] IntPtr repository, [In] string objectId, [In] git_otype type);

        [DllImport(Libgit2)]
        public static extern bool wrapped_git_odb_exists([In] IntPtr repository, [In] string objectId);

        [DllImport(Libgit2)]
        public static extern OperationResult wrapped_git_odb_read_header(out git_rawobj rawObject, [In] IntPtr repository, [In] string objectId);

        [DllImport(Libgit2)]
        public static extern OperationResult wrapped_git_odb_read(out git_rawobj rawObject, [In] IntPtr repository, [In] string objectId);

        [DllImport(Libgit2)]
        // TODO: Try to use a git_person struct
        public static extern OperationResult wrapped_git_apply_tag(out IntPtr tagPtr, [In] IntPtr repository, [In] string targetId, [In] string tagName, [In] string tagMessage, [In] string taggerName, [In] string taggerEmail, ulong taggerTime);
    }
}