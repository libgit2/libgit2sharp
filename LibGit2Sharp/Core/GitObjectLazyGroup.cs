using System;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class GitObjectLazyGroup : LazyGroup<GitObjectSafeHandle>
    {
        private readonly Repository repo;
        private readonly ObjectId id;

        public GitObjectLazyGroup(Repository repo, ObjectId id)
        {
            this.repo = repo;
            this.id = id;
        }

        protected override void EvaluateInternal(Action<GitObjectSafeHandle> evaluator)
        {
            using (var osw = new ObjectSafeWrapper(id, repo.Handle))
            {
                evaluator(osw.ObjectPtr);
            }
        }
    }
}
