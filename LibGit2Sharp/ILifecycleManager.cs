using System;

namespace LibGit2Sharp
{
    public interface ILifecycleManager : IDisposable
    {
        IntPtr RepositoryPtr { get; }
        RepositoryDetails Details { get; }
    }
}