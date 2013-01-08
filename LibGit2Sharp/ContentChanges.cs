using System;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Holds the changes between two <see cref = "Blob" />s.
    /// </summary>
    public class ContentChanges : Changes
    {
        /// <summary>
        ///   Needed for mocking purposes.
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
    }
}
