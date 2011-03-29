using System;

namespace LibGit2Sharp
{
    public class Blob : GitObject
    {
        internal Blob(IntPtr obj, GitOid? oid = null)
            : base(obj, oid)
        {
        }
    }
}