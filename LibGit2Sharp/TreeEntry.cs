using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class TreeEntry : IEquatable<TreeEntry>
    {
        private readonly ObjectId parentTreeId;
        private readonly Repository repo;
        private GitObject target;
        private readonly ObjectId targetOid;

        private static readonly LambdaEqualityHelper<TreeEntry> equalityHelper =
            new LambdaEqualityHelper<TreeEntry>(new Func<TreeEntry, object>[] { x => x.Name, x => x.parentTreeId });

        public TreeEntry(IntPtr obj, ObjectId parentTreeId, Repository repo)
        {
            this.parentTreeId = parentTreeId;
            this.repo = repo;
            IntPtr gitTreeEntryId = NativeMethods.git_tree_entry_id(obj);
            targetOid = new ObjectId((GitOid)Marshal.PtrToStructure(gitTreeEntryId, typeof(GitOid)));

            Attributes = NativeMethods.git_tree_entry_attributes(obj);
            Name = NativeMethods.git_tree_entry_name(obj).MarshallAsString();
        }

        public int Attributes { get; private set; }
        
        public string Name { get; private set; }

        public GitObject Target { get { return target ?? (target = RetreiveTreeEntryTarget()); } }

        private GitObject RetreiveTreeEntryTarget()
        {
            GitObject treeEntryTarget = repo.Lookup(targetOid);

            Ensure.ArgumentConformsTo(treeEntryTarget.GetType(), t => typeof(Blob).IsAssignableFrom(t) || typeof(Tree).IsAssignableFrom(t), "treeEntryTarget");
            
            return treeEntryTarget;
        }

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="TreeEntry"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="TreeEntry"/>.</param>
        /// <returns>True if the specified <see cref="Object"/> is equal to the current <see cref="TreeEntry"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as TreeEntry);
        }

        /// <summary>
        /// Determines whether the specified <see cref="TreeEntry"/> is equal to the current <see cref="TreeEntry"/>.
        /// </summary>
        /// <param name="other">The <see cref="TreeEntry"/> to compare with the current <see cref="TreeEntry"/>.</param>
        /// <returns>True if the specified <see cref="TreeEntry"/> is equal to the current <see cref="TreeEntry"/>; otherwise, false.</returns>
        public bool Equals(TreeEntry other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        /// Tests if two <see cref="TreeEntry"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="TreeEntry"/> to compare.</param>
        /// <param name="right">Second <see cref="TreeEntry"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(TreeEntry left, TreeEntry right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="TreeEntry"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="TreeEntry"/> to compare.</param>
        /// <param name="right">Second <see cref="TreeEntry"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(TreeEntry left, TreeEntry right)
        {
            return !Equals(left, right);
        }
    }
}