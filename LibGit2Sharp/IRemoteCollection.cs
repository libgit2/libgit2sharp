using System.Collections.Generic;

namespace LibGit2Sharp
{
    public interface IRemoteCollection : IEnumerable<Remote>
    {
        Remote this[string name] { get; }
        Remote Add(string name, string url);
        Remote Create(string name, string url, string fetchRefSpec);
    }
}