using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Tag
    /// </summary>
    public class Tag : ReferenceWrapper<GitObject>
    {
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
        public IGitObject Target
        {
            get
            {
                IGitObject target = TargetObject;

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
    }
}
