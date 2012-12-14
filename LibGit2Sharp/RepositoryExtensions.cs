using System;
using System.Collections.Generic;
using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides helper overloads to a <see cref = "Repository" />.
    /// </summary>
    public static class RepositoryExtensions
    {
        /// <summary>
        ///   Try to lookup an object by its sha or a reference name.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "repository">The <see cref = "Repository" /> being looked up.</param>
        /// <param name = "objectish">The revparse spec for the object to lookup.</param>
        /// <returns></returns>
        public static T Lookup<T>(this IRepository repository, string objectish) where T : GitObject
        {
            return (T)repository.Lookup(objectish, GitObject.TypeToTypeMap[typeof (T)]);
        }

        /// <summary>
        ///   Try to lookup an object by its <see cref = "ObjectId" />.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "repository">The <see cref = "Repository" /> being looked up.</param>
        /// <param name = "id">The id.</param>
        /// <returns></returns>
        public static T Lookup<T>(this IRepository repository, ObjectId id) where T : GitObject
        {
            return (T)repository.Lookup(id, GitObject.TypeToTypeMap[typeof(T)]);
        }

        /// <summary>
        ///   Creates a lightweight tag with the specified name. This tag will point at the commit pointed at by the <see cref = "Repository.Head" />.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "tagName">The name of the tag to create.</param>
        public static Tag ApplyTag(this IRepository repository, string tagName)
        {
            return ApplyTag(repository, tagName, repository.Head.CanonicalName);
        }

        /// <summary>
        ///   Creates a lightweight tag with the specified name. This tag will point at the <paramref name = "objectish" />.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "tagName">The name of the tag to create.</param>
        /// <param name = "objectish">The revparse spec for the target object.</param>
        public static Tag ApplyTag(this IRepository repository, string tagName, string objectish)
        {
            return repository.Tags.Add(tagName, objectish);
        }

        /// <summary>
        ///   Creates an annotated tag with the specified name. This tag will point at the commit pointed at by the <see cref = "Repository.Head" />.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "tagName">The name of the tag to create.</param>
        /// <param name = "tagger">The identity of the creator of this tag.</param>
        /// <param name = "message">The annotation message.</param>
        public static Tag ApplyTag(this IRepository repository, string tagName, Signature tagger, string message)
        {
            return ApplyTag(repository, tagName, repository.Head.CanonicalName, tagger, message);
        }

        /// <summary>
        ///   Creates an annotated tag with the specified name. This tag will point at the <paramref name = "objectish" />.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "tagName">The name of the tag to create.</param>
        /// <param name = "objectish">The revparse spec for the target object.</param>
        /// <param name = "tagger">The identity of the creator of this tag.</param>
        /// <param name = "message">The annotation message.</param>
        public static Tag ApplyTag(this IRepository repository, string tagName, string objectish, Signature tagger, string message)
        {
            return repository.Tags.Add(tagName, objectish, tagger, message);
        }

        /// <summary>
        ///   Creates a branch with the specified name. This branch will point at the commit pointed at by the <see cref = "Repository.Head" />.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "branchName">The name of the branch to create.</param>
        public static Branch CreateBranch(this IRepository repository, string branchName)
        {
            return CreateBranch(repository, branchName, repository.Head.Tip);
        }

        /// <summary>
        ///   Creates a branch with the specified name. This branch will point at <paramref name="target"/>.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "branchName">The name of the branch to create.</param>
        /// <param name = "target">The commit which should be pointed at by the Branch.</param>
        public static Branch CreateBranch(this IRepository repository, string branchName, Commit target)
        {
            return repository.Branches.Add(branchName, target);
        }

        /// <summary>
        ///   Creates a branch with the specified name. This branch will point at the commit pointed at by the <see cref = "Repository.Head" />.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "branchName">The name of the branch to create.</param>
        /// <param name = "committish">The revparse spec for the target commit.</param>
        public static Branch CreateBranch(this IRepository repository, string branchName, string committish)
        {
            return repository.Branches.Add(branchName, committish);
        }

        /// <summary>
        ///   Sets the current <see cref="Repository.Head"/> /> to the specified commit and optionally resets the <see cref = "Index" /> and
        ///   the content of the working tree to match.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "resetOptions">Flavor of reset operation to perform.</param>
        /// <param name = "committish">A revparse spec for the target commit object.</param>
        public static void Reset(this IRepository repository, ResetOptions resetOptions, string committish = "HEAD")
        {
            Ensure.ArgumentNotNullOrEmptyString(committish, "committish");

            Commit commit = LookUpCommit(repository, committish);

            repository.Reset(resetOptions, commit);
        }

        /// <summary>
        ///   Replaces entries in the <see cref="Index"/> with entries from the specified commit.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "committish">A revparse spec for the target commit object.</param>
        /// <param name = "paths">The list of paths (either files or directories) that should be considered.</param>
        public static void Reset(this IRepository repository, string committish = "HEAD", IEnumerable<string> paths = null)
        {
            if (repository.Info.IsBare)
            {
                throw new BareRepositoryException("Reset is not allowed in a bare repository");
            }

            Ensure.ArgumentNotNullOrEmptyString(committish, "committish");

            Commit commit = LookUpCommit(repository, committish);

            repository.Reset(commit, paths);
        }

        private static Commit LookUpCommit(IRepository repository, string committish)
        {
            GitObject obj = repository.Lookup(committish);
            Ensure.GitObjectIsNotNull(obj, committish);
            return obj.DereferenceToCommit(true);
        }

        /// <summary>
        ///   Stores the content of the <see cref = "Repository.Index" /> as a new <see cref = "LibGit2Sharp.Commit" /> into the repository.
        ///   The tip of the <see cref = "Repository.Head"/> will be used as the parent of this new Commit.
        ///   Once the commit is created, the <see cref = "Repository.Head"/> will move forward to point at it.
        ///   <para>Both the Author and Committer will be guessed from the Git configuration. An exception will be raised if no configuration is reachable.</para>
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "message">The description of why a change was made to the repository.</param>
        /// <param name = "amendPreviousCommit">True to amend the current <see cref = "LibGit2Sharp.Commit"/> pointed at by <see cref = "Repository.Head"/>, false otherwise.</param>
        /// <returns>The generated <see cref = "LibGit2Sharp.Commit" />.</returns>
        public static Commit Commit(this IRepository repository, string message, bool amendPreviousCommit = false)
        {
            Signature author = BuildSignatureFromGlobalConfiguration(repository, DateTimeOffset.Now);

            return repository.Commit(message, author, amendPreviousCommit);
        }

        /// <summary>
        ///   Stores the content of the <see cref = "Repository.Index" /> as a new <see cref = "LibGit2Sharp.Commit" /> into the repository.
        ///   The tip of the <see cref = "Repository.Head"/> will be used as the parent of this new Commit.
        ///   Once the commit is created, the <see cref = "Repository.Head"/> will move forward to point at it.
        ///   <para>The Committer will be guessed from the Git configuration. An exception will be raised if no configuration is reachable.</para>
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "author">The <see cref = "Signature" /> of who made the change.</param>
        /// <param name = "message">The description of why a change was made to the repository.</param>
        /// <param name = "amendPreviousCommit">True to amend the current <see cref = "LibGit2Sharp.Commit"/> pointed at by <see cref = "Repository.Head"/>, false otherwise.</param>
        /// <returns>The generated <see cref = "LibGit2Sharp.Commit" />.</returns>
        public static Commit Commit(this IRepository repository, string message, Signature author, bool amendPreviousCommit = false)
        {
            Signature committer = BuildSignatureFromGlobalConfiguration(repository, DateTimeOffset.Now);

            return repository.Commit(message, author, committer, amendPreviousCommit);
        }

        /// <summary>
        ///   Fetch from the specified remote.
        /// </summary>
        /// <param name="repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name="remoteName">The name of the <see cref="Remote"/> to fetch from.</param>
        /// <param name="tagFetchMode">Optional parameter indicating what tags to download.</param>
        /// <param name="onProgress">Progress callback. Corresponds to libgit2 progress callback.</param>
        /// <param name="onCompletion">Completion callback. Corresponds to libgit2 completion callback.</param>
        /// <param name="onUpdateTips">UpdateTips callback. Corresponds to libgit2 update_tips callback.</param>
        /// <param name="onTransferProgress">Callback method that transfer progress will be reported through.
        ///   Reports the client's state regarding the received and processed (bytes, objects) from the server.</param>
        public static void Fetch(this IRepository repository, string remoteName,
            TagFetchMode tagFetchMode = TagFetchMode.Auto,
            ProgressHandler onProgress = null,
            CompletionHandler onCompletion = null,
            UpdateTipsHandler onUpdateTips = null,
            TransferProgressHandler onTransferProgress = null)
        {
            Ensure.ArgumentNotNull(repository, "repository");
            Ensure.ArgumentNotNullOrEmptyString(remoteName, "remoteName");

            Remote remote = repository.Remotes.RemoteForName(remoteName, true);
            remote.Fetch(tagFetchMode, onProgress, onCompletion, onUpdateTips, onTransferProgress);
        }

        private static Signature BuildSignatureFromGlobalConfiguration(IRepository repository, DateTimeOffset now)
        {
            var name = repository.Config.Get<string>("user.name");
            var email = repository.Config.Get<string>("user.email");

            if ((name == null) || (email == null))
            {
                throw new LibGit2SharpException("Can not find Name and Email settings of the current user in Git configuration.");
            }

            return new Signature(name.Value, email.Value, now);
        }

        /// <summary>
        ///   Checkout the specified <see cref = "Branch" />, reference or SHA.
        /// </summary>
        /// <param name="repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "commitOrBranchSpec">A revparse spec for the commit or branch to checkout.</param>
        /// <returns>The <see cref = "Branch" /> that was checked out.</returns>
        public static Branch Checkout(this IRepository repository, string commitOrBranchSpec)
        {
            return repository.Checkout(commitOrBranchSpec, CheckoutOptions.None, null);
        }


        /// <summary>
        ///   Checkout the specified <see cref = "Branch" />.
        /// </summary>
        /// <param name="repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name="branch">The <see cref = "Branch" /> to check out.</param>
        /// <returns>The <see cref = "Branch" /> that was checked out.</returns>
        public static Branch Checkout(this IRepository repository, Branch branch)
        {
            return repository.Checkout(branch, CheckoutOptions.None, null);
        }
    }
}
