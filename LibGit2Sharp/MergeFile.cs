using LibGit2Sharp.Core;
using System.IO;
using System.Runtime.InteropServices;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options for merging a file.
    /// </summary>
    public sealed class MergeFileOptions
    {
        /// <summary>
        /// Label for the ancestor file side of the conflict which will be
        /// prepended to labels in diff3-format merge files.
        /// </summary>
        public string AncestorLabel { get; set; }

        /// <summary>
        /// Label for our file side of the conflict which will be prepended to labels in merge files.
        /// </summary>
        public string OurLabel { get; set; }

        /// <summary>
        /// Label for their file side of the conflict which will be prepended to labels in merge files.
        /// </summary>
        public string TheirLabel { get; set; }

        /// <summary>
        /// Flags for automerging content.
        /// </summary>
        public MergeFileFavor Favor { get; set; }

        /// <summary>
        /// The size of conflict markers (eg, " &lt; &lt; &lt; &lt; &lt; &lt; &lt; ").
        /// Default is 7.
        /// </summary>
        public short MarkerSize { get; set; }

        /// <summary>
        /// File merging flags.
        /// </summary>
        public MergeFileFlag Flags { get; set; }
    }

    /// <summary>
    /// Internal class to read, or specify directly, the bytes
    /// to use for merging files.
    /// </summary>
    internal class MergeFileInput
    {
        private git_merge_file_input input;
        private byte[] bytes;

        /// <summary>
        /// The bytes to use for merging files.  Marshals the bytes
        /// to the native pointer.
        /// </summary>
        public byte[] Bytes
        {
            get
            {
                return bytes;
            }
            set
            {
                bytes = value;
                input.size = bytes.Length;
                input.ptr = Marshal.AllocHGlobal(input.size);
                if (input.size > 0) { Marshal.Copy(bytes, 0, input.ptr, input.size); }
            }
        }

        /// <summary>
        /// Initialize the native merge file input object from managed
        /// sources.
        /// </summary>
        public MergeFileInput()
        {
            input = Proxy.git_merge_file_input_init();
        }

        /// <summary>
        /// Read the bytes from a file from disk and marshal to the
        /// native pointer.  
        /// </summary>
        /// <param name="path">The path to the input file.</param>
        /// <returns>The current object for chaining.</returns>
        public MergeFileInput Read(FilePath path)
        {
            string fullNativePath = Path.GetFullPath(path.Native);
            try
            {
                if (!File.Exists(fullNativePath)) { throw new FileNotFoundException("Cannot find input file", fullNativePath); }
                Bytes = File.ReadAllBytes(fullNativePath);
                return this;
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (IOException ex)
            {
                throw new LibGit2SharpException($"Unable to read input file '{fullNativePath}'", ex);
            }
        }

        /// <summary>
        /// Convert to a wrapper for the native structure.
        /// </summary>
        /// <param name="mergeFileInput">The internal class.</param>
        public static implicit operator GitMergeFileInputWrapper(MergeFileInput mergeFileInput)
        {
            return new GitMergeFileInputWrapper(mergeFileInput.input);
        }
    }

    /// <summary>
    /// Give managed access to a native merge file results object.
    /// </summary>
    internal class MergeFileResult
    {
        private git_merge_file_result result;

        /// <summary>
        /// Whether the file merge was successful without conflicts.
        /// </summary>
        public readonly bool Automergeable;

        /// <summary>
        /// The bytes of the merged file.
        /// </summary>
        public readonly byte[] Bytes;

        /// <summary>
        /// Create the object from the native wrapper.
        /// </summary>
        /// <param name="wrapper">The merge file result wrapper.</param>
        public MergeFileResult(GitMergeFileResultWrapper wrapper)
        {
            result = wrapper;

            Bytes = new byte[result.len];
            if (result.len > 0) { Marshal.Copy(result.ptr, Bytes, 0, result.len); }

            Automergeable = result.automergeable == 1;
        }

        /// <summary>
        /// Write the bytes of the merged file to disk.  Create the file and/or directory
        /// if it doesn't exist.
        /// </summary>
        /// <param name="path">The output file path.</param>
        /// <returns>The current object for chaining.</returns>
        public MergeFileResult Write(FilePath path)
        {
            string fullNativePath = Path.GetFullPath(path.Native);
            try
            {
                string resultDir = Path.GetDirectoryName(fullNativePath);
                if (!Directory.Exists(resultDir)) { Directory.CreateDirectory(resultDir); }
                if (File.Exists(fullNativePath)) { File.Delete(fullNativePath); }
                if (Bytes.Length > 0) { File.WriteAllBytes(fullNativePath, Bytes); }
            }
            catch (IOException ex)
            {
                throw new LibGit2SharpException($"Unable to write result file '{fullNativePath}'", ex);
            }
            return this;
        }
    }

    /// <summary>
    /// Internal extension methods to add to the public MergeFileOptions class.
    /// </summary>
    internal static class MergeFileOptionsExtensions
    {
        /// <summary>
        /// Create a native merge file options structure from the public merge file options.
        /// </summary>
        /// <param name="mergeFileOptions">The public merge file options class.</param>
        /// <returns>The native merge file options structure.</returns>
        public static unsafe git_merge_file_options ToNative(this MergeFileOptions mergeFileOptions)
        {
            git_merge_file_options git_merge_file_options = Proxy.git_merge_file_options_init();
            if (mergeFileOptions == null) { return git_merge_file_options; }

            if (!string.IsNullOrEmpty(mergeFileOptions.AncestorLabel))
            {
                git_merge_file_options.ancestor_label = (char*)StrictUtf8Marshaler.FromManaged(mergeFileOptions.AncestorLabel);
            }
            if (!string.IsNullOrEmpty(mergeFileOptions.OurLabel))
            {
                git_merge_file_options.our_label = (char*)StrictUtf8Marshaler.FromManaged(mergeFileOptions.OurLabel);
            }
            if (!string.IsNullOrEmpty(mergeFileOptions.TheirLabel))
            {
                git_merge_file_options.their_label = (char*)StrictUtf8Marshaler.FromManaged(mergeFileOptions.TheirLabel);
            }
            git_merge_file_options.favor = mergeFileOptions.Favor;
            git_merge_file_options.flags = mergeFileOptions.Flags;
            if (mergeFileOptions.MarkerSize > 0)
            {
                git_merge_file_options.marker_size = (ushort)mergeFileOptions.MarkerSize;
            }
            return git_merge_file_options;
        }
    }
}
