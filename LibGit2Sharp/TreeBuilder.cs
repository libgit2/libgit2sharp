using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class TreeBuilder
    {
        private readonly TreeBuilderSafeHandle handle;

        public TreeBuilder()
        {
            int res = NativeMethods.git_treebuilder_create(out handle, IntPtr.Zero);
            Ensure.Success(res);
        }

        public void Insert(TreeEntry entry)
        {
            GitOid oid = entry.Target.Id.Oid;
            int res = NativeMethods.git_treebuilder_insert(IntPtr.Zero, handle, entry.Name, ref oid, entry.Attributes);
            Ensure.Success(res);
        }

        public Tree Write(Repository repo)
        {
            GitOid oid;
            int res = NativeMethods.git_treebuilder_write(out oid, repo.Handle, handle);
            Ensure.Success(res);

            return repo.Lookup(new ObjectId(oid), GitObjectType.Tree) as Tree;
        }
    }
}
