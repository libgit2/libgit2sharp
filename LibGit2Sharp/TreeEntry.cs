using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Representation of an entry in a <see cref = "Tree" />.
    /// </summary>
    public class TreeEntry : IEquatable<TreeEntry>
    {
        private readonly ObjectId parentTreeId;
        private readonly Repository repo;
        private readonly Lazy<GitObject> target;
        private readonly ObjectId targetOid;

        private static readonly LambdaEqualityHelper<TreeEntry> equalityHelper =
            new LambdaEqualityHelper<TreeEntry>(new Func<TreeEntry, object>[] { x => x.Name, x => x.parentTreeId });

        internal TreeEntry(IntPtr obj, ObjectId parentTreeId, Repository repo)
        {
            this.parentTreeId = parentTreeId;
            this.repo = repo;
            IntPtr gitTreeEntryId = NativeMethods.git_tree_entry_id(obj);
            targetOid = new ObjectId((GitOid)Marshal.PtrToStructure(gitTreeEntryId, typeof(GitOid)));
            Type = NativeMethods.git_tree_entry_type(obj);
            target = new Lazy<GitObject>(RetrieveTreeEntryTarget);

            Attributes = (int)NativeMethods.git_tree_entry_attributes(obj);
            Name = NativeMethods.git_tree_entry_name(obj);
        }

        /// <summary>
        ///   Gets the UNIX file attributes.
        /// </summary>
        public int Attributes { get; private set; }

        /// <summary>
        ///   Gets the filename.
        ///   <para>The filename is expressed in a relative form. Path segments are separated with a forward slash."/></para>
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///   Gets the <see cref = "GitObject" /> being pointed at.
        /// </summary>
        public GitObject Target
        {
            get { return target.Value; }
        }

        /// <summary>
        ///   Gets the <see cref = "GitObjectType" /> of the <see cref = "Target" /> being pointed at.
        /// </summary>
        public GitObjectType Type { get; private set; }

        private GitObject RetrieveTreeEntryTarget()
        {
            GitObject treeEntryTarget = repo.Lookup(targetOid);

            //TODO: Warning submodules will appear as targets of type Commit
            Ensure.ArgumentConformsTo(treeEntryTarget.GetType(), t => typeof(Blob).IsAssignableFrom(t) || typeof(Tree).IsAssignableFrom(t), "treeEntryTarget");

            return treeEntryTarget;
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "Object" /> is equal to the current <see cref = "TreeEntry" />.
        /// </summary>
        /// <param name = "obj">The <see cref = "Object" /> to compare with the current <see cref = "TreeEntry" />.</param>
        /// <returns>True if the specified <see cref = "Object" /> is equal to the current <see cref = "TreeEntry" />; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as TreeEntry);
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "TreeEntry" /> is equal to the current <see cref = "TreeEntry" />.
        /// </summary>
        /// <param name = "other">The <see cref = "TreeEntry" /> to compare with the current <see cref = "TreeEntry" />.</param>
        /// <returns>True if the specified <see cref = "TreeEntry" /> is equal to the current <see cref = "TreeEntry" />; otherwise, false.</returns>
        public bool Equals(TreeEntry other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        ///   Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        ///   Tests if two <see cref = "TreeEntry" /> are equal.
        /// </summary>
        /// <param name = "left">First <see cref = "TreeEntry" /> to compare.</param>
        /// <param name = "right">Second <see cref = "TreeEntry" /> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(TreeEntry left, TreeEntry right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///   Tests if two <see cref = "TreeEntry" /> are different.
        /// </summary>
        /// <param name = "left">First <see cref = "TreeEntry" /> to compare.</param>
        /// <param name = "right">Second <see cref = "TreeEntry" /> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(TreeEntry left, TreeEntry right)
        {
            return !Equals(left, right);
        }
    }
}
