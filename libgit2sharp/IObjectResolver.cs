using System;

namespace libgit2sharp
{
    public interface IObjectResolver
    {
        object Resolve(string objectId, Type expectedType);
    }
}