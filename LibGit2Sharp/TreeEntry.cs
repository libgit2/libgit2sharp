using System;
using System.Diagnostics;
using System.Globalization;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// Representation of an entry in a <see cref="Tree"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class TreeEntry : IEquatable<TreeEntry>
    {
        private readonly ObjectId parentTreeId;
        private readonly Repository repo;
        private readonly Lazy<GitObject> target;
        private readonly ObjectId targetOid;
        private readonly Lazy<string> path;

        private static readonly LambdaEqualityHelper<TreeEntry> equalityHelper =
            new LambdaEqualityHelper<TreeEntry>(x => x.Name, x => x.parentTreeId);

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected TreeEntry()
        { }

        internal unsafe TreeEntry(TreeEntryHandle entry, ObjectId parentTreeId, Repository repo, string parentPath)
        {
            this.parentTreeId = parentTreeId;
            this.repo = repo;
            targetOid = Proxy.git_tree_entry_id(entry);

            GitObjectType treeEntryTargetType = Proxy.git_tree_entry_type(entry);
            TargetType = treeEntryTargetType.ToTreeEntryTargetType();

            target = new Lazy<GitObject>(RetrieveTreeEntryTarget);

            Mode = Proxy.git_tree_entry_attributes(entry);
            Name = Proxy.git_tree_entry_name(entry);
            path = new Lazy<string>(() => Tree.CombinePath(parentPath, Name));
        }

        /// <summary>
        /// Gets the file mode.
        /// </summary>
        public virtual Mode Mode { get; private set; }

        /// <summary>
        /// Gets the filename.
        /// </summary>
        public virtual string Name { get; private set; }

        /// <summary>
        /// Gets the path.
        /// <para>The path is expressed in a relative form from the latest known <see cref="Tree"/>. Path segments are separated with a forward or backslash, depending on the OS the libray is being run on."/></para>
        /// </summary>
        public virtual string Path { get { return path.Value; } }

        /// <summary>
        /// Gets the <see cref="GitObject"/> being pointed at.
        /// </summary>
        public virtual GitObject Target { get { return target.Value; } }

        internal ObjectId TargetId
        {
            get { return targetOid; }
        }

        /// <summary>
        /// Gets the <see cref="TreeEntryTargetType"/> of the <see cref="Target"/> being pointed at.
        /// </summary>
        public virtual TreeEntryTargetType TargetType { get; private set; }

        private GitObject RetrieveTreeEntryTarget()
        {
            switch (TargetType)
            {
                case TreeEntryTargetType.GitLink:
                    return new GitLink(repo, targetOid);

                case TreeEntryTargetType.Blob:
                case TreeEntryTargetType.Tree:
                    return GitObject.BuildFrom(repo, targetOid, TargetType.ToGitObjectType(), Path);

                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                      "TreeEntry target of type '{0}' is not supported.",
                                                                      TargetType));
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="TreeEntry"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="TreeEntry"/>.</param>
        /// <returns>True if the specified <see cref="object"/> is equal to the current <see cref="TreeEntry"/>; otherwise, false.</returns>
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

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                                     "TreeEntry: {0} => {1}",
                                     Path,
                                     TargetId);
            }
        }
    }
}
