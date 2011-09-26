﻿namespace LibGit2Sharp
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
        ///   Stores the content of the <see cref = "Repository.Index" /> as a new <see cref = "Commit" /> into the repository.
        /// </summary>
        /// <param name = "repository">The <see cref = "Repository" /> being worked with.</param>
        /// <param name = "author">The <see cref = "Signature" /> of who made the change.</param>
        /// <param name = "committer">The <see cref = "Signature" /> of who added the change to the repository.</param>
        /// <param name = "message">The description of why a change was made to the repository.</param>
        /// <returns>The generated <see cref = "Commit" />.</returns>
        public static Commit Commit(this Repository repository, Signature author, Signature committer, string message)
        {
            return repository.Commits.Create(author, committer, message);
        }
    }
}
