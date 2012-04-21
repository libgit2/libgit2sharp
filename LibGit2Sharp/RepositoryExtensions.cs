using System;
using LibGit2Sharp.Core;

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
        /// <param name = "shaOrRef">The shaOrRef to lookup.</param>
        /// <returns></returns>
        public static T Lookup<T>(this Repository repository, string shaOrRef) where T : GitObject
        {
            return (T)repository.Lookup(shaOrRef, GitObject.TypeToTypeMap[typeof(T)]);
        }

        /// <summary>
        ///   Try to lookup an object by its <see cref = "ObjectId" />.
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "repository">The <see cref = "Repository" /> being looked up.</param>
        /// <param name = "id">The id.</param>
        /// <returns></returns>
        public static T Lookup<T>(this Repository repository, ObjectId id) where T : GitObject
        {
            return (T)repository.Lookup(id, GitObject.TypeToTypeMap[typeof(T)]);
        }

        /// <summary>
        ///   Creates a lightweight tag with the specified name. This tag will point at the commit pointed at by the <see cref = "Repository.Head" />.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "tagName">The name of the tag to create.</param>
        public static Tag ApplyTag(this Repository repository, string tagName)
        {
            return ApplyTag(repository, tagName, repository.Head.CanonicalName);
        }

        /// <summary>
        ///   Creates a lightweight tag with the specified name. This tag will point at the <paramref name = "target" />.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "tagName">The name of the tag to create.</param>
        /// <param name = "target">The canonical reference name or sha which should be pointed at by the Tag.</param>
        public static Tag ApplyTag(this Repository repository, string tagName, string target)
        {
            return repository.Tags.Create(tagName, target);
        }

        /// <summary>
        ///   Creates an annotated tag with the specified name. This tag will point at the commit pointed at by the <see cref = "Repository.Head" />.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "tagName">The name of the tag to create.</param>
        /// <param name = "tagger">The identity of the creator of this tag.</param>
        /// <param name = "message">The annotation message.</param>
        public static Tag ApplyTag(this Repository repository, string tagName, Signature tagger, string message)
        {
            return ApplyTag(repository, tagName, repository.Head.CanonicalName, tagger, message);
        }

        /// <summary>
        ///   Creates an annotated tag with the specified name. This tag will point at the <paramref name = "target" />.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "tagName">The name of the tag to create.</param>
        /// <param name = "target">The canonical reference name or sha which should be pointed at by the Tag.</param>
        /// <param name = "tagger">The identity of the creator of this tag.</param>
        /// <param name = "message">The annotation message.</param>
        public static Tag ApplyTag(this Repository repository, string tagName, string target, Signature tagger, string message)
        {
            return repository.Tags.Create(tagName, target, tagger, message);
        }

        /// <summary>
        ///   Creates a branch with the specified name. This branch will point at the commit pointed at by the <see cref = "Repository.Head" />.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "branchName">The name of the branch to create.</param>
        public static Branch CreateBranch(this Repository repository, string branchName)
        {
            return CreateBranch(repository, branchName, repository.Head.CanonicalName);
        }

        /// <summary>
        ///   Creates a branch with the specified name. This branch will point at <paramref name="target"/>.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "branchName">The name of the branch to create.</param>
        /// <param name = "target">The commit which should be pointed at by the Branch.</param>
        public static Branch CreateBranch(this Repository repository, string branchName, Commit target)
        {
            Ensure.ArgumentNotNull(target, "target");
            return CreateBranch(repository, branchName, target.Id.Sha);
        }

        /// <summary>
        ///   Creates a branch with the specified name. This branch will point at the commit pointed at by the <see cref = "Repository.Head" />.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "branchName">The name of the branch to create.</param>
        /// <param name = "target">The canonical reference name or sha which should be pointed at by the Branch.</param>
        public static Branch CreateBranch(this Repository repository, string branchName, string target)
        {
            return repository.Branches.Create(branchName, target);
        }

        /// <summary>
        ///   Stores the content of the <see cref = "Repository.Index" /> as a new <see cref = "LibGit2Sharp.Commit" /> into the repository.
        ///   <para>Both the Author and Committer will be guessed from the Git configuration. An exception will be raised if no configuration is reachable.</para>
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "message">The description of why a change was made to the repository.</param>
        /// <param name="amendPreviousCommit">True to amend the current <see cref="LibGit2Sharp.Commit"/> pointed at by <see cref="Repository.Head"/>, false otherwise.</param>
        /// <returns>The generated <see cref = "LibGit2Sharp.Commit" />.</returns>
        public static Commit Commit(this Repository repository, string message, bool amendPreviousCommit = false)
        {
            Signature author = BuildSignatureFromGlobalConfiguration(repository, DateTimeOffset.Now);

            return repository.Commit(message, author, amendPreviousCommit);
        }

        /// <summary>
        ///   Stores the content of the <see cref = "Repository.Index" /> as a new <see cref = "LibGit2Sharp.Commit" /> into the repository.
        ///   <para>The Committer will be guessed from the Git configuration. An exception will be raised if no configuration is reachable.</para>
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "author">The <see cref = "Signature" /> of who made the change.</param>
        /// <param name = "message">The description of why a change was made to the repository.</param>
        /// <param name="amendPreviousCommit">True to amend the current <see cref="LibGit2Sharp.Commit"/> pointed at by <see cref="Repository.Head"/>, false otherwise.</param>
        /// <returns>The generated <see cref = "LibGit2Sharp.Commit" />.</returns>
        public static Commit Commit(this Repository repository, string message, Signature author, bool amendPreviousCommit = false)
        {
            Signature committer = BuildSignatureFromGlobalConfiguration(repository, DateTimeOffset.Now);

            return repository.Commit(message, author, committer, amendPreviousCommit);
        }

        /// <summary>
        ///   Stores the content of the <see cref = "Repository.Index" /> as a new <see cref = "LibGit2Sharp.Commit" /> into the repository.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "author">The <see cref = "Signature" /> of who made the change.</param>
        /// <param name = "committer">The <see cref = "Signature" /> of who added the change to the repository.</param>
        /// <param name = "message">The description of why a change was made to the repository.</param>
        /// <param name="amendPreviousCommit">True to amend the current <see cref="LibGit2Sharp.Commit"/> pointed at by <see cref="Repository.Head"/>, false otherwise.</param>
        /// <returns>The generated <see cref = "LibGit2Sharp.Commit" />.</returns>
        public static Commit Commit(this Repository repository, string message, Signature author, Signature committer, bool amendPreviousCommit = false)
        {
            return repository.Commits.Create(message, author, committer, amendPreviousCommit);
        }

        private static Signature BuildSignatureFromGlobalConfiguration(Repository repository, DateTimeOffset now)
        {
            var name = repository.Config.Get<string>("user.name", null);
            var email = repository.Config.Get<string>("user.email", null);

            if ((name == null) || (email == null))
            {
                throw new LibGit2Exception("Can not find Name and Email settings of the current user in Git configuration.");
            }

            return new Signature(name, email, now);
        }
    }
}
