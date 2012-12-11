namespace LibGit2Sharp
{
    /// <summary>
    ///   A Tag
    /// </summary>
    public class Tag : ReferenceWrapper<GitObject>
    {
        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Tag()
        { }

        internal Tag(Repository repo, Reference reference, string canonicalName)
            : base(repo, reference, _ => canonicalName)
        {
        }

        /// <summary>
        ///   Gets the optional information associated to this tag.
        ///   <para>When the <see cref = "Tag" /> is a lightweight tag, <c>null</c> is returned.</para>
        /// </summary>
        public virtual TagAnnotation Annotation
        {
            get { return TargetObject as TagAnnotation; }
        }

        /// <summary>
        ///   Gets the <see cref = "GitObject" /> that this tag points to.
        /// </summary>
        public virtual GitObject Target
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
        public virtual bool IsAnnotated
        {
            get { return Annotation != null; }
        }

        /// <summary>
        ///   Removes redundent leading namespaces (regarding the kind of
        ///   reference being wrapped) from the canonical name.
        /// </summary>
        /// <returns>The friendly shortened name</returns>
        protected override string Shorten()
        {
            return CanonicalName.Substring("refs/tags/".Length);
        }
    }
}
