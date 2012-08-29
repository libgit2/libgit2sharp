using System;
using System.Globalization;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Core.Handles;

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
        private readonly Lazy<string> path;

        private static readonly LambdaEqualityHelper<TreeEntry> equalityHelper =
            new LambdaEqualityHelper<TreeEntry>(new Func<TreeEntry, object>[] { x => x.Name, x => x.parentTreeId });

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected TreeEntry()
        { }

        internal TreeEntry(SafeHandle obj, ObjectId parentTreeId, Repository repo, FilePath parentPath)
        {
            this.parentTreeId = parentTreeId;
            this.repo = repo;
            targetOid = Proxy.git_tree_entry_id(obj);
            Type = Proxy.git_tree_entry_type(obj);
            target = new Lazy<GitObject>(RetrieveTreeEntryTarget);

            Mode = Proxy.git_tree_entry_attributes(obj);
            Name = Proxy.git_tree_entry_name(obj);
            path = new Lazy<string>(() => System.IO.Path.Combine(parentPath.Native, Name));
        }

        /// <summary>
        ///   Gets the file mode.
        /// </summary>
        public virtual Mode Mode { get; private set; }

        /// <summary>
        ///   Gets the filename.
        /// </summary>
        public virtual string Name { get; private set; }

        /// <summary>
        ///   Gets the path.
        ///   <para>The path is expressed in a relative form from the latest known <see cref="Tree"/>. Path segments are separated with a forward or backslash, depending on the OS the libray is being run on."/></para>
        /// </summary>
        public virtual string Path { get { return path.Value; } }

        /// <summary>
        ///   Gets the <see cref = "GitObject" /> being pointed at.
        /// </summary>
        public virtual GitObject Target
        {
            get { return target.Value; }
        }

        internal ObjectId TargetId
        {
            get { return targetOid; }
        }

        /// <summary>
        ///   Gets the <see cref = "GitObjectType" /> of the <see cref = "Target" /> being pointed at.
        /// </summary>
        public virtual GitObjectType Type { get; private set; }

        private GitObject RetrieveTreeEntryTarget()
        {
            if (!Type.HasAny(new[]{GitObjectType.Tree, GitObjectType.Blob}))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "TreeEntry target of type '{0}' are not supported.", Type));
            }

            GitObject treeEntryTarget = repo.LookupTreeEntryTarget(targetOid, Path);

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
