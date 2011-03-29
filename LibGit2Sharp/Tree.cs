using System;

namespace LibGit2Sharp
{
    public class Tree : GitObject
    {
        internal Tree(IntPtr obj, GitOid? oid = null)
            : base(obj, oid)
        {
        }
    }
}