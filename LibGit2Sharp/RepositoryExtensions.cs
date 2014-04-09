using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

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
            return ApplyTag(repository, tagName, repository.Head.CanonicalName);
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
            return ApplyTag(repository, tagName, repository.Head.CanonicalName, tagger, message);
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
        /// <param name="signature">Identification for use when updating the reflog</param>
        /// <param name="logMessage">Message to append to the reflog</param>
        public static Branch CreateBranch(this IRepository repository, string branchName, Signature signature = null, string logMessage = null)
        {
            return CreateBranch(repository, branchName, "HEAD", signature, logMessage);
        }

        /// <summary>
        /// Creates a branch with the specified name. This branch will point at <paramref name="target"/>.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="branchName">The name of the branch to create.</param>
        /// <param name="target">The commit which should be pointed at by the Branch.</param>
        /// <param name="signature">Identification for use when updating the reflog</param>
        /// <param name="logMessage">Message to append to the reflog</param>
        public static Branch CreateBranch(this IRepository repository, string branchName, Commit target, Signature signature = null, string logMessage = null)
        {
            return repository.Branches.Add(branchName, target, signature, logMessage);
        }

        /// <summary>
        /// Creates a branch with the specified name. This branch will point at the commit pointed at by the <see cref="Repository.Head"/>.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="branchName">The name of the branch to create.</param>
        /// <param name="committish">The revparse spec for the target commit.</param>
        /// <param name="signature">Identification for use when updating the reflog</param>
        /// <param name="logMessage">Message to append to the reflog</param>
        public static Branch CreateBranch(this IRepository repository, string branchName, string committish, Signature signature = null, string logMessage = null)
        {
            return repository.Branches.Add(branchName, committish, signature, logMessage);
        }

        /// <summary>
        /// Sets the current <see cref="Repository.Head"/> to the specified commit and optionally resets the <see cref="Index"/> and
        /// the content of the working tree to match.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="resetMode">Flavor of reset operation to perform.</param>
        /// <param name="committish">A revparse spec for the target commit object.</param>
        /// <param name="signature">Identification for use when updating the reflog</param>
        /// <param name="logMessage">Message to append to the reflog</param>
        public static void Reset(this IRepository repository, ResetMode resetMode, string committish = "HEAD",
            Signature signature = null, string logMessage = null)
        {
            Ensure.ArgumentNotNullOrEmptyString(committish, "committish");

            Commit commit = LookUpCommit(repository, committish);

            repository.Reset(resetMode, commit, signature, logMessage);
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
        public static void Reset(this IRepository repository, string committish = "HEAD", IEnumerable<string> paths = null, ExplicitPathsOptions explicitPathsOptions = null)
        {
            if (repository.Info.IsBare)
            {
                throw new BareRepositoryException("Reset is not allowed in a bare repository");
            }

            Ensure.ArgumentNotNullOrEmptyString(committish, "committish");

            Commit commit = LookUpCommit(repository, committish);

            repository.Reset(commit, paths, explicitPathsOptions);
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
        /// <param name="options">The <see cref="CommitOptions"/> that specify the commit behavior.</param>
        /// <returns>The generated <see cref="LibGit2Sharp.Commit"/>.</returns>
        public static Commit Commit(this IRepository repository, string message, CommitOptions options = null)
        {
            Signature author = repository.Config.BuildSignature(DateTimeOffset.Now, true);

            return repository.Commit(message, author, options);
        }

        /// <summary>
        /// Stores the content of the <see cref="Repository.Index"/> as a new <see cref="LibGit2Sharp.Commit"/> into the repository.
        /// The tip of the <see cref="Repository.Head"/> will be used as the parent of this new Commit.
        /// Once the commit is created, the <see cref="Repository.Head"/> will move forward to point at it.
        /// <para>Both the Author and Committer will be guessed from the Git configuration. An exception will be raised if no configuration is reachable.</para>
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="message">The description of why a change was made to the repository.</param>
        /// <param name="amendPreviousCommit">True to amend the current <see cref="LibGit2Sharp.Commit"/> pointed at by <see cref="Repository.Head"/>, false otherwise.</param>
        /// <returns>The generated <see cref="LibGit2Sharp.Commit"/>.</returns>
        [Obsolete("This method will be removed in the next release. Please use a Commit overload that accepts a CommitOptions instead.")]
        public static Commit Commit(this IRepository repository, string message, bool amendPreviousCommit)
        {
            Signature author = repository.Config.BuildSignature(DateTimeOffset.Now, true);

            return repository.Commit(message, author, new CommitOptions { AmendPreviousCommit = amendPreviousCommit });
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
        public static Commit Commit(this IRepository repository, string message, Signature author, CommitOptions options = null)
        {
            Signature committer = repository.Config.BuildSignature(DateTimeOffset.Now, true);

            return repository.Commit(message, author, committer, options);
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
        /// <param name="amendPreviousCommit">True to amend the current <see cref="LibGit2Sharp.Commit"/> pointed at by <see cref="Repository.Head"/>, false otherwise.</param>
        /// <returns>The generated <see cref="LibGit2Sharp.Commit"/>.</returns>
        [Obsolete("This method will be removed in the next release. Please use a Commit overload that accepts a CommitOptions instead.")]
        public static Commit Commit(this IRepository repository, string message, Signature author, bool amendPreviousCommit)
        {
            return repository.Commit(message, author, new CommitOptions { AmendPreviousCommit = amendPreviousCommit });
        }

        /// <summary>
        /// Fetch from the specified remote.
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="remoteName">The name of the <see cref="Remote"/> to fetch from.</param>
        /// <param name="options"><see cref="FetchOptions"/> controlling fetch behavior</param>
        public static void Fetch(this IRepository repository, string remoteName, FetchOptions options = null)
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
        /// <param name="signature">The identity used for updating the reflog</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public static Branch Checkout(this IRepository repository, string commitOrBranchSpec, Signature signature = null)
        {
            return repository.Checkout(commitOrBranchSpec, CheckoutModifiers.None, null, null, signature);
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
        /// <param name="signature">The identity used for updating the reflog</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public static Branch Checkout(this IRepository repository, Branch branch, Signature signature = null)
        {
            return repository.Checkout(branch, CheckoutModifiers.None, null, null, signature);
        }

        /// <summary>
        /// Checkout the specified <see cref="LibGit2Sharp.Commit"/>.
        /// <para>
        ///   Will detach the HEAD and make it point to this commit sha.
        /// </para>
        /// </summary>
        /// <param name="repository">The <see cref="Repository"/> being worked with.</param>
        /// <param name="commit">The <see cref="LibGit2Sharp.Commit"/> to check out.</param>
        /// <param name="signature">The identity used for updating the reflog</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public static Branch Checkout(this IRepository repository, Commit commit, Signature signature = null)
        {
            return repository.Checkout(commit, CheckoutModifiers.None, null, null, signature);
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
                                                          normalizedPath, repo.Info.WorkingDirectory));
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
                throw new LibGit2SharpException(string.Format(CultureInfo.InvariantCulture, "Unexpected kind of identifier '{0}'.", identifier));
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
    }
}
