using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides helper overloads to a <see cref="Repository"/>.
    /// </summary>
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Try to lookup an object by its sha or a reference name.
        /// </summary>
        /// <typeparam name="T">The kind of <see cref="GitObject"/> to lookup.</typeparam>
        /// <param name="repository">The <see cref="Repository"/> being looked up.</param>
        /// <param name="objectish">The revparse spec for the object to lookup.</param>
        /// <returns>The retrieved <see cref="GitObject"/>, or <c>null</c> if none was found.</returns>
        public static T Lookup<T>(this IRepository repository, string objectish) where T : GitObject
        {
            EnsureNoGitLink<T>();

            if (typeof(T) == typeof(GitObject))
            {
                return (T)repository.Lookup(objectish);
            }

            return (T)repository.Lookup(objectish, GitObject.TypeToKindMap[typeof(T)]);
        }

        /// <summary>
        /// Try to lookup an object by its <see cref="ObjectId"/>.
        /// </summary>
        /// <typeparam name="T">The kind of <see cref="GitObject"/> to lookup.</typeparam>
        /// <param name="repository">The <see cref="Repository"/> being looked up.</param>
        /// <param name="id">The id.</param>
        /// <returns>The retrieved <see cref="GitObject"/>, or <c>null</c> if none was found.</returns>
        public static T Lookup<T>(this IRepository repository, ObjectId id) where T : GitObject
        {
            EnsureNoGitLink<T>();

            if (typeof(T) == typeof(GitObject))
            {
                return (T)repository.Lookup(id);
            }

            return (T)repository.Lookup(id, GitObject.TypeToKindMap[typeof(T)]);
        }

        private static void EnsureNoGitLink<T>() where T : GitObject
        {
            if (typeof(T) != typeof(GitLink))
            {
                return;
            }

            throw new ArgumentException("A GitObject of type 'GitLink' cannot be looked up.");
        }

        /// <summary>
        /// Creates a lightweight tag with the specified name. This tag will point at the commit pointed at by the <see cref="Repository.Head"/>.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="tagName">The name of the tag to create.</param>
        public static Tag ApplyTag(this IRepository repository, string tagName)
        {
            return repository.Tags.Add(tagName, RetrieveHeadCommit(repository));
        }

        /// <summary>
        /// Creates a lightweight tag with the specified name. This tag will point at the <paramref name="objectish"/>.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="tagName">The name of the tag to create.</param>
        /// <param name="objectish">The revparse spec for the target object.</param>
        public static Tag ApplyTag(this IRepository repository, string tagName, string objectish)
        {
            return repository.Tags.Add(tagName, objectish);
        }

        /// <summary>
        /// Creates an annotated tag with the specified name. This tag will point at the commit pointed at by the <see cref="Repository.Head"/>.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="tagName">The name of the tag to create.</param>
        /// <param name="tagger">The identity of the creator of this tag.</param>
        /// <param name="message">The annotation message.</param>
        public static Tag ApplyTag(this IRepository repository, string tagName, Signature tagger, string message)
        {
            return repository.Tags.Add(tagName, RetrieveHeadCommit(repository), tagger, message);
        }

        private static Commit RetrieveHeadCommit(IRepository repository)
        {
            Commit commit = repository.Head.Tip;

            Ensure.GitObjectIsNotNull(commit, "HEAD");

            return commit;
        }

        /// <summary>
        /// Creates an annotated tag with the specified name. This tag will point at the <paramref name="objectish"/>.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="tagName">The name of the tag to create.</param>
        /// <param name="objectish">The revparse spec for the target object.</param>
        /// <param name="tagger">The identity of the creator of this tag.</param>
        /// <param name="message">The annotation message.</param>
        public static Tag ApplyTag(this IRepository repository, string tagName, string objectish, Signature tagger, string message)
        {
            return repository.Tags.Add(tagName, objectish, tagger, message);
        }

        /// <summary>
        /// Creates a branch with the specified name. This branch will point at the commit pointed at by the <see cref="Repository.Head"/>.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="branchName">The name of the branch to create.</param>
        public static Branch CreateBranch(this IRepository repository, string branchName)
        {
            var head = repository.Head;
            var reflogName = head is DetachedHead ? head.Tip.Sha : head.FriendlyName;

            return CreateBranch(repository, branchName, reflogName);
        }

        /// <summary>
        /// Creates a branch with the specified name. This branch will point at <paramref name="target"/>.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="branchName">The name of the branch to create.</param>
        /// <param name="target">The commit which should be pointed at by the Branch.</param>
        public static Branch CreateBranch(this IRepository repository, string branchName, Commit target)
        {
            return repository.Branches.Add(branchName, target);
        }

        /// <summary>
        /// Creates a branch with the specified name. This branch will point at the commit pointed at by the <see cref="Repository.Head"/>.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="branchName">The name of the branch to create.</param>
        /// <param name="committish">The revparse spec for the target commit.</param>
        public static Branch CreateBranch(this IRepository repository, string branchName, string committish)
        {
            return repository.Branches.Add(branchName, committish);
        }

        /// <summary>
        /// Sets the current <see cref="Repository.Head"/> and resets the <see cref="Index"/> and
        /// the content of the working tree to match.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="resetMode">Flavor of reset operation to perform.</param>
        public static void Reset(this IRepository repository, ResetMode resetMode)
        {
            repository.Reset(resetMode, "HEAD");
        }

        /// <summary>
        /// Sets the current <see cref="Repository.Head"/> to the specified commitish and optionally resets the <see cref="Index"/> and
        /// the content of the working tree to match.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="resetMode">Flavor of reset operation to perform.</param>
        /// <param name="committish">A revparse spec for the target commit object.</param>
        public static void Reset(this IRepository repository, ResetMode resetMode, string committish)
        {
            Ensure.ArgumentNotNullOrEmptyString(committish, "committish");

            Commit commit = LookUpCommit(repository, committish);

            repository.Reset(resetMode, commit);
        }

        private static Commit LookUpCommit(IRepository repository, string committish)
        {
            GitObject obj = repository.Lookup(committish);
            Ensure.GitObjectIsNotNull(obj, committish);
            return obj.Peel<Commit>(true);
        }

        /// <summary>
        /// Stores the content of the <see cref="Repository.Index"/> as a new <see cref="LibGit2Sharp.Commit"/> into the repository.
        /// The tip of the <see cref="Repository.Head"/> will be used as the parent of this new Commit.
        /// Once the commit is created, the <see cref="Repository.Head"/> will move forward to point at it.
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="message">The description of why a change was made to the repository.</param>
        /// <param name="author">The <see cref="Signature"/> of who made the change.</param>
        /// <param name="committer">The <see cref="Signature"/> of who added the change to the repository.</param>
        /// <returns>The generated <see cref="LibGit2Sharp.Commit"/>.</returns>
        public static Commit Commit(this IRepository repository, string message, Signature author, Signature committer)
        {
            return repository.Commit(message, author, committer, default(CommitOptions));
        }

        internal static string BuildRelativePathFrom(this IRepository repo, string path)
        {
            //TODO: To be removed when libgit2 natively implements this
            if (!Path.IsPathRooted(path))
            {
                return path;
            }

            string normalizedPath = Path.GetFullPath(path);

            if (!PathStartsWith(repo, normalizedPath, repo.Info.WorkingDirectory))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                                                          "Unable to process file '{0}'. This file is not located under the working directory of the repository ('{1}').",
                                                          normalizedPath,
                                                          repo.Info.WorkingDirectory));
            }

            return normalizedPath.Substring(repo.Info.WorkingDirectory.Length);
        }

        internal static bool PathStartsWith(IRepository repository, string path, string value)
        {
            var pathCase = new PathCase(repository);
            return pathCase.StartsWith(path, value);
        }

        private static ObjectId DereferenceToCommit(Repository repo, string identifier)
        {
            var options = LookUpOptions.DereferenceResultToCommit;

            if (!AllowOrphanReference(repo, identifier))
            {
                options |= LookUpOptions.ThrowWhenNoGitObjectHasBeenFound;
            }

            // TODO: Should we check the type? Git-log allows TagAnnotation oid as parameter. But what about Blobs and Trees?
            GitObject commit = repo.Lookup(identifier, GitObjectType.Any, options);

            return commit != null ? commit.Id : null;
        }

        private static bool AllowOrphanReference(IRepository repo, string identifier)
        {
            return string.Equals(identifier, "HEAD", StringComparison.Ordinal)
                   || string.Equals(identifier, repo.Head.CanonicalName, StringComparison.Ordinal);
        }

        private static ObjectId SingleCommittish(this Repository repo, object identifier)
        {
            if (ReferenceEquals(identifier, null))
            {
                return null;
            }

            if (identifier is string)
            {
                return DereferenceToCommit(repo, (string)identifier);
            }

            if (identifier is ObjectId)
            {
                return DereferenceToCommit(repo, ((ObjectId)identifier).Sha);
            }

            if (identifier is Commit)
            {
                return ((Commit)identifier).Id;
            }

            if (identifier is TagAnnotation)
            {
                return DereferenceToCommit(repo, ((TagAnnotation)identifier).Target.Id.Sha);
            }

            if (identifier is Tag)
            {
                return DereferenceToCommit(repo, ((Tag)identifier).Target.Id.Sha);
            }

            var branch = identifier as Branch;
            if (branch != null)
            {
                if (branch.Tip != null || !branch.IsCurrentRepositoryHead)
                {
                    Ensure.GitObjectIsNotNull(branch.Tip, branch.CanonicalName);
                    return branch.Tip.Id;
                }
            }

            if (identifier is Reference)
            {
                return DereferenceToCommit(repo, ((Reference)identifier).CanonicalName);
            }

            return null;
        }

        /// <summary>
        /// Dereferences the passed identifier to a commit. If the identifier is enumerable, all items are dereferenced.
        /// </summary>
        /// <param name="repo">Repository to search</param>
        /// <param name="identifier">Committish to dereference</param>
        /// <param name="throwIfNotFound">If true, allow thrown exceptions to propagate. If false, exceptions will be swallowed and null returned.</param>
        /// <returns>A series of commit <see cref="ObjectId"/>s which identify commit objects.</returns>
        internal static IEnumerable<ObjectId> Committishes(this Repository repo, object identifier, bool throwIfNotFound = false)
        {
            var singleReturnValue = repo.SingleCommittish(identifier);

            if (singleReturnValue != null)
            {
                yield return singleReturnValue;
                yield break;
            }

            if (identifier is IEnumerable)
            {
                foreach (object entry in (IEnumerable)identifier)
                {
                    foreach (ObjectId oid in Committishes(repo, entry))
                    {
                        yield return oid;
                    }
                }

                yield break;
            }

            if (throwIfNotFound)
            {
                throw new LibGit2SharpException("Unexpected kind of identifier '{0}'.", identifier);
            }

            yield return null;
        }

        /// <summary>
        /// Dereference the identifier to a commit. If the identifier is enumerable, dereference the first element.
        /// </summary>
        /// <param name="repo">The <see cref="Repository"/> to search</param>
        /// <param name="identifier">Committish to dereference</param>
        /// <returns>An <see cref="ObjectId"/> for a commit object.</returns>
        internal static ObjectId Committish(this Repository repo, object identifier)
        {
            return repo.Committishes(identifier, true).First();
        }

        /// <summary>
        /// Merges changes from branch into the branch pointed at by HEAD.
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="branch">The branch to merge into the branch pointed at by HEAD.</param>
        /// <param name="merger">The <see cref="Signature"/> of who is performing the merge.</param>
        /// <returns>The <see cref="MergeResult"/> of the merge.</returns>
        public static MergeResult Merge(this IRepository repository, Branch branch, Signature merger)
        {
            return repository.Merge(branch, merger, null);
        }

        /// <summary>
        /// Merges changes from the commit into the branch pointed at by HEAD.
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="committish">The commit to merge into the branch pointed at by HEAD.</param>
        /// <param name="merger">The <see cref="Signature"/> of who is performing the merge.</param>
        /// <returns>The <see cref="MergeResult"/> of the merge.</returns>
        public static MergeResult Merge(this IRepository repository, string committish, Signature merger)
        {
            return repository.Merge(committish, merger, null);
        }

        /// <summary>
        /// Updates specifed paths in the index and working directory with the versions from the specified branch, reference, or SHA.
        /// <para>
        /// This method does not switch branches or update the current repository HEAD.
        /// </para>
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name = "committishOrBranchSpec">A revparse spec for the commit or branch to checkout paths from.</param>
        /// <param name="paths">The paths to checkout. Will throw if null is passed in. Passing an empty enumeration results in nothing being checked out.</param>
        public static void CheckoutPaths(this IRepository repository, string committishOrBranchSpec, IEnumerable<string> paths)
        {
            repository.CheckoutPaths(committishOrBranchSpec, paths, null);
        }

        /// <summary>
        /// Sets the current <see cref="IRepository.Head"/> to the specified commit and optionally resets the <see cref="Index"/> and
        /// the content of the working tree to match.
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="resetMode">Flavor of reset operation to perform.</param>
        /// <param name="commit">The target commit object.</param>
        public static void Reset(this IRepository repository, ResetMode resetMode, Commit commit)
        {
            repository.Reset(resetMode, commit);
        }

        /// <summary>
        /// Find where each line of a file originated.
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="path">Path of the file to blame.</param>
        /// <returns>The blame for the file.</returns>
        public static BlameHunkCollection Blame(this IRepository repository, string path)
        {
            return repository.Blame(path, null);
        }

        /// <summary>
        /// Cherry-picks the specified commit.
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="commit">The <see cref="LibGit2Sharp.Commit"/> to cherry-pick.</param>
        /// <param name="committer">The <see cref="Signature"/> of who is performing the cherry pick.</param>
        /// <returns>The result of the cherry pick.</returns>
        public static CherryPickResult CherryPick(this IRepository repository, Commit commit, Signature committer)
        {
            return repository.CherryPick(commit, committer, null);
        }

        /// <summary>
        /// Merges changes from commit into the branch pointed at by HEAD.
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="commit">The commit to merge into the branch pointed at by HEAD.</param>
        /// <param name="merger">The <see cref="Signature"/> of who is performing the merge.</param>
        /// <returns>The <see cref="MergeResult"/> of the merge.</returns>
        public static MergeResult Merge(this IRepository repository, Commit commit, Signature merger)
        {
            return repository.Merge(commit, merger, null);
        }

        /// <summary>
        /// Revert the specified commit.
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="commit">The <see cref="LibGit2Sharp.Commit"/> to revert.</param>
        /// <param name="reverter">The <see cref="Signature"/> of who is performing the revert.</param>
        /// <returns>The result of the revert.</returns>
        public static RevertResult Revert(this IRepository repository, Commit commit, Signature reverter)
        {
            return repository.Revert(commit, reverter, null);
        }

        /// <summary>
        /// Retrieves the state of all files in the working directory, comparing them against the staging area and the latest commit.
        /// </summary>
        /// <returns>A <see cref="RepositoryStatus"/> holding the state of all the files.</returns>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        public static RepositoryStatus RetrieveStatus(this IRepository repository)
        {
            Proxy.git_index_read(repository.Index.Handle);
            return new RepositoryStatus((Repository)repository, null);
        }

        /// <summary>
        /// Finds the most recent annotated tag that is reachable from a commit.
        /// <para>
        ///   If the tag points to the commit, then only the tag is shown. Otherwise,
        ///   it suffixes the tag name with the number of additional commits on top
        ///   of the tagged object and the abbreviated object name of the most recent commit.
        /// </para>
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="commit">The commit to be described.</param>
        /// <returns>A descriptive identifier for the commit based on the nearest annotated tag.</returns>
        public static string Describe(this IRepository repository, Commit commit)
        {
            return repository.Describe(commit, new DescribeOptions());
        }
    }
}
