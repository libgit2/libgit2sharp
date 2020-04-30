using System;
using System.Collections.Generic;
using System.Text;

namespace LibGit2Sharp.Core
{
    public struct Line
    {
        /// <summary>
        /// Points to the number of the original line in the blob
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// This content of the line in the original blob
        /// </summary>
        public String Content { get; }

        internal Line(int lineNumber, string content)
        {
            LineNumber = lineNumber;
            Content = content;
        }
    }
}
