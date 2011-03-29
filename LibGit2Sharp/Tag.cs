using System;

namespace LibGit2Sharp
{
    public class Tag : GitObject
    {
        internal Tag(IntPtr obj, GitOid? oid = null)
            : base(obj, oid)
        {
        }
    }
}