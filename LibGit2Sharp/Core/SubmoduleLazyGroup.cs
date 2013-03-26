using System;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal class SubmoduleLazyGroup : LazyGroup<SubmoduleSafeHandle>
    {
        private readonly string name;

        public SubmoduleLazyGroup(Repository repo, string name)
            : base(repo)
        {
            this.name = name;
        }

        protected override void EvaluateInternal(Action<SubmoduleSafeHandle> evaluator)
        {
            using (var handle = Proxy.git_submodule_lookup(repo.Handle, name))
            {
                evaluator(handle);
            }
        }
    }
}
