using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A commit with information about its source. Input for merge and rebase functions
    /// </summary>
    public class AnnotatedCommit : IDisposable, IAnnotatedCommit
    {
        internal readonly Repository repository;
        internal AnnotatedCommitHandle Handle { get; private set; }

        /// <summary>
        /// Initialize a <see cref="LibGit2Sharp.AnnotatedCommit"/> from extended SHA-1 syntax
        /// </summary>
        /// <param name="repository">The repository in which the commit lives</param>
        /// <param name="revspec">A string in extended SHA-1 syntax to look up the object</param>
        public AnnotatedCommit(Repository repository, string revspec)
        {
            this.repository = repository;
            this.Handle = Proxy.git_annotated_commit_from_revspec(repository.Handle, revspec);
        }

        /// <summary>
        /// Initialize a <see cref="LibGit2Sharp.AnnotatedCommit"/> from extended SHA-1 syntax
        /// </summary>
        /// <param name="repository">The repository in which the commit lives</param>
        /// <param name="reference">A reference pointing to the commit</param>
        public AnnotatedCommit(Repository repository, Reference reference)
        {
            this.repository = repository;
            using (var refHandle = Proxy.git_reference_lookup(repository.Handle, reference.CanonicalName, true))
            {
                this.Handle = Proxy.git_annotated_commit_from_ref(repository.Handle, refHandle);
            }
        }

        /// <summary>
        /// Initialize a <see cref="LibGit2Sharp.AnnotatedCommit"/> from extended SHA-1 syntax
        /// </summary>
        /// <param name="repository">The repository in which the commit lives</param>
        /// <param name="commit">A commit</param>
        public AnnotatedCommit(Repository repository, Commit commit)
        {
            this.repository = repository;
            this.Handle = Proxy.git_annotated_commit_lookup(repository.Handle, commit.Id.Oid);
        }

        /// <summary>
        /// Releases all resource used by the <see cref="LibGit2Sharp.AnnotatedCommit"/> object.
        /// </summary>
        public void Dispose()
        {
            Handle.Dispose();
        }

        AnnotatedCommit IAnnotatedCommit.GetAnnotatedCommit()
        {
            return this;
        }
    }
}

