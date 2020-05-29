using System;
using System.Collections.Generic;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    /// Represents a line with line number and content.
    /// </summary>
    public struct Line
    {
        /// <summary>
        /// The line number of the original line in the blob.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// The content of the line in the original blob.
        /// </summary>
        public string Content { get; }

        internal Line(int lineNumber, string content)
        {
            LineNumber = lineNumber;
            Content = content;
        }
    }
}
