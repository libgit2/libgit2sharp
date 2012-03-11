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
        private IList<object> includedIdentifier = new List<object> { "HEAD" };
        private IList<object> excludedIdentifier = new List<object>();
        private readonly GitSortOptions sortOptions;

        /// <summary>
        ///   Initializes a new instance of the <see cref = "CommitCollection" /> class.
        ///   The commits will be enumerated according in reverse chronological order.
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
        ///   Gets the current sorting strategy applied when enumerating the collection
        /// </summary>
        public GitSortOptions SortedBy
        {
            get { return sortOptions; }
        }

        #region IEnumerable<Commit> Members

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public IEnumerator<Commit> GetEnumerator()
        {
            if ((repo.Info.IsEmpty) && includedIdentifier.Any(o => PointsAtTheHead(o.ToString()))) // TODO: ToString() == fragile
            {
                return Enumerable.Empty<Commit>().GetEnumerator();
            }

            return new CommitEnumerator(repo, includedIdentifier, excludedIdentifier, sortOptions);
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        ///   Returns the list of commits of the repository matching the specified <paramref name = "filter" />.
        /// </summary>
        /// <param name = "filter">The options used to control which commits will be returned.</param>
        /// <returns>A collection of commits, ready to be enumerated.</returns>
        public ICommitCollection QueryBy(Filter filter)
        {
            Ensure.ArgumentNotNull(filter, "filter");
            Ensure.ArgumentNotNull(filter.Since, "filter.Since");
            Ensure.ArgumentNotNullOrEmptyString(filter.Since.ToString(), "filter.Since");

            return new CommitCollection(repo, filter.SortBy)
                       {
                           includedIdentifier = ToList(filter.Since),
                           excludedIdentifier = ToList(filter.Until)
                       };
        }

        private static IList<object> ToList(object obj)
        {
            var list = new List<object>();

            if (obj == null)
            {
                return list;
            }

            var types = new[]
                            {
                                typeof(string), typeof(ObjectId),
                                typeof(Commit), typeof(TagAnnotation),
                                typeof(Tag), typeof(Branch), typeof(DetachedHead),
                                typeof(Reference), typeof(DirectReference), typeof(SymbolicReference)
                            };

            if (types.Contains(obj.GetType()))
            {
                list.Add(obj);
                return list;
            }

            list.AddRange(((IEnumerable)obj).Cast<object>());
            return list;
        }

        private static bool PointsAtTheHead(string shaOrRefName)
        {
            return ("HEAD".Equals(shaOrRefName, StringComparison.Ordinal) || "refs/heads/master".Equals(shaOrRefName, StringComparison.Ordinal));
        }

        /// <summary>
        ///   Stores the content of the <see cref = "Repository.Index" /> as a new <see cref = "Commit" /> into the repository.
        /// </summary>
        /// <param name = "message">The description of why a change was made to the repository.</param>
        /// <param name = "author">The <see cref = "Signature" /> of who made the change.</param>
        /// <param name = "committer">The <see cref = "Signature" /> of who added the change to the repository.</param>
        /// <param name="amendPreviousCommit">True to amend the current <see cref="Commit"/> pointed at by <see cref="Repository.Head"/>, false otherwise.</param>
        /// <returns>The generated <see cref = "Commit" />.</returns>
        public Commit Create(string message, Signature author, Signature committer, bool amendPreviousCommit)
        {
            Ensure.ArgumentNotNull(message, "message");
            Ensure.ArgumentNotNull(author, "author");
            Ensure.ArgumentNotNull(committer, "committer");

            if (amendPreviousCommit && repo.Info.IsEmpty)
            {
                throw new LibGit2Exception("Can not amend anything. The Head doesn't point at any commit.");
            }

            GitOid treeOid;
            int res = NativeMethods.git_tree_create_fromindex(out treeOid, repo.Index.Handle);
            Ensure.Success(res);

            var parentIds = RetrieveParentIdsOfTheCommitBeingCreated(repo, amendPreviousCommit);

            GitOid commitOid;
            using (var treePtr = new ObjectSafeWrapper(new ObjectId(treeOid), repo))
            using (var parentObjectPtrs = new DisposableEnumerable<ObjectSafeWrapper>(parentIds.Select(id => new ObjectSafeWrapper(id, repo))))
            using (SignatureSafeHandle authorHandle = author.BuildHandle())
            using (SignatureSafeHandle committerHandle = committer.BuildHandle())
            {
                string encoding = null; //TODO: Handle the encoding of the commit to be created

                IntPtr[] parentsPtrs = parentObjectPtrs.Select(o => o.ObjectPtr ).ToArray();
                res = NativeMethods.git_commit_create(out commitOid, repo.Handle, repo.Refs["HEAD"].CanonicalName, authorHandle,
                                                      committerHandle, encoding, message, treePtr.ObjectPtr, parentObjectPtrs.Count(), parentsPtrs);
                Ensure.Success(res);
            }

            return repo.Lookup<Commit>(new ObjectId(commitOid));
        }

        private static IEnumerable<ObjectId> RetrieveParentIdsOfTheCommitBeingCreated(Repository repo, bool amendPreviousCommit)
        {
            if (amendPreviousCommit)
            {
                return repo.Head.Tip.Parents.Select(c => c.Id);
            }

            if (repo.Info.IsEmpty)
            {
                return Enumerable.Empty<ObjectId>();
            }

            return new[] { repo.Head.Tip.Id };
        }

        private class CommitEnumerator : IEnumerator<Commit>
        {
            private readonly Repository repo;
            private readonly RevWalkerSafeHandle handle;
            private ObjectId currentOid;

            public CommitEnumerator(Repository repo, IList<object> includedIdentifier, IList<object> excludedIdentifier, GitSortOptions sortingStrategy)
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
                    Ensure.GitObjectIsNotNull(branch.Tip as GitObject, branch.CanonicalName);

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

                throw new LibGit2Exception(string.Format(CultureInfo.InvariantCulture, "Unexpected kind of identifier '{0}'.", identifier));
            }
        }
    }
}
