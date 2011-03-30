using System;

namespace LibGit2Sharp
{
    public class Tag : GitObject
    {
        internal Tag(IntPtr obj, ObjectId id = null)
            : base(obj, id)
        {
        }
    }
}