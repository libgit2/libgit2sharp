using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Allows callers to specify how blob content filters will be applied.
    /// </summary>
    public sealed class FilteringOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilteringOptions"/> class.
        /// </summary>
        /// <param name="hintPath">The path that a file would be checked out as</param>
        public FilteringOptions(string hintPath)
        {
            Ensure.ArgumentNotNull(hintPath, "hintPath");

            this.HintPath = hintPath;
        }

        /// <summary>
        /// The path to "hint" to the filters will be used to apply
        /// attributes.
        /// </summary>
        public string HintPath { get; private set; }
    }
}
