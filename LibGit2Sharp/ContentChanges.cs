using System;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Holds the changes between two <see cref = "Blob" />s.
    /// </summary>
    public class ContentChanges : IContentChanges
    {
        private readonly StringBuilder patchBuilder = new StringBuilder();
        private static readonly Utf8Marshaler marshaler = (Utf8Marshaler)Utf8Marshaler.GetInstance(string.Empty);

        protected ContentChanges()
        {
        }

        internal ContentChanges(Repository repo, IBlob oldBlob, IBlob newBlob, GitDiffOptions options)
        {
            using (var osw1 = new ObjectSafeWrapper(oldBlob.Id, repo))
            using (var osw2 = new ObjectSafeWrapper(newBlob.Id, repo))
            {
                Ensure.Success(NativeMethods.git_diff_blobs(osw1.ObjectPtr, osw2.ObjectPtr, options, IntPtr.Zero, FileCallback, HunkCallback, LineCallback));
            }
        }

        private int FileCallback(IntPtr data, GitDiffDelta delta, float progress)
        {
            IsBinaryComparison = IsBinaryDelta(delta);

            if (!IsBinaryComparison)
            {
                return 0;
            }

            PatchBuilder.Append("Binary content differ\n");

            return 0;
        }

        internal static bool IsBinaryDelta(GitDiffDelta delta)
        {
            //TODO Fix the interop issue on amd64 and use GitDiffDelta.Binary
            return delta.OldFile.Flags.Has(GitDiffFileFlags.GIT_DIFF_FILE_BINARY) || delta.NewFile.Flags.Has(GitDiffFileFlags.GIT_DIFF_FILE_BINARY);
        }

        private int HunkCallback(IntPtr data, GitDiffDelta delta, GitDiffRange range, IntPtr header, uint headerlen)
        {
            string decodedContent = marshaler.NativeToString(header, headerlen);

            PatchBuilder.AppendFormat("{0}", decodedContent);
            return 0;
        }

        private int LineCallback(IntPtr data, GitDiffDelta delta, GitDiffRange range, GitDiffLineOrigin lineorigin, IntPtr content, uint contentlen)
        {
            string decodedContent = marshaler.NativeToString(content, contentlen);

            string prefix;

            switch (lineorigin)
            {
                case GitDiffLineOrigin.GIT_DIFF_LINE_ADDITION:
                    LinesAdded++;
                    prefix = Encoding.ASCII.GetString(new[] { (byte)lineorigin });
                    break;

                case GitDiffLineOrigin.GIT_DIFF_LINE_DELETION:
                    LinesDeleted++;
                    prefix = Encoding.ASCII.GetString(new[] { (byte)lineorigin });
                    break;

                case GitDiffLineOrigin.GIT_DIFF_LINE_CONTEXT:
                    prefix = Encoding.ASCII.GetString(new[] { (byte)lineorigin });
                    break;

                default:
                    prefix = string.Empty;
                    break;
            }

            PatchBuilder.AppendFormat("{0}{1}", prefix, decodedContent);
            return 0;
        }

        /// <summary>
        ///   Determines if at least one of the compared <see cref="Blob"/>s holds some binary content.
        /// </summary>
        public bool IsBinaryComparison { get; protected set; }

        /// <summary>
        ///   The number of lines added.
        /// </summary>
        public int LinesAdded { get; set; }

        /// <summary>
        ///   The number of lines deleted.
        /// </summary>
        public int LinesDeleted { get; set; }

        /// <summary>
        ///   The patch corresponding to these changes.
        /// </summary>
        public string Patch
        {
            get { return patchBuilder.ToString(); }
        }

        public StringBuilder PatchBuilder
        {
            get { return patchBuilder; }
        }
    }
}
