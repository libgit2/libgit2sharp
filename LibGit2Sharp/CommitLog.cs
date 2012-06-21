using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A log of commits in a <see cref = "Repository" />
    /// </summary>
    public class CommitLog : IQueryableCommitLog
    {
        private readonly Repository repo;
        readonly Filter queryFilter;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected CommitLog()
        { }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "CommitLog" /> class.
        ///   The commits will be enumerated according in reverse chronological order.
        /// </summary>
        /// <param name = "repo">The repository.</param>
        internal CommitLog(Repository repo)
            : this(repo, new Filter())
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "CommitLog" /> class.
        /// </summary>
        /// <param name = "repo">The repository.</param>
        /// <param name="queryFilter">The filter to use in querying commits</param>
        internal CommitLog(Repository repo, Filter queryFilter)
        {
            this.repo = repo;
            this.queryFilter = queryFilter;
        }

        /// <summary>
        ///   Gets the current sorting strategy applied when enumerating the log
        /// </summary>
        public virtual GitSortOptions SortedBy
        {
            get { return queryFilter.SortBy; }
        }

        #region IEnumerable<Commit> Members

        /// <summary>
        ///   Returns an enumerator that iterates through the log.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the log.</returns>
        public virtual IEnumerator<Commit> GetEnumerator()
        {
            if ((repo.Info.IsEmpty) && queryFilter.SinceList.Any(o => PointsAtTheHead(o.ToString()))) // TODO: ToString() == fragile
            {
                return Enumerable.Empty<Commit>().GetEnumerator();
            }

            return new CommitEnumerator(repo, queryFilter);
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the log.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator" /> object that can be used to iterate through the log.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        ///   Returns the list of commits of the repository matching the specified <paramref name = "filter" />.
        /// </summary>
        /// <param name = "filter">The options used to control which commits will be returned.</param>
        /// <returns>A list of commits, ready to be enumerated.</returns>
        public virtual ICommitLog QueryBy(Filter filter)
        {
            Ensure.ArgumentNotNull(filter, "filter");

            return new CommitLog(repo, filter);
        }

        private static bool PointsAtTheHead(string shaOrRefName)
        {
            return ("HEAD".Equals(shaOrRefName, StringComparison.Ordinal) || "refs/heads/master".Equals(shaOrRefName, StringComparison.Ordinal));
        }

        /// <summary>
        ///   Find the best possible common ancestor given two <see cref = "Commit"/>s.
        /// </summary>
        /// <param name = "first">The first <see cref = "Commit"/>.</param>
        /// <param name = "second">The second <see cref = "Commit"/>.</param>
        /// <returns>The common ancestor or null if none found.</returns>
        public virtual Commit FindCommonAncestor(Commit first, Commit second)
        {
            Ensure.ArgumentNotNull(first, "first");
            Ensure.ArgumentNotNull(second, "second");

            using (var osw1 = new ObjectSafeWrapper(first.Id, repo))
            using (var osw2 = new ObjectSafeWrapper(second.Id, repo))
            {
                GitOid ret;
                int result = NativeMethods.git_merge_base(out ret, repo.Handle, osw1.ObjectPtr, osw2.ObjectPtr);

                if (result == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.Success(result);

                return repo.Lookup<Commit>(new ObjectId(ret));
            }
        }

        /// <summary>
        ///   Find the best possible common ancestor given two or more <see cref="Commit"/>.
        /// </summary>
        /// <param name = "commits">The <see cref = "Commit"/>s for which to find the common ancestor.</param>
        /// <returns>The common ancestor or null if none found.</returns>
        public virtual Commit FindCommonAncestor(IEnumerable<Commit> commits)
        {
            Ensure.ArgumentNotNull(commits, "commits");

            Commit ret = null;
            int count = 0;

            foreach (var commit in commits)
            {
                if (commit == null)
                {
                    throw new ArgumentException("Enumerable contains null at position: " + count.ToString(CultureInfo.InvariantCulture), "commits");
                }

                count++;

                if (count == 1)
                {
                    ret = commit;
                    continue;
                }

                ret = FindCommonAncestor(ret, commit);
                if (ret == null)
                {
                    break;
                }
            }

            if (count < 2)
            {
                throw new ArgumentException("The enumerable must contains at least two commits.", "commits");
            }

            return ret;
        }

        /// <summary>
        ///   Stores the content of the <see cref = "Repository.Index" /> as a new <see cref = "Commit" /> into the repository.
        ///   The tip of the <see cref = "Repository.Head"/> will be used as the parent of this new Commit.
        ///   Once the commit is created, the <see cref = "Repository.Head"/> will move forward to point at it.
        /// </summary>
        /// <param name = "message">The description of why a change was made to the repository.</param>
        /// <param name = "author">The <see cref = "Signature" /> of who made the change.</param>
        /// <param name = "committer">The <see cref = "Signature" /> of who added the change to the repository.</param>
        /// <param name = "amendPreviousCommit">True to amend the current <see cref = "Commit"/> pointed at by <see cref = "Repository.Head"/>, false otherwise.</param>
        /// <returns>The generated <see cref = "Commit" />.</returns>
        [Obsolete("This method will be removed in the next release. Please use Repository.Commit() instead.")]
        public Commit Create(string message, Signature author, Signature committer, bool amendPreviousCommit)
        {
            return repo.Commit(message, author, committer, amendPreviousCommit);
        }

        private class CommitEnumerator : IEnumerator<Commit>
        {
            private readonly Repository repo;
            private readonly RevWalkerSafeHandle handle;
            private ObjectId currentOid;

            public CommitEnumerator(Repository repo, Filter filter)
            {
                this.repo = repo;
                int res = NativeMethods.git_revwalk_new(out handle, repo.Handle);
                repo.RegisterForCleanup(handle);

                Ensure.Success(res);

                Sort(filter.SortBy);
                Push(filter.SinceList);
                Hide(filter.UntilList);

                if(!string.IsNullOrEmpty(filter.SinceGlob))
                {
                    Ensure.Success(NativeMethods.git_revwalk_push_glob(handle, filter.SinceGlob));
                }

                if(!string.IsNullOrEmpty(filter.UntilGlob))
                {
                    Ensure.Success(NativeMethods.git_revwalk_hide_glob(handle, filter.UntilGlob));
                }
            }

            #region IEnumerator<Commit> Members

            public Commit Current
            {
                get { return repo.Lookup<Commit>(currentOid); }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                GitOid oid;
                int res = NativeMethods.git_revwalk_next(out oid, handle);

                if (res == (int)GitErrorCode.RevWalkOver)
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
                handle.SafeDispose();
            }

            private delegate int HidePushSignature(RevWalkerSafeHandle handle, ref GitOid oid);

            private void InternalHidePush(IList<object> identifier, HidePushSignature hidePush)
            {
                IEnumerable<ObjectId> oids = RetrieveCommitOids(identifier).TakeWhile(o => o != null);

                foreach (ObjectId actedOn in oids)
                {
                    GitOid oid = actedOn.Oid;
                    int res = hidePush(handle, ref oid);
                    Ensure.Success(res);
                }
            }

            private void Push(IList<object> identifier)
            {
                InternalHidePush(identifier, NativeMethods.git_revwalk_push);
            }

            private void Hide(IList<object> identifier)
            {
                if (identifier == null)
                {
                    return;
                }

                InternalHidePush(identifier, NativeMethods.git_revwalk_hide);
            }

            private void Sort(GitSortOptions options)
            {
                NativeMethods.git_revwalk_sorting(handle, options);
            }

            private ObjectId DereferenceToCommit(string identifier)
            {
                // TODO: Should we check the type? Git-log allows TagAnnotation oid as parameter. But what about Blobs and Trees?
                GitObject commit = repo.Lookup(identifier, GitObjectType.Any, LookUpOptions.ThrowWhenNoGitObjectHasBeenFound | LookUpOptions.DereferenceResultToCommit);

                return commit != null ? commit.Id : null;
            }

            private IEnumerable<ObjectId> RetrieveCommitOids(object identifier)
            {
                if (identifier is string)
                {
                    yield return DereferenceToCommit(identifier as string);
                    yield break;
                }

                if (identifier is ObjectId)
                {
                    yield return DereferenceToCommit(((ObjectId)identifier).Sha);
                    yield break;
                }

                if (identifier is Commit)
                {
                    yield return ((Commit)identifier).Id;
                    yield break;
                }

                if (identifier is TagAnnotation)
                {
                    yield return DereferenceToCommit(((TagAnnotation)identifier).Target.Id.Sha);
                    yield break;
                }

                if (identifier is Tag)
                {
                    yield return DereferenceToCommit(((Tag)identifier).Target.Id.Sha);
                    yield break;
                }

                if (identifier is Branch)
                {
                    var branch = (Branch)identifier;
                    Ensure.GitObjectIsNotNull(branch.Tip, branch.CanonicalName);

                    yield return branch.Tip.Id;
                    yield break;
                }

                if (identifier is Reference)
                {
                    yield return DereferenceToCommit(((Reference)identifier).CanonicalName);
                    yield break;
                }

                if (identifier is IEnumerable)
                {
                    var enumerable = (IEnumerable)identifier;

                    foreach (object entry in enumerable)
                    {
                        foreach (ObjectId oid in RetrieveCommitOids(entry))
                        {
                            yield return oid;
                        }
                    }

                    yield break;
                }

                throw new LibGit2SharpException(string.Format(CultureInfo.InvariantCulture, "Unexpected kind of identifier '{0}'.", identifier));
            }
        }
    }
}
