using System;
using System.Collections.Generic;
using System.IO;

namespace LibGit2Sharp
{
    /// <summary>
    /// The archiving method needs to be passed an inheritor of this class, which will then be used
    /// to provide low-level archiving facilities (tar, zip, ...).
    /// <para>
    ///   <see cref="ObjectDatabase.Archive(LibGit2Sharp.Commit,LibGit2Sharp.ArchiverBase)"/>
    /// </para>
    /// </summary>
    public abstract class ArchiverBase
    {
        /// <summary>
        /// Override this method to perform operations before the archiving of each entry of the tree takes place.
        /// </summary>
        /// <param name="tree">The tree that will be archived</param>
        /// <param name="oid">The ObjectId of the commit being archived, or null if there is no commit.</param>
        /// <param name="modificationTime">The modification time that will be used for the files in the archive.</param>
        public virtual void BeforeArchiving(Tree tree, ObjectId oid, DateTimeOffset modificationTime)
        { }

        /// <summary>
        /// Override this method to perform operations after the archiving of each entry of the tree took place.
        /// </summary>
        /// <param name="tree">The tree that was archived</param>
        /// <param name="oid">The ObjectId of the commit being archived, or null if there is no commit.</param>
        /// <param name="modificationTime">The modification time that was used for the files in the archive.</param>
        public virtual void AfterArchiving(Tree tree, ObjectId oid, DateTimeOffset modificationTime)
        { }

        internal void OrchestrateArchiving(Tree tree, ObjectId oid, DateTimeOffset modificationTime)
        {
            BeforeArchiving(tree, oid, modificationTime);

            ArchiveTree(tree, "", modificationTime);

            AfterArchiving(tree, oid, modificationTime);
        }

        private void ArchiveTree(IEnumerable<TreeEntry> tree, string path, DateTimeOffset modificationTime)
        {
            foreach (var entry in tree)
            {
                AddTreeEntry(Path.Combine(path, entry.Name), entry, modificationTime);

                // Recurse if we have subtrees
                if (entry.Mode == Mode.Directory)
                {
                    ArchiveTree((Tree)entry.Target, Path.Combine(path, entry.Name), modificationTime);
                }
            }
        }

        /// <summary>
        /// Implements the archiving of a TreeEntry in a given format.
        /// </summary>
        /// <param name="path">The path of the entry in the archive.</param>
        /// <param name="entry">The entry to archive.</param>
        /// <param name="modificationTime">The datetime the entry was last modified.</param>
        protected abstract void AddTreeEntry(string path, TreeEntry entry, DateTimeOffset modificationTime);
    }
}
