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

        public override bool Equals(object obj)
        {
            return Equals(obj as Tag);
        }
        
        public bool Equals(Tag other)
        {
           return equalityHelper.Equals(this, other);
        }

        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        public static bool operator ==(Tag left, Tag right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Tag left, Tag right)
        {
            return !Equals(left, right);
        }
    }
}