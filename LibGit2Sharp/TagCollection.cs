﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;

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
        public Tag this[string name]
        {
            get { return repo.Refs.Resolve<Tag>(NormalizeToCanonicalName(name)); }
        }

        #region IEnumerable<Tag> Members

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public IEnumerator<Tag> GetEnumerator()
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
        public Tag Create(string name, string target, Signature tagger, string message, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(target, "target");
            Ensure.ArgumentNotNull(tagger, "tagger");
            Ensure.ArgumentNotNull(message, "message");

            GitObject objectToTag = RetrieveObjectToTag(target);

            int res;
            using (var objectPtr = new ObjectSafeWrapper(objectToTag.Id, repo))
            {
                GitOid oid;
                res = NativeMethods.git_tag_create(out oid, repo.Handle, name, objectPtr.ObjectPtr, tagger.Handle, message, allowOverwrite);
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
        public Tag Create(string name, string target, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNullOrEmptyString(target, "target");

            GitObject objectToTag = RetrieveObjectToTag(target);

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
        ///   Deletes the tag with the specified name.
        /// </summary>
        /// <param name = "name">The short or canonical name of the tag or the to delete.</param>
        public void Delete(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            int res = NativeMethods.git_tag_delete(repo.Handle, UnCanonicalizeName(name));
            Ensure.Success(res);
        }

        private GitObject RetrieveObjectToTag(string target)
        {
            GitObject objectToTag = repo.Lookup(target);

            if (objectToTag == null)
            {
                throw new LibGit2Exception(String.Format(CultureInfo.InvariantCulture, "No object identified by '{0}' can be found in the repository.", target));
            }

            return objectToTag;
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
