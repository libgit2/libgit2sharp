using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    internal class IndexCallbacks
    {
        public static NativeMethods.git_index_matched_path_cb ToCallback(IndexUpdateHandler handler)
        {
            NativeMethods.git_index_matched_path_cb cb = (path, pathSpec, intPtr) =>
            {
                if (handler != null)
                {
                    return handler(path, pathSpec);
                }
                return 0;
            };
            return cb;
        }
    }

    /// <summary>
    /// Options for updating foo bar baz
    /// </summary>
    public class IndexUpdateOptions
    {
        /// <summary>
        /// Callback for matching paths
        /// </summary>
        public IndexUpdateHandler IndexUpdateHandler { get; set; }
    }
}
