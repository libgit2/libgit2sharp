using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The collection of <see cref = "Tag" />s in a <see cref = "Repository" />
    /// </summary>
    public class TagCollection : IEnumerable<Tag>
    {
        private readonly Repository repo;
        private const string refsTagsPrefix = "refs/tags/";

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected TagCollection()
        { }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "TagCollection" /> class.
        /// </summary>
        /// <param name = "repo">The repo.</param>
        internal TagCollection(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Gets the <see cref = "Tag" /> with the specified name.
        /// </summary>
        public virtual Tag this[string name]
        {
            get
            {
                Ensure.ArgumentNotNullOrEmptyString(name, "name");
                var canonicalName = NormalizeToCanonicalName(name);
                var reference = repo.Refs.Resolve<Reference>(canonicalName);
                return reference == null ? null : new Tag(repo, reference, canonicalName);
            }
        }

        #region IEnumerable<Tag> Members

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<Tag> GetEnumerator()
        {
            return Libgit2UnsafeHelper
                .ListAllTagNames(repo.Handle)
                .Select(n => this[n])
                .GetEnumerator();
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
        ///   Creates an annotated tag with the specified name.
        /// </summary>
        /// <param name = "name">The name.</param>
        /// <param name = "target">The target which can be sha or a canonical reference name.</param>
        /// <param name = "tagger">The tagger.</param>
        /// <param name = "message">The message.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing tag, false otherwise.</param>
        /// <returns></returns>
        public virtual Tag Add(string name, string target, Signature tagger, string message, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(target, "target");
            Ensure.ArgumentNotNull(tagger, "tagger");
            Ensure.ArgumentNotNull(message, "message");

            GitObject objectToTag = repo.Lookup(target, GitObjectType.Any, LookUpOptions.ThrowWhenNoGitObjectHasBeenFound);

            string prettifiedMessage = ObjectDatabase.PrettifyMessage(message);

            int res;
            using (var objectPtr = new ObjectSafeWrapper(objectToTag.Id, repo))
            using (SignatureSafeHandle taggerHandle = tagger.BuildHandle())
            {
                GitOid oid;
                res = NativeMethods.git_tag_create(out oid, repo.Handle, name, objectPtr.ObjectPtr, taggerHandle, prettifiedMessage, allowOverwrite);
            }

            Ensure.Success(res);

            return this[name];
        }

        internal static string PrettifyMessage(string message)
        {
            var buffer = new byte[NativeMethods.GIT_PATH_MAX];
            int res = NativeMethods.git_message_prettify(buffer, buffer.Length, message, false);
            Ensure.Success(res);

            return Utf8Marshaler.Utf8FromBuffer(buffer) ?? string.Empty;
        }

        /// <summary>
        ///   Creates an annotated tag with the specified name.
        /// </summary>
        /// <param name = "name">The name.</param>
        /// <param name = "target">The target which can be sha or a canonical reference name.</param>
        /// <param name = "tagger">The tagger.</param>
        /// <param name = "message">The message.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing tag, false otherwise.</param>
        /// <returns></returns>
        [Obsolete("This method will be removed in the next release. Please use Add() instead.")]
        public virtual Tag Create(string name, string target, Signature tagger, string message, bool allowOverwrite = false)
        {
            return Add(name, target, tagger, message, allowOverwrite);
        }

        /// <summary>
        ///   Creates a lightweight tag with the specified name.
        /// </summary>
        /// <param name = "name">The name.</param>
        /// <param name = "target">The target which can be sha or a canonical reference name.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing tag, false otherwise.</param>
        /// <returns></returns>
        public virtual Tag Add(string name, string target, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(target, "target");

            GitObject objectToTag = repo.Lookup(target, GitObjectType.Any, LookUpOptions.ThrowWhenNoGitObjectHasBeenFound);

            int res;
            using (var objectPtr = new ObjectSafeWrapper(objectToTag.Id, repo))
            {
                GitOid oid;
                res = NativeMethods.git_tag_create_lightweight(out oid, repo.Handle, name, objectPtr.ObjectPtr, allowOverwrite);
            }

            Ensure.Success(res);

            return this[name];
        }

        /// <summary>
        ///   Creates a lightweight tag with the specified name.
        /// </summary>
        /// <param name = "name">The name.</param>
        /// <param name = "target">The target which can be sha or a canonical reference name.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing tag, false otherwise.</param>
        /// <returns></returns>
        [Obsolete("This method will be removed in the next release. Please use Add() instead.")]
        public virtual Tag Create(string name, string target, bool allowOverwrite = false)
        {
            return Add(name, target, allowOverwrite);
        }

        /// <summary>
        ///   Deletes the tag with the specified name.
        /// </summary>
        /// <param name = "name">The short or canonical name of the tag to delete.</param>
        public virtual void Remove(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            int res = NativeMethods.git_tag_delete(repo.Handle, UnCanonicalizeName(name));
            Ensure.Success(res);
        }

        /// <summary>
        ///   Deletes the tag with the specified name.
        /// </summary>
        /// <param name = "name">The short or canonical name of the tag to delete.</param>
        [Obsolete("This method will be removed in the next release. Please use Remove() instead.")]
        public virtual void Delete(string name)
        {
            Remove(name);
        }

        private static string NormalizeToCanonicalName(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            if (name.StartsWith(refsTagsPrefix, StringComparison.Ordinal))
            {
                return name;
            }

            return string.Concat(refsTagsPrefix, name);
        }

        private static string UnCanonicalizeName(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            if (!name.StartsWith(refsTagsPrefix, StringComparison.Ordinal))
            {
                return name;
            }

            return name.Substring(refsTagsPrefix.Length);
        }
    }
}
