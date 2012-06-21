using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Base class for changes.
    /// </summary>
    public abstract class Changes
    {
        private readonly StringBuilder patchBuilder = new StringBuilder();

        internal void AppendToPatch(string patch)
        {
            patchBuilder.Append(patch);
        }

        /// <summary>
        ///   The number of lines added.
        /// </summary>
        public virtual int LinesAdded { get; internal set; }

        /// <summary>
        ///   The number of lines deleted.
        /// </summary>
        public virtual int LinesDeleted { get; internal set; }

        /// <summary>
        ///   The patch corresponding to these changes.
        /// </summary>
        public virtual string Patch
        {
            get { return patchBuilder.ToString(); }
        }

        /// <summary>
        ///   Determines if at least one side of the comparison holds binary content.
        /// </summary>
        public virtual bool IsBinaryComparison { get; protected set; }
    }
}
