﻿using System;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Holds the changes between two <see cref = "Blob" />s.
    /// </summary>
    public class ContentChanges : Changes, IContentChanges
    {
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
            IsBinaryComparison = delta.IsBinary();

            if (!IsBinaryComparison)
            {
                return 0;
            }

            AppendToPatch("Binary content differ\n");

            return 0;
        }

        private int HunkCallback(IntPtr data, GitDiffDelta delta, GitDiffRange range, IntPtr header, uint headerlen)
        {
            string decodedContent = Utf8Marshaler.FromNative(header, headerlen);

            AppendToPatch(decodedContent);
            return 0;
        }

        private int LineCallback(IntPtr data, GitDiffDelta delta, GitDiffRange range, GitDiffLineOrigin lineorigin, IntPtr content, uint contentlen)
        {
            string decodedContent = Utf8Marshaler.FromNative(content, contentlen);

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
