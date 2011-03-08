using System;

namespace LibGit2Sharp
{
    public interface IBuilder
    {
        GitObject BuildFrom(Core.GitObject obj);
    }
}
