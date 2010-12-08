using System;

namespace libgit2sharp
{
    public interface IResolver
    {
        GitObject Resolve(string objectId);
        TType Resolve<TType>(string objectId);
        object Resolve(string objectId, Type expectedType);
    }
}