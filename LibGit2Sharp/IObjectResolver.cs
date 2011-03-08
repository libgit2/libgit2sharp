using System;

namespace LibGit2Sharp
{
    public interface IObjectResolver
    {
        object Resolve(string objectId, Type expectedType);
    }
}
