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

            if (typeof (T) == typeof (GitObject))
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

        /// <summary>
        /// Replaces entries in the <see cref="Index"/> with entries from the specified commit.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="committish">A revparse spec for the target commit object.</param>
        /// <param name="paths">The list of paths (either files or directories) that should be considered.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        [Obsolete("This method will be removed in the next release. Please use Index.Replace() instead.")]
        public static void Reset(this IRepository repository, string committish = "HEAD", IEnumerable<string> paths = null, ExplicitPathsOptions explicitPathsOptions = null)
        {
            if (repository.Info.IsBare)
            {
                throw new BareRepositoryException("Reset is not allowed in a bare repository");
            }

            Ensure.ArgumentNotNullOrEmptyString(committish, "committish");

            Commit commit = LookUpCommit(repository, committish);

            repository.Index.Replace(commit, paths, explicitPathsOptions);
        }

        private static Commit LookUpCommit(IRepository repository, string committish)
        {
            GitObject obj = repository.Lookup(committish);
            Ensure.GitObjectIsNotNull(obj, committish);
            return obj.DereferenceToCommit(true);
        }

        /// <summary>
        /// Stores the content of the <see cref="Repository.Index"/> as a new <see cref="LibGit2Sharp.Commit"/> into the repository.
        /// The tip of the <see cref="Repository.Head"/> will be used as the parent of this new Commit.
        /// Once the commit is created, the <see cref="Repository.Head"/> will move forward to point at it.
        /// <para>Both the Author and Committer will be guessed from the Git configuration. An exception will be raised if no configuration is reachable.</para>
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="message">The description of why a change was made to the repository.</param>
        /// <returns>The generated <see cref="LibGit2Sharp.Commit"/>.</returns>
        [Obsolete("This method will be removed in the next release. Please use Commit(string, Signature, Signature) instead.")]
        public static Commit Commit(this IRepository repository, string message)
        {
            return repository.Commit(message, (CommitOptions)null);
        }

        /// <summary>
        /// Stores the content of the <see cref="Repository.Index"/> as a new <see cref="LibGit2Sharp.Commit"/> into the repository.
        /// The tip of the <see cref="Repository.Head"/> will be used as the parent of this new Commit.
        /// Once the commit is created, the <see cref="Repository.Head"/> will move forward to point at it.
        /// <para>Both the Author and Committer will be guessed from the Git configuration. An exception will be raised if no configuration is reachable.</para>
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="message">The description of why a change was made to the repository.</param>
        /// <param name="options">The <see cref="CommitOptions"/> that specify the commit behavior.</param>
        /// <returns>The generated <see cref="LibGit2Sharp.Commit"/>.</returns>
        [Obsolete("This method will be removed in the next release. Please use Commit(string, Signature, Signature, CommitOptions) instead.")]
        public static Commit Commit(this IRepository repository, string message, CommitOptions options)
        {
            Signature author = repository.Config.BuildSignatureOrThrow(DateTimeOffset.Now);

            return repository.Commit(message, author, options);
        }

        /// <summary>
        /// Stores the content of the <see cref="Repository.Index"/> as a new <see cref="LibGit2Sharp.Commit"/> into the repository.
        /// The tip of the <see cref="Repository.Head"/> will be used as the parent of this new Commit.
        /// Once the commit is created, the <see cref="Repository.Head"/> will move forward to point at it.
        /// <para>The Committer will be guessed from the Git configuration. An exception will be raised if no configuration is reachable.</para>
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="author">The <see cref="Signature"/> of who made the change.</param>
        /// <param name="message">The description of why a change was made to the repository.</param>
        /// <returns>The generated <see cref="LibGit2Sharp.Commit"/>.</returns>
        [Obsolete("This method will be removed in the next release. Please use Commit(string, Signature, Signature) instead.")]
        public static Commit Commit(this IRepository repository, string message, Signature author)
        {
            return repository.Commit(message, author, (CommitOptions)null);
        }

        /// <summary>
        /// Stores the content of the <see cref="Repository.Index"/> as a new <see cref="LibGit2Sharp.Commit"/> into the repository.
        /// The tip of the <see cref="Repository.Head"/> will be used as the parent of this new Commit.
        /// Once the commit is created, the <see cref="Repository.Head"/> will move forward to point at it.
        /// <para>The Committer will be guessed from the Git configuration. An exception will be raised if no configuration is reachable.</para>
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="author">The <see cref="Signature"/> of who made the change.</param>
        /// <param name="message">The description of why a change was made to the repository.</param>
        /// <param name="options">The <see cref="CommitOptions"/> that specify the commit behavior.</param>
        /// <returns>The generated <see cref="LibGit2Sharp.Commit"/>.</returns>
        [Obsolete("This method will be removed in the next release. Please use Commit(string, Signature, Signature, CommitOptions) instead.")]
        public static Commit Commit(this IRepository repository, string message, Signature author, CommitOptions options)
        {
            Signature committer = repository.Config.BuildSignatureOrThrow(DateTimeOffset.Now);

            return repository.Commit(message, author, committer, options);
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

        /// <summary>
        /// Fetch from the specified remote.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="remoteName">The name of the <see cref="Remote"/> to fetch from.</param>
        public static void Fetch(this IRepository repository, string remoteName)
        {
            repository.Fetch(remoteName, null);
        }

        /// <summary>
        /// Fetch from the specified remote.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="remoteName">The name of the <see cref="Remote"/> to fetch from.</param>
        /// <param name="options"><see cref="FetchOptions"/> controlling fetch behavior</param>
        public static void Fetch(this IRepository repository, string remoteName, FetchOptions options)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNullOrEmptyString(remoteName, "remoteName");

            Remote remote = repository.Network.Remotes.RemoteForName(remoteName, true);
            repository.Network.Fetch(remote, options);
        }

        /// <summary>
        /// Checkout the specified <see cref="Branch"/>, reference or SHA.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="commitOrBranchSpec">A revparse spec for the commit or branch to checkout.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public static Branch Checkout(this IRepository repository, string commitOrBranchSpec)
        {
            CheckoutOptions options = new CheckoutOptions();
            return repository.Checkout(commitOrBranchSpec, options);
        }

        /// <summary>
        /// Checkout the commit pointed at by the tip of the specified <see cref="Branch"/>.
        /// <para>
        ///   If this commit is the current tip of the branch as it exists in the repository, the HEAD
        ///   will point to this branch. Otherwise, the HEAD will be detached, pointing at the commit sha.
        /// </para>
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="branch">The <see cref="Branch"/> to check out.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public static Branch Checkout(this IRepository repository, Branch branch)
        {
            CheckoutOptions options = new CheckoutOptions();
            return repository.Checkout(branch, options);
        }

        /// <summary>
        /// Checkout the specified <see cref="LibGit2Sharp.Commit"/>.
        /// <para>
        ///   Will detach the HEAD and make it point to this commit sha.
        /// </para>
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="commit">The <see cref="LibGit2Sharp.Commit"/> to check out.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public static Branch Checkout(this IRepository repository, Commit commit)
        {
            CheckoutOptions options = new CheckoutOptions();
            return repository.Checkout(commit, options);
        }

        internal static string BuildRelativePathFrom(this Repository repo, string path)
        {
            //TODO: To be removed when libgit2 natively implements this
            if (!Path.IsPathRooted(path))
            {
                return path;
            }

            string normalizedPath = Path.GetFullPath(path);

            if (!repo.PathStartsWith(normalizedPath, repo.Info.WorkingDirectory))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                                                          "Unable to process file '{0}'. This file is not located under the working directory of the repository ('{1}').",
                                                          normalizedPath,
                                                          repo.Info.WorkingDirectory));
            }

            return normalizedPath.Substring(repo.Info.WorkingDirectory.Length);
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
                throw new LibGit2SharpException(CultureInfo.InvariantCulture, "Unexpected kind of identifier '{0}'.", identifier);
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
        /// Checkout the tip commit of the specified <see cref="Branch"/> object. If this commit is the
        /// current tip of the branch, will checkout the named branch. Otherwise, will checkout the tip commit
        /// as a detached HEAD.
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="branch">The <see cref="Branch"/> to check out.</param>
        /// <param name="options"><see cref="CheckoutOptions"/> controlling checkout behavior.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public static Branch Checkout(this IRepository repository, Branch branch, CheckoutOptions options)
        {
            return repository.Checkout(branch, options);
        }

        /// <summary>
        /// Checkout the specified <see cref="LibGit2Sharp.Commit"/>.
        /// <para>
        ///   Will detach the HEAD and make it point to this commit sha.
        /// </para>
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="commit">The <see cref="LibGit2Sharp.Commit"/> to check out.</param>
        /// <param name="options"><see cref="CheckoutOptions"/> controlling checkout behavior.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public static Branch Checkout(this IRepository repository, Commit commit, CheckoutOptions options)
        {
            return repository.Checkout(commit, options);
        }

        /// <summary>
        /// Checkout the specified <see cref="Branch"/>, reference or SHA.
        /// <para>
        ///   If the committishOrBranchSpec parameter resolves to a branch name, then the checked out HEAD will
        ///   will point to the branch. Otherwise, the HEAD will be detached, pointing at the commit sha.
        /// </para>
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="committishOrBranchSpec">A revparse spec for the commit or branch to checkout.</param>
        /// <param name="options"><see cref="CheckoutOptions"/> controlling checkout behavior.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public static Branch Checkout(this IRepository repository, string committishOrBranchSpec, CheckoutOptions options)
        {
            return repository.Checkout(committishOrBranchSpec, options);
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
        /// Replaces entries in the <see cref="Repository.Index"/> with entries from the specified commit.
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="commit">The target commit object.</param>
        /// <param name="paths">The list of paths (either files or directories) that should be considered.</param>
        [Obsolete("This method will be removed in the next release. Please use Index.Replace() instead.")]
        public static void Reset(this IRepository repository, Commit commit, IEnumerable<string> paths)
        {
            repository.Index.Replace(commit, paths, null);
        }

        /// <summary>
        /// Replaces entries in the <see cref="Repository.Index"/> with entries from the specified commit.
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="commit">The target commit object.</param>
        [Obsolete("This method will be removed in the next release. Please use Index.Replace() instead.")]
        public static void Reset(this IRepository repository, Commit commit)
        {
            repository.Index.Replace(commit, null, null);
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
        /// Promotes to the staging area the latest modifications of a file in the working directory (addition, updation or removal).
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="path">The path of the file within the working directory.</param>
        public static void Stage(this IRepository repository, string path)
        {
            repository.Stage(path, null);
        }

        /// <summary>
        /// Promotes to the staging area the latest modifications of a collection of files in the working directory (addition, updation or removal).
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        public static void Stage(this IRepository repository, IEnumerable<string> paths)
        {
            repository.Stage(paths, null);
        }

        /// <summary>
        /// Removes from the staging area all the modifications of a file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="path">The path of the file within the working directory.</param>
        public static void Unstage(this IRepository repository, string path)
        {
            repository.Unstage(path, null);
        }

        /// <summary>
        /// Removes from the staging area all the modifications of a collection of file since the latest commit (addition, updation or removal).
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        public static void Unstage(this IRepository repository, IEnumerable<string> paths)
        {
            repository.Unstage(paths, null);
        }

        /// <summary>
        /// Removes a file from the staging area, and optionally removes it from the working directory as well.
        /// <para>
        ///   If the file has already been deleted from the working directory, this method will only deal
        ///   with promoting the removal to the staging area.
        /// </para>
        /// <para>
        ///   The default behavior is to remove the file from the working directory as well.
        /// </para>
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="path">The path of the file within the working directory.</param>
        public static void Remove(this IRepository repository, string path)
        {
            repository.Remove(path, true, null);
        }

        /// <summary>
        /// Removes a file from the staging area, and optionally removes it from the working directory as well.
        /// <para>
        ///   If the file has already been deleted from the working directory, this method will only deal
        ///   with promoting the removal to the staging area.
        /// </para>
        /// <para>
        ///   The default behavior is to remove the file from the working directory as well.
        /// </para>
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="path">The path of the file within the working directory.</param>
        /// <param name="removeFromWorkingDirectory">True to remove the file from the working directory, False otherwise.</param>
        public static void Remove(this IRepository repository, string path, bool removeFromWorkingDirectory)
        {
            repository.Remove(path, removeFromWorkingDirectory, null);
        }

        /// <summary>
        /// Removes a collection of fileS from the staging, and optionally removes them from the working directory as well.
        /// <para>
        ///   If a file has already been deleted from the working directory, this method will only deal
        ///   with promoting the removal to the staging area.
        /// </para>
        /// <para>
        ///   The default behavior is to remove the files from the working directory as well.
        /// </para>
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        public static void Remove(this IRepository repository, IEnumerable<string> paths)
        {
            repository.Remove(paths, true, null);
        }

        /// <summary>
        /// Removes a collection of fileS from the staging, and optionally removes them from the working directory as well.
        /// <para>
        ///   If a file has already been deleted from the working directory, this method will only deal
        ///   with promoting the removal to the staging area.
        /// </para>
        /// <para>
        ///   The default behavior is to remove the files from the working directory as well.
        /// </para>
        /// </summary>
        /// <param name="repository">The <see cref="IRepository"/> being worked with.</param>
        /// <param name="paths">The collection of paths of the files within the working directory.</param>
        /// <param name="removeFromWorkingDirectory">True to remove the files from the working directory, False otherwise.</param>
        public static void Remove(this IRepository repository, IEnumerable<string> paths, bool removeFromWorkingDirectory)
        {
            repository.Remove(paths, removeFromWorkingDirectory, null);
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
