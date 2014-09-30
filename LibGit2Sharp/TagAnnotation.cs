using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A TagAnnotation
    /// </summary>
    public class TagAnnotation : GitObject
    {
        private readonly GitObjectLazyGroup group;
        private readonly ILazy<GitObject> lazyTarget;
        private readonly ILazy<string> lazyName;
        private readonly ILazy<string> lazyMessage;
        private readonly ILazy<Signature> lazyTagger;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected TagAnnotation()
        { }

        internal TagAnnotation(Repository repo, ObjectId id)
            : base(repo, id)
        {
            lazyName = GitObjectLazyGroup.Singleton(repo, id, Proxy.git_tag_name);
            lazyTarget = GitObjectLazyGroup.Singleton(repo, id,
                obj => BuildFrom(repo, Proxy.git_tag_target_id(obj), Proxy.git_tag_target_type(obj), null));

            group = new GitObjectLazyGroup(repo, id);
            lazyTagger = group.AddLazy(Proxy.git_tag_tagger);
            lazyMessage = group.AddLazy(Proxy.git_tag_message);
        }

        /// <summary>
        /// Dereference tag to a commit.
        /// </summary>
        /// <param name="throwsIfCanNotBeDereferencedToACommit"></param>
        /// <returns></returns>
        internal override Commit DereferenceToCommit(bool throwsIfCanNotBeDereferencedToACommit)
        {
            using (GitObjectSafeHandle peeledHandle = Proxy.git_object_peel(repo.Handle, Id, GitObjectType.Commit, throwsIfCanNotBeDereferencedToACommit))
            {
                if (peeledHandle == null)
                {
                    return null;
                }

                return (Commit)BuildFrom(repo, Proxy.git_object_id(peeledHandle), GitObjectType.Commit, null);
            }
        }

        /// <summary>
        /// Gets the name of this tag.
        /// </summary>
        public virtual string Name { get { return lazyName.Value; } }

        /// <summary>
        /// Gets the message of this tag.
        /// </summary>
        public virtual string Message { get { return lazyMessage.Value; } }

        /// <summary>
        /// Gets the <see cref="GitObject"/> that this tag annotation points to.
        /// </summary>
        public virtual GitObject Target { get { return lazyTarget.Value; } }

        /// <summary>
        /// Gets the tagger.
        /// </summary>
        public virtual Signature Tagger { get { return lazyTagger.Value; } }
    }
}
