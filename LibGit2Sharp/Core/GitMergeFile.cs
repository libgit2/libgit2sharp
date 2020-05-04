using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// The file inputs to git_merge_file. Callers should populate the git_merge_file_input structure
    /// with descriptions of the files in each side of the conflict for use in producing the merge file.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct git_merge_file_input
    {
        public uint version;

        /// <summary>
        /// Pointer to the contents of the file.
        /// </summary>
        public IntPtr ptr;

        /// <summary>
        /// Size of the contents pointed to in ptr.
        /// </summary>
        public int size;

        /// <summary>
        /// File name of the conflicted file, or null to not merge the path.
        /// </summary>
        public char* path;

        /// <summary>
        /// File mode of the conflicted file, or 0 to not merge the mode.
        /// </summary>
        public uint mode;
    }

    /// <summary>
    /// Information about file-level merging
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct git_merge_file_result
    {
        /// <summary>
        /// 1 if the output was automerged, 0 if the output contains conflict markers.
        /// </summary>
        public int automergeable;

        /// <summary>
        /// The path that the resultant merge file should use, or null if a filename conflict would occur.
        /// </summary>
        public char* path;

        /// <summary>
        /// The mode that the resultant merge file should use.
        /// </summary>
        public uint mode;

        /// <summary>
        /// The contents of the merge.
        /// </summary>
        public IntPtr ptr;

        /// <summary>
        /// The length of the merge contents.
        /// </summary>
        public int len;
    }

    /// <summary>
    /// Options for merging a file
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct git_merge_file_options
    {
        public uint version;

        /// <summary>
        /// Label for the ancestor file side of the conflict which will be
        /// prepended to labels in diff3-format merge files.
        /// </summary>
        public char* ancestor_label;

        /// <summary>
        /// Label for our file side of the conflict which will be prepended to labels in merge files.
        /// </summary>
        public char* our_label;

        /// <summary>
        /// Label for their file side of the conflict which will be prepended to labels in merge files.
        /// </summary>
        public char* their_label;

        /// <summary>
        /// Flags for automerging content.
        /// </summary>
        public MergeFileFavor favor;

        /// <summary>
        /// File merging flags.
        /// </summary>
        public MergeFileFlag flags;

        /// <summary>
        /// The size of conflict markers (eg, " &lt; &lt; &lt; &lt; &lt; &lt; &lt; ").
        /// Default is 7.
        /// </summary>
        public ushort marker_size;
    }

    /// <summary>
    /// Wrapper for native merge file input object to handle initialization
    /// and freeing.
    /// </summary>
    internal class GitMergeFileInputWrapper : IDisposable
    {
        private IntPtr ptr;

        /// <summary>
        /// Initialize the wrapper with the native object and marshal it
        /// to a pointer.
        /// </summary>
        /// <param name="git_merge_file_input">The native merge file input object.</param>
        public GitMergeFileInputWrapper(git_merge_file_input git_merge_file_input)
        {
            ptr = Marshal.AllocHGlobal(Marshal.SizeOf(git_merge_file_input));
            Marshal.StructureToPtr(git_merge_file_input, ptr, false);
        }

        /// <summary>
        /// Convert the wrapper to a pointer to the native structure.
        /// </summary>
        /// <param name="gitMergeFileInputWrapper">The wrapper.</param>
        public unsafe static implicit operator git_merge_file_input* (GitMergeFileInputWrapper gitMergeFileInputWrapper)
        {
            return (git_merge_file_input*)gitMergeFileInputWrapper.ptr;
        }

        /// <summary>
        /// Convert the native strucure to a wrapper.
        /// </summary>
        /// <param name="git_merge_file_input">The native structure.</param>
        public static implicit operator GitMergeFileInputWrapper(git_merge_file_input git_merge_file_input)
        {
            return new GitMergeFileInputWrapper(git_merge_file_input);
        }

        /// <summary>
        /// Free the native structure and the associated pointer.
        /// </summary>
        public unsafe void Dispose()
        {
            if (ptr == IntPtr.Zero) { return; }

            var git_merge_file_input = Marshal.PtrToStructure<git_merge_file_input>(ptr);

            EncodingMarshaler.Cleanup(new IntPtr(git_merge_file_input.path));
            git_merge_file_input.path = (char*)IntPtr.Zero;

            Marshal.FreeHGlobal(git_merge_file_input.ptr);
            git_merge_file_input.ptr = IntPtr.Zero;

            Marshal.FreeHGlobal(ptr);
            ptr = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Wrapper for native merge file result object to handle initialization
    /// and freeing.
    /// </summary>
    internal class GitMergeFileResultWrapper : IDisposable
    {
        private IntPtr ptr;

        /// <summary>
        /// Initialize the wrapper with the native object and marshal it
        /// to a pointer.
        /// </summary>
        /// <param name="git_merge_file_result">The native merge file result object.</param>
        public GitMergeFileResultWrapper(git_merge_file_result git_merge_file_result)
        {
            ptr = Marshal.AllocHGlobal(Marshal.SizeOf(git_merge_file_result));
            Marshal.StructureToPtr(git_merge_file_result, ptr, false);
        }

        /// <summary>
        /// Convert the wrapper to a pointer to the native structure.
        /// </summary>
        /// <param name="gitMergeFileResultWrapper">The wrapper.</param>
        public static implicit operator git_merge_file_result(GitMergeFileResultWrapper gitMergeFileResultWrapper)
        {
            return Marshal.PtrToStructure<git_merge_file_result>(gitMergeFileResultWrapper.ptr);
        }

        /// <summary>
        /// Convert the native strucure to a wrapper.
        /// </summary>
        /// <param name="git_merge_file_result">The native structure.</param>
        public static implicit operator GitMergeFileResultWrapper(git_merge_file_result git_merge_file_result)
        {
            return new GitMergeFileResultWrapper(git_merge_file_result);
        }

        /// <summary>
        /// Free the native structure and the associated pointer.
        /// </summary>
        public unsafe void Dispose()
        {
            if (ptr == IntPtr.Zero) { return; }

            Proxy.git_merge_file_result_free((git_merge_file_result*)ptr);
            ptr = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Wrapper for native merge file options object to handle initialization
    /// and freeing.
    /// </summary>
    internal class GitMergeFileOptionsWrapper : IDisposable
    {
        private IntPtr ptr;

        /// <summary>
        /// Initialize the wrapper with the native object and marshal it
        /// to a pointer.
        /// </summary>
        /// <param name="git_merge_file_options">The native merge file options object.</param>
        public GitMergeFileOptionsWrapper(git_merge_file_options git_merge_file_options)
        {
            ptr = Marshal.AllocHGlobal(Marshal.SizeOf(git_merge_file_options));
            Marshal.StructureToPtr(git_merge_file_options, ptr, false);
        }

        /// <summary>
        /// Convert the wrapper to a pointer to the native structure.
        /// </summary>
        /// <param name="gitMergeFileOptionsWrapper">The wrapper.</param>
        public unsafe static implicit operator git_merge_file_options* (GitMergeFileOptionsWrapper gitMergeFileOptionsWrapper)
        {
            return (git_merge_file_options*)gitMergeFileOptionsWrapper.ptr;
        }

        /// <summary>
        /// Convert the native strucure to a wrapper.
        /// </summary>
        /// <param name="git_merge_file_options">The native structure.</param>
        public static implicit operator GitMergeFileOptionsWrapper(git_merge_file_options git_merge_file_options)
        {
            return new GitMergeFileOptionsWrapper(git_merge_file_options);
        }

        /// <summary>
        /// Free the native structure and the associated pointer.
        /// </summary>
        public unsafe void Dispose()
        {
            if (ptr == IntPtr.Zero) { return; }

            var git_merge_file_options = Marshal.PtrToStructure<git_merge_file_options>(ptr);

            EncodingMarshaler.Cleanup(new IntPtr(git_merge_file_options.ancestor_label));
            git_merge_file_options.ancestor_label = (char*)IntPtr.Zero;

            EncodingMarshaler.Cleanup(new IntPtr(git_merge_file_options.our_label));
            git_merge_file_options.our_label = (char*)IntPtr.Zero;

            EncodingMarshaler.Cleanup(new IntPtr(git_merge_file_options.their_label));
            git_merge_file_options.their_label = (char*)IntPtr.Zero;

            Marshal.FreeHGlobal(ptr);
            ptr = IntPtr.Zero;
        }
    }
}
