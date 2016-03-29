using System;
using System.Collections;
using System.Collections.Generic;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    internal class MatchedPathsAggregator : IEnumerable<FilePath>
    {
        private readonly List<FilePath> matchedPaths = new List<FilePath>();

        /// <summary>
        /// The delegate with a signature that matches the native diff git_diff_notify_cb function's signature.
        /// </summary>
        /// <param name="diffListSoFar">The diff list so far, before the delta is inserted.</param>
        /// <param name="deltaToAdd">The delta that is being diffed</param>
        /// <param name="matchedPathspec">The pathsec that matched the path of the diffed files.</param>
        /// <param name="payload">Payload object.</param>
        internal int OnGitDiffNotify(IntPtr diffListSoFar, IntPtr deltaToAdd, IntPtr matchedPathspec, IntPtr payload)
        {
            // Convert null strings into empty strings.
            var path = LaxFilePathMarshaler.FromNative(matchedPathspec) ?? FilePath.Empty;

            if (matchedPaths.Contains(path))
            {
                return 0;
            }

            matchedPaths.Add(path);
            return 0;
        }

        public IEnumerator<FilePath> GetEnumerator()
        {
            return matchedPaths.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
