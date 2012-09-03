using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides helper overloads to a <see cref = "TagCollection" />.
    /// </summary>
    public static class TagCollectionExtensions
    {
        /// <summary>
        ///   Creates an annotated tag with the specified name.
        /// </summary>
        /// <param name = "name">The name.</param>
        /// <param name = "objectish">Revparse spec for the target object.</param>
        /// <param name = "tagger">The tagger.</param>
        /// <param name = "message">The message.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing tag, false otherwise.</param>
        /// <param name = "tags">The <see cref="TagCollection"/> being worked with.</param>
        public static Tag Add(this TagCollection tags, string name, string objectish, Signature tagger, string message, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(objectish, "target");

            GitObject objectToTag = tags.repo.Lookup(objectish, GitObjectType.Any, LookUpOptions.ThrowWhenNoGitObjectHasBeenFound);

            return tags.Add(name, objectToTag, tagger, message, allowOverwrite);
        }

        /// <summary>
        ///   Creates a lightweight tag with the specified name.
        /// </summary>
        /// <param name = "name">The name.</param>
        /// <param name = "objectish">Revparse spec for the target object.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing tag, false otherwise.</param>
        /// <param name = "tags">The <see cref="TagCollection"/> being worked with.</param>
        public static Tag Add(this TagCollection tags, string name, string objectish, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(objectish, "objectish");

            GitObject objectToTag = tags.repo.Lookup(objectish, GitObjectType.Any, LookUpOptions.ThrowWhenNoGitObjectHasBeenFound);

            return tags.Add(name, objectToTag, allowOverwrite);
        }

        /// <summary>
        ///   Deletes the tag with the specified name.
        /// </summary>
        /// <param name = "name">The short or canonical name of the tag to delete.</param>
        /// <param name = "tags">The <see cref="TagCollection"/> being worked with.</param>
        public static void Remove(this TagCollection tags, string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            Proxy.git_tag_delete(tags.repo.Handle, tags.UnCanonicalizeName(name));
        }
    }
}
