using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A collection of commits in a <see cref = "Repository" />
    /// </summary>
    public class CommitCollection : IQueryableCommitCollection
    {
        private readonly Repository repo;
        private object includedIdentifier = "HEAD";
        private object excludedIdentifier;
        private readonly GitSortOptions sortOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref = "CommitCollection"/> class.
        /// The commits will be enumerated according in reverse chronological order.
        /// </summary>
        /// <param name = "repo">The repository.</param>
        internal CommitCollection(Repository repo)
            : this(repo, GitSortOptions.Time)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "CommitCollection" /> class.
        /// </summary>
        /// <param name = "repo">The repository.</param>
        /// <param name = "sortingStrategy">The sorting strategy which should be applied when enumerating the commits.</param>
        internal CommitCollection(Repository repo, GitSortOptions sortingStrategy)
        {
            this.repo = repo;
            sortOptions = sortingStrategy;
        }

        /// <summary>
        /// Gets the current sorting strategy applied when enumerating the collection
        /// </summary>
        public GitSortOptions SortedBy
        {
            get { return sortOptions; }
        }

        #region IEnumerable<Commit> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public IEnumerator<Commit> GetEnumerator()
        {
            if ((repo.Info.IsEmpty) && PointsAtTheHead(includedIdentifier.ToString()))
            {
                return Enumerable.Empty<Commit>().GetEnumerator();
            }

            return new CommitEnumerator(repo, includedIdentifier, excludedIdentifier, sortOptions);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        ///  Returns the list of commits of the repository matching the specified <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter">The options used to control which commits will be returned.</param>
        /// <returns>A collection of commits, ready to be enumerated.</returns>
        public ICommitCollection QueryBy(Filter filter)
        {
            Ensure.ArgumentNotNull(filter, "filter");
            Ensure.ArgumentNotNull(filter.Since, "filter.Since");
            Ensure.ArgumentNotNullOrEmptyString(filter.Since.ToString(), "filter.Since");

            return new CommitCollection(repo, filter.SortBy)
                       {
                           includedIdentifier = filter.Since, 
                           excludedIdentifier = filter.Until
                       };
        }

        private static bool PointsAtTheHead(string shaOrRefName)
        {
            return ("HEAD".Equals(shaOrRefName, StringComparison.Ordinal) || "refs/heads/master".Equals(shaOrRefName, StringComparison.Ordinal));
        }

        /// <summary>
        ///  Stores the content of the <see cref="Repository.Index"/> as a new <see cref="Commit"/> into the repository.
        /// </summary>
        /// <param name="author">The <see cref="Signature"/> of who made the change.</param>
        /// <param name="committer">The <see cref="Signature"/> of who added the change to the repository.</param>
        /// <param name="message">The description of why a change was made to the repository.</param>
        /// <returns>The generated <see cref="Commit"/>.</returns>
        public Commit Create(Signature author, Signature committer, string message)
        {
            GitOid treeOid;
            int res = NativeMethods.git_tree_create_fromindex(out treeOid, repo.Index.Handle);
            Ensure.Success(res);

            Reference head = repo.Head;
            GitOid[] gitOids = RetrieveCommitParent(head);

            GitOid commitOid;
            res = NativeMethods.git_commit_create(out commitOid, repo.Handle, head.CanonicalName, author.Handle,
                                                  committer.Handle, message, ref treeOid, gitOids.Count(), ref gitOids);
            Ensure.Success(res);

            return repo.Lookup<Commit>(new ObjectId(commitOid));
        }

        private static GitOid[] RetrieveCommitParent(Reference head)
        {
            DirectReference oidRef = head.ResolveToDirectReference();
            if (oidRef == null)
            {
                return new GitOid[] { };
            }

            var headCommitId = new ObjectId(oidRef.TargetIdentifier);
            return new[] { headCommitId.Oid };
        }

        private class CommitEnumerator : IEnumerator<Commit>
        {
            private readonly Repository repo;
            private readonly RevWalkerSafeHandle handle;
            private ObjectId currentOid;

            public CommitEnumerator(Repository repo, object includedIdentifier, object excludedIdentifier, GitSortOptions sortingStrategy)
            {
                this.repo = repo;
                int res = NativeMethods.git_revwalk_new(out handle, repo.Handle);
                Ensure.Success(res);

                Sort(sortingStrategy);
                Push(includedIdentifier);
                Hide(excludedIdentifier);
            }

            #region IEnumerator<Commit> Members

            public Commit Current
            {
                get
                {
                    if (currentOid == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return repo.Lookup<Commit>(currentOid);
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                GitOid oid;
                var res = NativeMethods.git_revwalk_next(out oid, handle);

                if (res == (int)GitErrorCode.GIT_EREVWALKOVER)
                {
                    return false;
                }

                Ensure.Success(res);

                currentOid = new ObjectId(oid);

                return true;
            }

            public void Reset()
            {
                NativeMethods.git_revwalk_reset(handle);
            }

            #endregion

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (handle == null || handle.IsInvalid)
                {
                    return;
                }

                handle.Dispose();
            }

            private void Push(object identifier)
            {
                var oid = RetrieveCommitId(identifier).Oid;
                int res = NativeMethods.git_revwalk_push(handle, ref oid);
                Ensure.Success(res);
            }

            private void Hide(object identifier)
            {
                if (identifier == null)
                {
                    return;
                }

                var oid = RetrieveCommitId(identifier).Oid;
                int res = NativeMethods.git_revwalk_hide(handle, ref oid);
                Ensure.Success(res);
            }

            private void Sort(GitSortOptions options)
            {
                NativeMethods.git_revwalk_sorting(handle, options);
            }

            private ObjectId RetrieveCommitId(object identifier)
            {
                string shaOrReferenceName = RetrieveShaOrReferenceName(identifier);

                GitObject gitObj = repo.Lookup(shaOrReferenceName);

                // TODO: Should we check the type? Git-log allows TagAnnotation oid as parameter. But what about Blobs and Trees?
                if (gitObj == null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "No valid git object pointed at by '{0}' exists in the repository.", identifier));
                }

                return gitObj.Id;
            }

            private static string RetrieveShaOrReferenceName(object identifier)
            {
                if (identifier is string)
                {
                    return identifier as string;
                }

                if (identifier is ObjectId || identifier is Reference || identifier is Branch)
                    return identifier.ToString();

                if (identifier is Commit)
                    return ((Commit)identifier).Id.Sha;

                throw new InvalidOperationException(string.Format("Unexpected kind of identifier '{0}'.", identifier));
            }
        
        }
    }
}