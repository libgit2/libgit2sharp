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
    /// A log of commits in a <see cref="Repository"/>
    /// </summary>
    public sealed class CommitLog : IQueryableCommitLog
    {
        private readonly Repository repo;
        private readonly CommitFilter queryFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitLog"/> class.
        /// The commits will be enumerated according in reverse chronological order.
        /// </summary>
        /// <param name="repo">The repository.</param>
        internal CommitLog(Repository repo)
            : this(repo, new CommitFilter())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommitLog"/> class.
        /// </summary>
        /// <param name="repo">The repository.</param>
        /// <param name="queryFilter">The filter to use in querying commits</param>
        internal CommitLog(Repository repo, CommitFilter queryFilter)
        {
            this.repo = repo;
            this.queryFilter = queryFilter;
        }

        /// <summary>
        /// Gets the current sorting strategy applied when enumerating the log
        /// </summary>
        public CommitSortStrategies SortedBy
        {
            get { return queryFilter.SortBy; }
        }

        #region IEnumerable<Commit> Members

        /// <summary>
        /// Returns an enumerator that iterates through the log.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the log.</returns>
        public IEnumerator<Commit> GetEnumerator()
        {
            return new CommitEnumerator(repo, queryFilter);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the log.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the log.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Returns the list of commits of the repository matching the specified <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter">The options used to control which commits will be returned.</param>
        /// <returns>A list of commits, ready to be enumerated.</returns>
        public ICommitLog QueryBy(CommitFilter filter)
        {
            Ensure.ArgumentNotNull(filter, "filter");
            Ensure.ArgumentNotNull(filter.Since, "filter.Since");
            Ensure.ArgumentNotNullOrEmptyString(filter.Since.ToString(), "filter.Since");

            return new CommitLog(repo, filter);
        }

        /// <summary>
        /// Find the best possible common ancestor given two <see cref="Commit"/>s.
        /// </summary>
        /// <param name="first">The first <see cref="Commit"/>.</param>
        /// <param name="second">The second <see cref="Commit"/>.</param>
        /// <returns>The common ancestor or null if none found.</returns>
        public Commit FindCommonAncestor(Commit first, Commit second)
        {
            Ensure.ArgumentNotNull(first, "first");
            Ensure.ArgumentNotNull(second, "second");

            ObjectId id = Proxy.git_merge_base(repo.Handle, first, second);

            return id == null ? null : repo.Lookup<Commit>(id);
        }

        /// <summary>
        /// Find the best possible common ancestor given two or more <see cref="Commit"/>.
        /// </summary>
        /// <param name="commits">The <see cref="Commit"/>s for which to find the common ancestor.</param>
        /// <returns>The common ancestor or null if none found.</returns>
        public Commit FindCommonAncestor(IEnumerable<Commit> commits)
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

        private class CommitEnumerator : IEnumerator<Commit>
        {
            private readonly Repository repo;
            private readonly RevWalkerSafeHandle handle;
            private ObjectId currentOid;

            public CommitEnumerator(Repository repo, CommitFilter filter)
            {
                this.repo = repo;
                handle = Proxy.git_revwalk_new(repo.Handle);
                repo.RegisterForCleanup(handle);

                Sort(filter.SortBy);
                Push(filter.SinceList);
                Hide(filter.UntilList);
                FirstParentOnly(filter.FirstParentOnly);
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
                ObjectId id = Proxy.git_revwalk_next(handle);

                if (id == null)
                {
                    return false;
                }

                currentOid = id;

                return true;
            }

            public void Reset()
            {
                Proxy.git_revwalk_reset(handle);
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

            private delegate void HidePushSignature(RevWalkerSafeHandle handle, ObjectId id);

            private void InternalHidePush(IList<object> identifier, HidePushSignature hidePush)
            {
                IEnumerable<ObjectId> oids = repo.Committishes(identifier).TakeWhile(o => o != null);

                foreach (ObjectId actedOn in oids)
                {
                    hidePush(handle, actedOn);
                }
            }

            private void Push(IList<object> identifier)
            {
                InternalHidePush(identifier, Proxy.git_revwalk_push);
            }

            private void Hide(IList<object> identifier)
            {
                if (identifier == null)
                {
                    return;
                }

                InternalHidePush(identifier, Proxy.git_revwalk_hide);
            }

            private void Sort(CommitSortStrategies options)
            {
                Proxy.git_revwalk_sorting(handle, options);
            }

            private void FirstParentOnly(bool firstParent)
            {
                if (firstParent)
                {
                    Proxy.git_revwalk_simplify_first_parent(handle);
                }
            }
        }
    }
}
