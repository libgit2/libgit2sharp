using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// The collection of <see cref="Tag"/>s in a <see cref="Repository"/>
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class TagCollection : IEnumerable<Tag>
    {
        internal readonly Repository repo;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected TagCollection()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TagCollection"/> class.
        /// </summary>
        /// <param name="repo">The repo.</param>
        internal TagCollection(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        /// Gets the <see cref="Tag"/> with the specified name.
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
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<Tag> GetEnumerator()
        {
            return Proxy
                .git_tag_list(repo.Handle)
                .Select(n => this[n])
                .GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Creates an annotated tag with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="objectish">Revparse spec for the target object.</param>
        /// <param name="tagger">The tagger.</param>
        /// <param name="message">The message.</param>
        public virtual Tag Add(string name, string objectish, Signature tagger, string message)
        {
            return Add(name, objectish, tagger, message, false);
        }

        /// <summary>
        /// Creates an annotated tag with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="objectish">Revparse spec for the target object.</param>
        /// <param name="tagger">The tagger.</param>
        /// <param name="message">The message.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing tag, false otherwise.</param>
        public virtual Tag Add(string name, string objectish, Signature tagger, string message, bool allowOverwrite)
        {
            Ensure.ArgumentNotNullOrEmptyString(objectish, "target");

            GitObject objectToTag = repo.Lookup(objectish, GitObjectType.Any, LookUpOptions.ThrowWhenNoGitObjectHasBeenFound);

            return Add(name, objectToTag, tagger, message, allowOverwrite);
        }

        /// <summary>
        /// Creates a lightweight tag with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="objectish">Revparse spec for the target object.</param>
        public virtual Tag Add(string name, string objectish)
        {
            return Add(name, objectish, false);
        }

        /// <summary>
        /// Creates a lightweight tag with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="objectish">Revparse spec for the target object.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing tag, false otherwise.</param>
        public virtual Tag Add(string name, string objectish, bool allowOverwrite)
        {
            Ensure.ArgumentNotNullOrEmptyString(objectish, "objectish");

            GitObject objectToTag = repo.Lookup(objectish, GitObjectType.Any, LookUpOptions.ThrowWhenNoGitObjectHasBeenFound);

            return Add(name, objectToTag, allowOverwrite);
        }

        /// <summary>
        /// Creates an annotated tag with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="target">The target <see cref="GitObject"/>.</param>
        /// <param name="tagger">The tagger.</param>
        /// <param name="message">The message.</param>
        /// <returns>The added <see cref="Tag"/>.</returns>
        public virtual Tag Add(string name, GitObject target, Signature tagger, string message)
        {
            return Add(name, target, tagger, message, false);
        }

        /// <summary>
        /// Creates an annotated tag with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="target">The target <see cref="GitObject"/>.</param>
        /// <param name="tagger">The tagger.</param>
        /// <param name="message">The message.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing tag, false otherwise.</param>
        /// <returns>The added <see cref="Tag"/>.</returns>
        public virtual Tag Add(string name, GitObject target, Signature tagger, string message, bool allowOverwrite)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(target, "target");
            Ensure.ArgumentNotNull(tagger, "tagger");
            Ensure.ArgumentNotNull(message, "message");

            string prettifiedMessage = Proxy.git_message_prettify(message, null);

            Proxy.git_tag_create(repo.Handle, name, target, tagger, prettifiedMessage, allowOverwrite);

            return this[name];
        }

        /// <summary>
        /// Creates a lightweight tag with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="target">The target <see cref="GitObject"/>.</param>
        /// <returns>The added <see cref="Tag"/>.</returns>
        public virtual Tag Add(string name, GitObject target)
        {
            return Add(name, target, false);
        }

        /// <summary>
        /// Creates a lightweight tag with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="target">The target <see cref="GitObject"/>.</param>
        /// <param name="allowOverwrite">True to allow silent overwriting a potentially existing tag, false otherwise.</param>
        /// <returns>The added <see cref="Tag"/>.</returns>
        public virtual Tag Add(string name, GitObject target, bool allowOverwrite)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(target, "target");

            Proxy.git_tag_create_lightweight(repo.Handle, name, target, allowOverwrite);

            return this[name];
        }

        /// <summary>
        /// Deletes the tag with the specified name.
        /// </summary>
        /// <param name="name">The short or canonical name of the tag to delete.</param>
        public virtual void Remove(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            Proxy.git_tag_delete(repo.Handle, UnCanonicalizeName(name));
        }

        /// <summary>
        /// Deletes the tag with the specified name.
        /// </summary>
        /// <param name="tag">The tag to delete.</param>
        public virtual void Remove(Tag tag)
        {
            Ensure.ArgumentNotNull(tag, "tag");

            Remove(tag.CanonicalName);
        }

        private static string NormalizeToCanonicalName(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            if (name.LooksLikeTag())
            {
                return name;
            }

            return string.Concat(Reference.TagPrefix, name);
        }

        private static string UnCanonicalizeName(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            if (!name.LooksLikeTag())
            {
                return name;
            }

            return name.Substring(Reference.TagPrefix.Length);
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "Count = {0}", this.Count());
            }
        }
    }
}
