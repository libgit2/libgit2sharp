using System;

namespace LibGit2Sharp
{
    public interface IBuilder
    {
        GitObject BuildFrom(IntPtr gitObjectPtr, ObjectType type);
    }
}