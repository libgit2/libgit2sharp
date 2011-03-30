using System;

namespace LibGit2Sharp
{
    public class Blob : GitObject
    {
        internal Blob(IntPtr obj, ObjectId id = null)
            : base(obj, id)
        {
        }
    }
}