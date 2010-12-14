using System;

namespace libgit2sharp
{
    public interface IBuilder
    {
        GitObject BuildFrom(IntPtr gitObjectPtr, ObjectType type);
    }
}