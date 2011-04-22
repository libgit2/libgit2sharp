using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Tag
    /// </summary>
    public class Tag : IEquatable<Tag>
    {
        private static readonly LambdaEqualityHelper<Tag> equalityHelper =
           new LambdaEqualityHelper<Tag>(new Func<Tag, object>[] { x => x.CanonicalName, x => x.Target });

        internal Tag(string canonicalName, GitObject target, TagAnnotation tagAnnotation)
        {
            Ensure.ArgumentNotNullOrEmptyString(canonicalName, "canonicalName");
            Ensure.ArgumentNotNull(target, "target");

            CanonicalName = canonicalName;
            Target = target;
            Annotation = tagAnnotation;
        }

        public TagAnnotation Annotation { get; private set; }

        public string CanonicalName { get; private set; }

        /// <summary>
        ///   Gets the name of this tag.
        /// </summary>
        public string Name { get { return Shorten(CanonicalName); } }

        public GitObject Target { get; private set; }

        /// <summary>
        ///   Indicates whether the tag holds any metadata.
        /// </summary>
        public bool IsAnnotated { get { return Annotation != null; } }

        private static string Shorten(string tagName)
        {
            Ensure.ArgumentConformsTo(tagName, s => s.StartsWith("refs/tags/", StringComparison.Ordinal), "tagName");

            return tagName.Substring("refs/tags/".Length);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="Tag"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="Tag"/>.</param>
        /// <returns>True if the specified <see cref="Object"/> is equal to the current <see cref="Tag"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Tag);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Tag"/> is equal to the current <see cref="Tag"/>.
        /// </summary>
        /// <param name="other">The <see cref="Tag"/> to compare with the current <see cref="Tag"/>.</param>
        /// <returns>True if the specified <see cref="Tag"/> is equal to the current <see cref="Tag"/>; otherwise, false.</returns>
        public bool Equals(Tag other)
        {
           return equalityHelper.Equals(this, other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        /// Tests if two <see cref="Tag"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="Tag"/> to compare.</param>
        /// <param name="right">Second <see cref="Tag"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(Tag left, Tag right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="Tag"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="Tag"/> to compare.</param>
        /// <param name="right">Second <see cref="Tag"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(Tag left, Tag right)
        {
            return !Equals(left, right);
        }
    }
}