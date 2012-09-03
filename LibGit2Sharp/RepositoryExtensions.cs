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
        /// <param name = "commitish">The revparse spec for the target commit.</param>
        public static Branch CreateBranch(this IRepository repository, string branchName, string commitish)
        {
            return repository.Branches.Add(branchName, commitish);
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

        private static Signature BuildSignatureFromGlobalConfiguration(IRepository repository, DateTimeOffset now)
        {
            var name = repository.Config.Get<string>("user.name", null);
            var email = repository.Config.Get<string>("user.email", null);

            if ((name == null) || (email == null))
            {
                throw new LibGit2SharpException("Can not find Name and Email settings of the current user in Git configuration.");
            }

            return new Signature(name, email, now);
        }
    }
}
