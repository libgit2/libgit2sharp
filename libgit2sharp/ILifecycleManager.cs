using System;

namespace libgit2sharp
{
    public interface ILifecycleManager : IDisposable
    {
        IntPtr RepositoryPtr { get; }
        RepositoryDetails Details { get; }
    }
}