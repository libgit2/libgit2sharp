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
        /// Rewriter for tag names. This is called with
        /// (OldTag.Name, OldTag.IsAnnotated, OldTarget.Identifier).
        /// OldTarget.Identifier is either the SHA of a direct reference,
        /// or the canonical name of a symbolic reference.
        /// </summary>
        public Func<string, bool, string, string> TagNameRewriter { get; set; }

        /// <summary>
        /// Empty commits should be removed while rewriting.
        /// </summary>
        public bool PruneEmptyCommits { get; set; }
    }
}
