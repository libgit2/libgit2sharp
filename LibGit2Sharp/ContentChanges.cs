using System;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Holds the changes between two <see cref = "Blob" />s.
    /// </summary>
    public class ContentChanges
    {
        private readonly StringBuilder patchBuilder = new StringBuilder();

        protected ContentChanges()
        {
        }

        internal ContentChanges(Repository repo, Blob oldBlob, Blob newBlob, GitDiffOptions options)
        {
            using (var osw1 = new ObjectSafeWrapper(oldBlob.Id, repo))
            using (var osw2 = new ObjectSafeWrapper(newBlob.Id, repo))
            {
                Ensure.Success(NativeMethods.git_diff_blobs(repo.Handle, osw1.ObjectPtr, osw2.ObjectPtr, options, IntPtr.Zero, HunkCallback, LineCallback));
            }
        }

        private static string NativeToString(IntPtr content, IntPtr contentlen)
        {
            return ((Utf8Marshaler)(Utf8Marshaler.GetInstance(string.Empty))).NativeToString(content, contentlen.ToInt32());
        }

        private int HunkCallback(IntPtr data, GitDiffDelta delta, GitDiffRange range, IntPtr header, IntPtr headerlen)
        {
            string decodedContent = NativeToString(header, headerlen);

            PatchBuilder.AppendFormat("{0}", decodedContent);
            return 0;
        }

        private int LineCallback(IntPtr data, GitDiffDelta delta, GitDiffLineOrigin lineorigin, IntPtr content, IntPtr contentlen)
        {
            string decodedContent = NativeToString(content, contentlen);

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
        ///   The number of lines added.
        /// </summary>
        public int LinesAdded { get; internal set; }

        /// <summary>
        ///   The number of lines deleted.
        /// </summary>
        public int LinesDeleted { get; internal set; }

        /// <summary>
        ///   The patch corresponding to these changes.
        /// </summary>
        public string Patch
        {
            get { return patchBuilder.ToString(); }
        }

        internal StringBuilder PatchBuilder
        {
            get { return patchBuilder; }
        }
    }
}
