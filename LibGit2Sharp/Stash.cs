namespace LibGit2Sharp
{
    ///<summary>
    ///   A Stash
    ///   <para>A stash is a snapshot of the dirty state of the working directory (i.e. the modified tracked files and staged changes)</para>
    ///</summary>
    public class Stash : ReferenceWrapper<Commit>
    {
        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected Stash()
        { }

        internal Stash(Repository repo, ObjectId targetId, int index)
            : base(repo, new DirectReference(string.Format("stash@{{{0}}}", index), repo, targetId), r => r.CanonicalName)
        { }

        /// <summary>
        ///   Gets the <see cref = "Commit" /> that this stash points to.
        /// </summary>
        public virtual Commit Target
        {
            get { return TargetObject; }
        }

        /// <summary>
        ///   Gets the message associated to this <see cref="Stash"/>.
        /// </summary>
        public virtual string Message
        {
            get { return Target.Message; }
        }

        /// <summary>
        ///   Returns "stash@{i}", where i is the index of this <see cref="Stash"/>.
        /// </summary>
        protected override string Shorten()
        {
            return CanonicalName;
        }
    }
}
