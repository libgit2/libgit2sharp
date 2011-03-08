using System;

namespace LibGit2Sharp
{
    public interface ILifecycleManager : IDisposable
    {
        Core.Repository CoreRepository { get; }
        RepositoryDetails Details { get; }
    }
}