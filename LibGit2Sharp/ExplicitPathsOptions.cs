using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Allows callers to specify how unmatched paths should be handled
    /// by operations such as Reset(), Compare(), Unstage(), ...
    /// <para>
    ///   By passing these options, the passed paths will be treated as
    ///   explicit paths, and NOT pathspecs containing globs.
    /// </para>
    /// </summary>
    public sealed class ExplicitPathsOptions
    {
        /// <summary>
        /// Associated paths will be treated as explicit paths.
        /// </summary>
        public ExplicitPathsOptions()
        {
            ShouldFailOnUnmatchedPath = true;
        }

        /// <summary>
        /// When set to true, the called operation will throw a <see cref="UnmatchedPathException"/> when an unmatched
        /// path is encountered.
        /// <para>
        ///   Set to true by default.
        /// </para>
        /// </summary>
        public bool ShouldFailOnUnmatchedPath { get; set; }

        /// <summary>
        /// Sets a callback that will be called once for each unmatched path.
        /// </summary>
        public UnmatchedPathHandler OnUnmatchedPath { get; set; }
    }
}
