using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Tag
    /// </summary>
    public class Tag : ReferenceWrapper<GitObject>, IEquatable<Tag>
    {
        private static readonly LambdaEqualityHelper<Tag> equalityHelper =
            new LambdaEqualityHelper<Tag>(new Func<Tag, object>[] { x => x.CanonicalName, x => x.Target });

        internal Tag(Repository repo, Reference reference, string canonicalName)
            : base(repo, reference, _ => canonicalName)
        {
        }

        /// <summary>
        ///   Gets the optional information associated to this tag.
        ///   <para>When the <see cref = "Tag" /> is a lightweight tag, <c>null</c> is returned.</para>
        /// </summary>
        public TagAnnotation Annotation
        {
            get { return TargetObject as TagAnnotation; }
        }

        /// <summary>
        ///   Gets the <see cref = "GitObject" /> that this tag points to.
        /// </summary>
        public GitObject Target
        {
            get
            {
                GitObject target = TargetObject;

                if ((!(target is TagAnnotation)))
                {
                    return target;
                }

                return ((TagAnnotation)target).Target;
            }
        }

        /// <summary>
        ///   Indicates whether the tag holds any metadata.
        /// </summary>
        public bool IsAnnotated
        {
            get { return Annotation != null; }
        }

        /// <summary>
        ///   Returns the friendly shortened name from a canonical name.
        /// </summary>
        /// <param name="canonicalName">The canonical name to shorten.</param>
        /// <returns></returns>
        protected override string Shorten(string canonicalName)
        {
            Ensure.ArgumentConformsTo(canonicalName, s => s.StartsWith("refs/tags/", StringComparison.Ordinal), "tagName");

            return canonicalName.Substring("refs/tags/".Length);
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "Object" /> is equal to the current <see cref = "Tag" />.
        /// </summary>
        /// <param name = "obj">The <see cref = "Object" /> to compare with the current <see cref = "Tag" />.</param>
        /// <returns>True if the specified <see cref = "Object" /> is equal to the current <see cref = "Tag" />; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Tag);
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "Tag" /> is equal to the current <see cref = "Tag" />.
        /// </summary>
        /// <param name = "other">The <see cref = "Tag" /> to compare with the current <see cref = "Tag" />.</param>
        /// <returns>True if the specified <see cref = "Tag" /> is equal to the current <see cref = "Tag" />; otherwise, false.</returns>
        public bool Equals(Tag other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        ///   Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        ///   Tests if two <see cref = "Tag" /> are equal.
        /// </summary>
        /// <param name = "left">First <see cref = "Tag" /> to compare.</param>
        /// <param name = "right">Second <see cref = "Tag" /> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(Tag left, Tag right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///   Tests if two <see cref = "Tag" /> are different.
        /// </summary>
        /// <param name = "left">First <see cref = "Tag" /> to compare.</param>
        /// <param name = "right">Second <see cref = "Tag" /> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(Tag left, Tag right)
        {
            return !Equals(left, right);
        }
    }
}
