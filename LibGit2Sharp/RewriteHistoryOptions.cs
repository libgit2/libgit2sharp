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

        /// <summary>
        /// Action to exectute after rewrite succeeds,
        /// but before it is finalized.
        /// <para>
        /// An exception thrown here will rollback the operation.
        /// This is useful to inspect the new state of the repository
        /// and throw if you need to adjust and try again.
        /// </para>
        /// </summary>
        public Action OnSucceeding { get; set; }

        /// <summary>
        /// Action to execute if an error occurred during rewrite,
        /// before rollback of rewrite progress.
        /// Does not fire for exceptions thrown in <see cref="OnSucceeding" />.
        /// <para>
        /// This is useful to inspect the state of the repository
        /// at the time of the exception for troubleshooting.
        /// It is not meant to be used for general error handling;
        /// for that use <code>try</code>/<code>catch</code>.
        /// </para>
        /// <para>
        /// An exception thrown here will replace the original exception.
        /// You may want to pass the callback exception as an <code>innerException</code>.
        /// </para>
        /// </summary>
        public Action<Exception> OnError { get; set; }
    }
}
