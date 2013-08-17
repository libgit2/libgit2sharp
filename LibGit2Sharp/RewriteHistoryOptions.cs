using System;
using System.Collections.Generic;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options for a RewriteHistory operation.
    /// </summary>
    public sealed class RewriteHistoryOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RewriteHistoryOptions"/> class.
        /// </summary>
        public RewriteHistoryOptions()
        {
            BackupRefsNamespace = "refs/original/";
        }

        /// <summary>
        /// Namespace where rewritten references should be stored.
        /// (required; default: "refs/original/")
        /// </summary>
        public string BackupRefsNamespace { get; set; }

        /// <summary>
        /// Rewriter for commit metadata.
        /// </summary>
        public Func<Commit, CommitRewriteInfo> CommitHeaderRewriter { get; set; }

        /// <summary>
        /// Rewriter for mangling parent links.
        /// </summary>
        public Func<Commit, IEnumerable<Commit>> CommitParentsRewriter { get; set; }

        /// <summary>
        /// Rewriter for commit trees.
        /// </summary>
        public Func<Commit, TreeDefinition> CommitTreeRewriter { get; set; }

        /// <summary>
        /// Rewriter for tag names. This is called with (OldTag.Name, OldTag.IsAnnotated, OldTarget).
        /// </summary>
        public Func<String, bool, GitObject, string> TagNameRewriter { get; set; }
    }
}
