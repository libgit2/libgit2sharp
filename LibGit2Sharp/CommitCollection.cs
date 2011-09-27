﻿using System;
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
                                typeof(Tag), typeof(Branch),
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
        /// <param name = "author">The <see cref = "Signature" /> of who made the change.</param>
        /// <param name = "committer">The <see cref = "Signature" /> of who added the change to the repository.</param>
        /// <param name = "message">The description of why a change was made to the repository.</param>
        /// <returns>The generated <see cref = "Commit" />.</returns>
        public Commit Create(Signature author, Signature committer, string message)
        {
            GitOid treeOid;
            int res = NativeMethods.git_tree_create_fromindex(out treeOid, repo.Index.Handle);
            string encoding = null;

            Ensure.Success(res);

            Reference head = repo.Refs["HEAD"];

            GitOid commitOid;
            using (var treePtr = new ObjectSafeWrapper(new ObjectId(treeOid), repo))
            using (ObjectSafeWrapper headPtr = RetrieveHeadCommitPtr(head))
            {
                IntPtr[] parentPtrs = BuildArrayFrom(headPtr);
                res = NativeMethods.git_commit_create(out commitOid, repo.Handle, head.CanonicalName, author.Handle,
                                                      committer.Handle, encoding, message, treePtr.ObjectPtr, parentPtrs.Count(), parentPtrs);
            }
            Ensure.Success(res);

            return repo.Lookup<Commit>(new ObjectId(commitOid));
        }

        private static IntPtr[] BuildArrayFrom(ObjectSafeWrapper headPtr)
        {
            if (headPtr.ObjectPtr == IntPtr.Zero)
            {
                return new IntPtr[] { };
            }

            return new[] { headPtr.ObjectPtr };
        }

        private ObjectSafeWrapper RetrieveHeadCommitPtr(Reference head)
        {
            DirectReference oidRef = head.ResolveToDirectReference();
            if (oidRef == null)
            {
                return new ObjectSafeWrapper(null, repo);
            }

            return new ObjectSafeWrapper(new ObjectId(oidRef.TargetIdentifier), repo);
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
                if (handle == null || handle.IsInvalid)
                {
                    return;
                }

                handle.Dispose();
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

            private GitObject RetrieveObject(string shaOrReferenceName)
            {
                GitObject gitObj = repo.Lookup(shaOrReferenceName);

                // TODO: Should we check the type? Git-log allows TagAnnotation oid as parameter. But what about Blobs and Trees?
                EnsureGitObjectNotNull(shaOrReferenceName, gitObj);

                return gitObj;
            }

            private static void EnsureGitObjectNotNull(string shaOrReferenceName, GitObject gitObj)
            {
                if (gitObj != null)
                {
                    return;
                }

                throw new LibGit2Exception(string.Format(CultureInfo.InvariantCulture,
                                                         "No valid git object pointed at by '{0}' exists in the repository.",
                                                         shaOrReferenceName));
            }

            private ObjectId DereferenceToCommit(string identifier)
            {
                GitObject obj = RetrieveObject(identifier);

                if (obj is Commit)
                {
                    return obj.Id;
                }

                if (obj is TagAnnotation)
                {
                    return DereferenceToCommit(((TagAnnotation)obj).Target.Sha);
                }

                if (obj is Blob || obj is Tree)
                {
                    return null;
                }

                throw new LibGit2Exception(string.Format(CultureInfo.InvariantCulture,
                                                         "The Git object pointed at by '{0}' can not be dereferenced to a commit.",
                                                         identifier));
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
                    EnsureGitObjectNotNull(branch.CanonicalName, branch.Tip);

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
