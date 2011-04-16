using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Tag
    /// </summary>
    public class Tag : DirectReference
    {
        /// <summary>
        ///   Gets the name of this tag.
        /// </summary>
        public string Name { get { return Shorten(CanonicalName); } }

        public TagAnnotation Annotation { get; private set; }

        private static string Shorten(string referenceName)
        {
            Ensure.ArgumentConformsTo(referenceName, s => s.StartsWith("refs/tags/", StringComparison.Ordinal), "referenceName");
            return referenceName.Substring("refs/tags/".Length);
        }

        /// <summary>
        ///   Indicates whether the tag holds any metadata.
        /// </summary>
        public bool IsAnnotated { get { return Annotation != null; } }

        internal static Tag BuildFromReference(Reference reference)
        {
            GitObject target = reference.ResolveToDirectReference().Target;

            return new Tag { CanonicalName = reference.CanonicalName, Target = target, Annotation = target as TagAnnotation};
        }
    }
}