using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;

namespace LibGit2Sharp
{
    /// <summary>
    /// Base class for configuring rename and copy detection options in a diff.
    /// </summary>
    public abstract class Find
    {
        /// <summary>
        /// Enables rename detection.
        /// </summary>
        /// <param name="threshold">The minimum threshold to include the rename.</param>
        /// <returns>
        /// An object containing the configuration.
        /// </returns>
        public static FindRenames Renames(uint? threshold = null)
        {
            return new FindRenames(threshold);
        }

        /// <summary>
        /// Enables copy detection.
        /// </summary>
        /// <param name="threshold">The minimum threshold to include the copy.</param>
        /// <param name="checkUnmodifiedFiles">if set to <c>true</c> copies will be found in unmodified files.</param>
        /// <returns>
        /// An object containing the configuration.
        /// </returns>
        public static FindCopies Copies(uint? threshold = null, bool checkUnmodifiedFiles = false)
        {
            return new FindCopies(threshold, checkUnmodifiedFiles);
        }

        internal abstract GitDiffFindOptions Options { get; }

        internal bool RequiresUnmodifiedFiles
        {
            get { return Options.Flags.HasFlag(GitDiffFindOptionFlags.GIT_DIFF_FIND_COPIES_FROM_UNMODIFIED); }
        }

        /// <summary>
        /// Class for configuring copy detection in a diff.
        /// </summary>
        public class FindCopies : Find
        {
            private readonly GitDiffFindOptions options;

            internal override GitDiffFindOptions Options
            {
                get { return options; }
            }

            internal FindCopies(GitDiffFindOptions options, uint? threshold, bool checkUnmodifiedFiles)
            {
                if (threshold.HasValue)
                {
                    options.CopyThreshold = threshold.Value;
                }

                if (checkUnmodifiedFiles)
                {
                    options.Flags |= GitDiffFindOptionFlags.GIT_DIFF_FIND_COPIES_FROM_UNMODIFIED;
                }

                this.options = options;
            }

            internal FindCopies(uint? threshold, bool checkUnmodifiedFiles)
                : this(new GitDiffFindOptions { Flags = GitDiffFindOptionFlags.GIT_DIFF_FIND_COPIES }, threshold,
                       checkUnmodifiedFiles)
            { }

            /// <summary>
            /// Enables rename detection.
            /// </summary>
            /// <param name="threshold">The minimum threshold to include the rename.</param>
            /// <returns>
            /// An object containing the configuration.
            /// </returns>
            public Find AndRenames(uint? threshold = null)
            {
                options.Flags |= GitDiffFindOptionFlags.GIT_DIFF_FIND_RENAMES;
                return new FindRenames(options, threshold);
            }
        }

        /// <summary>
        /// Class for configuring rename detection in a diff.
        /// </summary>
        public class FindRenames : Find
        {
            private readonly GitDiffFindOptions options;

            internal override GitDiffFindOptions Options
            {
                get { return options; }
            }

            internal FindRenames(uint? threshold)
                : this(new GitDiffFindOptions { Flags = GitDiffFindOptionFlags.GIT_DIFF_FIND_RENAMES }, threshold)
            { }

            internal FindRenames(GitDiffFindOptions options, uint? threshold)
            {
                if (threshold.HasValue)
                {
                    options.RenameThreshold = threshold.Value;
                }

                this.options = options;
            }

            /// <summary>
            /// Enables copy detection.
            /// </summary>
            /// <param name="threshold">The minimum threshold to include the copy.</param>
            /// <param name="checkUnmodifiedFiles">if set to <c>true</c> copies will be found in unmodified files.</param>
            /// <returns>
            /// An object containing the configuration.
            /// </returns>
            public Find AndCopies(uint? threshold = null, bool checkUnmodifiedFiles = false)
            {
                options.Flags |= GitDiffFindOptionFlags.GIT_DIFF_FIND_COPIES;
                return new FindCopies(options, threshold, checkUnmodifiedFiles);
            }
        }
    }
}