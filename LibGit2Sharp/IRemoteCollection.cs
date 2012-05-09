using System.Collections.Generic;

namespace LibGit2Sharp
{
    public interface IRemoteCollection : IEnumerable<Remote>
    {
        Remote this[string name] { get; }
        Remote Create(string name, string url);
    }
}