using System;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class GitObjectLazyGroup : LazyGroup<ObjectHandle>
    {
        private readonly ObjectId id;

        public GitObjectLazyGroup(Repository repo, ObjectId id)
            : base(repo)
        {
            this.id = id;
        }

        protected override void EvaluateInternal(Action<ObjectHandle> evaluator)
        {
            using (var osw = new ObjectSafeWrapper(id, repo.Handle))
            {
                evaluator(osw.ObjectPtr);
            }
        }

        public static ILazy<TResult> Singleton<TResult>(Repository repo, ObjectId id, Func<ObjectHandle, TResult> resultSelector)
        {
            return Singleton(() =>
            {
                using (var osw = new ObjectSafeWrapper(id, repo.Handle))
                {
                    return resultSelector(osw.ObjectPtr);
                }
            });
        }
    }
}
