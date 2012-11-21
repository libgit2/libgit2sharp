using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The collection of <see cref = "Tag" />s in a <see cref = "Repository" />
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class TagCollection : IEnumerable<Tag>
    {
        internal readonly Repository repo;
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
            return Proxy
                .git_tag_list(repo.Handle)
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
        /// <param name = "target">The target <see cref="GitObject"/>.</param>
        /// <param name = "tagger">The tagger.</param>
        /// <param name = "message">The message.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing tag, false otherwise.</param>
        /// <returns></returns>
        public virtual Tag Add(string name, GitObject target, Signature tagger, string message, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(target, "target");
            Ensure.ArgumentNotNull(tagger, "tagger");
            Ensure.ArgumentNotNull(message, "message");

            string prettifiedMessage = Proxy.git_message_prettify(message);

            Proxy.git_tag_create(repo.Handle, name, target, tagger, prettifiedMessage, allowOverwrite);

            return this[name];
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
            return this.Add(name, target, tagger, message, allowOverwrite);
        }

        /// <summary>
        ///   Creates a lightweight tag with the specified name.
        /// </summary>
        /// <param name = "name">The name.</param>
        /// <param name = "target">The target <see cref="GitObject"/>.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing tag, false otherwise.</param>
        /// <returns></returns>
        public virtual Tag Add(string name, GitObject target, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(target, "target");

            Proxy.git_tag_create_lightweight(repo.Handle, name, target, allowOverwrite);

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
            return this.Add(name, target, allowOverwrite);
        }

        /// <summary>
        ///   Deletes the tag with the specified name.
        /// </summary>
        /// <param name = "tag">The tag to delete.</param>
        public virtual void Remove(Tag tag)
        {
            Ensure.ArgumentNotNull(tag, "tag");

            this.Remove(tag.CanonicalName);
        }

        /// <summary>
        ///   Deletes the tag with the specified name.
        /// </summary>
        /// <param name = "name">The short or canonical name of the tag to delete.</param>
        [Obsolete("This method will be removed in the next release. Please use Remove() instead.")]
        public virtual void Delete(string name)
        {
            this.Remove(name);
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

        internal string UnCanonicalizeName(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            if (!name.StartsWith(refsTagsPrefix, StringComparison.Ordinal))
            {
                return name;
            }

            return name.Substring(refsTagsPrefix.Length);
        }

        private string DebuggerDisplay
        {
            get { return string.Format("Count = {0}", this.Count()); }
        }
    }
}
