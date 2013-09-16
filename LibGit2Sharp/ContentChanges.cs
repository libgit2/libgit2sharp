using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Holds the changes between two <see cref="Blob"/>s.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ContentChanges
    {
        private readonly StringBuilder patchBuilder = new StringBuilder();
        private bool isBinaryComparison;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected ContentChanges()
        { }

        internal ContentChanges(Repository repo, Blob oldBlob, Blob newBlob, GitDiffOptions options)
        {
            Proxy.git_diff_blobs(repo.Handle,
                                 oldBlob != null ? oldBlob.Id : null,
                                 newBlob != null ? newBlob.Id : null,
                                 options, FileCallback, HunkCallback, LineCallback);
        }

        internal ContentChanges(bool isBinaryComparison)
        {
            this.isBinaryComparison = isBinaryComparison;
        }

        internal void AppendToPatch(string patch)
        {
            patchBuilder.Append(patch);
        }

        /// <summary>
        /// The number of lines added.
        /// </summary>
        public virtual int LinesAdded { get; internal set; }

        /// <summary>
        /// The number of lines deleted.
        /// </summary>
        public virtual int LinesDeleted { get; internal set; }

        /// <summary>
        /// The patch corresponding to these changes.
        /// </summary>
        public virtual string Patch
        {
            get { return patchBuilder.ToString(); }
        }

        /// <summary>
        /// Determines if at least one side of the comparison holds binary content.
        /// </summary>
        public virtual bool IsBinaryComparison
        {
            get { return isBinaryComparison; }
            private set { isBinaryComparison = value; }
        }

        private int FileCallback(GitDiffDelta delta, float progress, IntPtr payload)
        {
            IsBinaryComparison = delta.IsBinary();

            if (!IsBinaryComparison)
            {
                return 0;
            }

            AppendToPatch("Binary content differ\n");

            return 0;
        }

        private int HunkCallback(GitDiffDelta delta, GitDiffRange range, IntPtr header, UIntPtr headerlen, IntPtr payload)
        {
            string decodedContent = Utf8Marshaler.FromNative(header, (int)headerlen);

            AppendToPatch(decodedContent);
            return 0;
        }

        private int LineCallback(GitDiffDelta delta, GitDiffRange range, GitDiffLineOrigin lineorigin, IntPtr content, UIntPtr contentlen, IntPtr payload)
        {
            string decodedContent = Utf8Marshaler.FromNative(content, (int)contentlen);

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

            AppendToPatch(prefix);
            AppendToPatch(decodedContent);
            return 0;
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    @"{{+{0}, -{1}}}", LinesAdded, LinesDeleted);
            }
        }
    }
}
