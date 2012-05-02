using System.Text;

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
