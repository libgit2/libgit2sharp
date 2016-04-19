using System;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class SubmoduleLazyGroup : LazyGroup<SubmoduleHandle>
    {
        private readonly string name;

        public SubmoduleLazyGroup(Repository repo, string name)
            : base(repo)
        {
            this.name = name;
        }

        protected override void EvaluateInternal(Action<SubmoduleHandle> evaluator)
        {
            repo.Submodules.Lookup(name,
                                   handle =>
                                   {
                                       evaluator(handle);
                                       return default(object);
                                   },
                                   true);
        }
    }
}
