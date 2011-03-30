using System;

namespace LibGit2Sharp
{
    public class Tree : GitObject
    {
        internal Tree(IntPtr obj, ObjectId id = null)
            : base(obj, id)
        {
        }
    }
}