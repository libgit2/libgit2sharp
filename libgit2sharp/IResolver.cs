using System;

namespace libgit2sharp
{
    public interface IResolver
    {
        object Resolve(string objectId, Type expectedType);
    }
}