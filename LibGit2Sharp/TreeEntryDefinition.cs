using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Holds the meta data of a <see cref = "TreeEntry" />.
    /// </summary>
    public class TreeEntryDefinition : IEquatable<TreeEntryDefinition>
    {
        private Lazy<GitObject> target;

        private static readonly LambdaEqualityHelper<TreeEntryDefinition> equalityHelper =
            new LambdaEqualityHelper<TreeEntryDefinition>(x => x.Mode, x => x.Type, x => x.TargetId);

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected TreeEntryDefinition()
        {
        }

        /// <summary>
        ///   Gets file mode.
        /// </summary>
        public virtual Mode Mode { get; private set; }

        /// <summary>
        ///   Gets the <see cref = "GitObjectType" /> of the target being pointed at.
        /// </summary>
        public virtual GitObjectType Type { get; private set; }

        /// <summary>
        ///   Gets the <see cref = "ObjectId" /> of the target being pointed at.
        /// </summary>
        public virtual ObjectId TargetId { get; private set; }

        internal virtual GitObject Target
        {
            get { return target.Value; }
        }

        internal static TreeEntryDefinition From(TreeEntry treeEntry)
        {
            return new TreeEntryDefinition
                       {
                           Mode = treeEntry.Mode,
                           Type = treeEntry.Type,
                           TargetId = treeEntry.TargetId,
                           target = new Lazy<GitObject>(() => treeEntry.Target)
                       };
        }

        internal static TreeEntryDefinition From(Blob blob, Mode mode)
        {
            return new TreeEntryDefinition
                       {
                           Mode = mode,
                           Type = GitObjectType.Blob,
                           TargetId = blob.Id,
                           target = new Lazy<GitObject>(() => blob)
                       };
        }

        internal static TreeEntryDefinition TransientBlobFrom(string filePath, Mode mode)
        {
            Ensure.ArgumentConformsTo(mode, m => m.HasAny(new[] { Mode.NonExecutableFile, Mode.ExecutableFile, Mode.NonExecutableGroupWritableFile }), "mode");

            return new TransientBlobTreeEntryDefinition
                       {
                           Builder = odb => odb.CreateBlob(filePath),
                           Mode = mode,
                       };
        }

        internal static TreeEntryDefinition From(Tree tree)
        {
            return new TreeEntryDefinition
                       {
                           Mode = Mode.Directory,
                           Type = GitObjectType.Tree,
                           TargetId = tree.Id,
                           target = new Lazy<GitObject>(() => tree)
                       };
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "Object" /> is equal to the current <see cref = "TreeEntryDefinition" />.
        /// </summary>
        /// <param name = "obj">The <see cref = "Object" /> to compare with the current <see cref = "TreeEntryDefinition" />.</param>
        /// <returns>True if the specified <see cref = "Object" /> is equal to the current <see cref = "TreeEntryDefinition" />; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as TreeEntryDefinition);
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "TreeEntryDefinition" /> is equal to the current <see cref = "TreeEntryDefinition" />.
        /// </summary>
        /// <param name = "other">The <see cref = "TreeEntryDefinition" /> to compare with the current <see cref = "TreeEntryDefinition" />.</param>
        /// <returns>True if the specified <see cref = "TreeEntryDefinition" /> is equal to the current <see cref = "TreeEntryDefinition" />; otherwise, false.</returns>
        public bool Equals(TreeEntryDefinition other)
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
        ///   Tests if two <see cref = "TreeEntryDefinition" /> are equal.
        /// </summary>
        /// <param name = "left">First <see cref = "TreeEntryDefinition" /> to compare.</param>
        /// <param name = "right">Second <see cref = "TreeEntryDefinition" /> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(TreeEntryDefinition left, TreeEntryDefinition right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///   Tests if two <see cref = "TreeEntryDefinition" /> are different.
        /// </summary>
        /// <param name = "left">First <see cref = "TreeEntryDefinition" /> to compare.</param>
        /// <param name = "right">Second <see cref = "TreeEntryDefinition" /> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(TreeEntryDefinition left, TreeEntryDefinition right)
        {
            return !Equals(left, right);
        }
    }

    internal abstract class TransientTreeEntryDefinition : TreeEntryDefinition
    {
        public override ObjectId TargetId
        {
            get { return ObjectId.Zero; }
        }

        internal override GitObject Target
        {
            get { return null; }
        }
    }

    internal class TransientTreeTreeEntryDefinition : TransientTreeEntryDefinition
    {
        public override Mode Mode
        {
            get { return Mode.Directory; }
        }

        public override GitObjectType Type
        {
            get { return GitObjectType.Tree; }
        }
    }

    internal class TransientBlobTreeEntryDefinition : TransientTreeEntryDefinition
    {
        public override GitObjectType Type
        {
            get { return GitObjectType.Blob; }
        }

        public Func<ObjectDatabase, Blob> Builder { get; set; }
    }
}
