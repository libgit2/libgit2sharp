using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using System.Text;
using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// A container which references a list of other <see cref="Tree"/>s and <see cref="Blob"/>s.
    /// </summary>
    /// <remarks>
    /// Since the introduction of partially cloned repositories, trees might be missing on your local repository (see https://git-scm.com/docs/partial-clone)
    /// </remarks>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Tree : GitObject, IEnumerable<TreeEntry>
    {
        private readonly string path;

        private readonly ILazy<int> lazyCount;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Tree()
        { }

        internal Tree(Repository repo, ObjectId id, string path)
            : base(repo, id)
        {
            this.path = path ?? "";

            lazyCount = GitObjectLazyGroup.Singleton(repo, id, Proxy.git_tree_entrycount, throwIfMissing: true);
        }

        /// <summary>
        /// Gets the number of <see cref="TreeEntry"/> immediately under this <see cref="Tree"/>.
        /// </summary>
        /// <exception cref="NotFoundException">Throws if tree is missing</exception>
        public virtual int Count => lazyCount.Value;

        /// <summary>
        /// Gets the <see cref="TreeEntry"/> pointed at by the <paramref name="relativePath"/> in this <see cref="Tree"/> instance.
        /// </summary>
        /// <param name="relativePath">The relative path to the <see cref="TreeEntry"/> from this instance.</param>
        /// <returns><c>null</c> if nothing has been found, the <see cref="TreeEntry"/> otherwise.</returns>
        /// <exception cref="NotFoundException">Throws if tree is missing</exception>
        public virtual TreeEntry this[string relativePath]
        {
            get { return RetrieveFromPath(relativePath); }
        }

        private unsafe TreeEntry RetrieveFromPath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return null;
            }

            using (TreeEntryHandle treeEntry = Proxy.git_tree_entry_bypath(repo.Handle, Id, relativePath))
            {
                if (treeEntry == null)
                {
                    return null;
                }

                string filename = relativePath.Split('/').Last();
                string parentPath = relativePath.Substring(0, relativePath.Length - filename.Length);
                return new TreeEntry(treeEntry, Id, repo, Tree.CombinePath(path, parentPath));
            }
        }

        internal string Path => path;

        #region IEnumerable<TreeEntry> Members

        unsafe TreeEntry byIndex(ObjectSafeWrapper obj, uint i, ObjectId parentTreeId, Repository repo, string parentPath)
        {
            using (var entryHandle = Proxy.git_tree_entry_byindex(obj.ObjectPtr, i))
            {
                return new TreeEntry(entryHandle, parentTreeId, repo, parentPath);
            }
        }

        internal static string CombinePath(string a, string b)
        {
            var bld = new StringBuilder();
            bld.Append(a);
            if (!string.IsNullOrEmpty(a) &&
                !a.EndsWith("/", StringComparison.Ordinal) &&
                !b.StartsWith("/", StringComparison.Ordinal))
            {
                bld.Append('/');
            }
            bld.Append(b);

            return bld.ToString();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        /// <exception cref="NotFoundException">Throws if tree is missing</exception>
        public virtual IEnumerator<TreeEntry> GetEnumerator()
        {
            using (var obj = new ObjectSafeWrapper(Id, repo.Handle, throwIfMissing: true))
            {
                for (uint i = 0; i < Count; i++) {
                    yield return byIndex(obj, i, Id, repo, path);
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        /// <exception cref="NotFoundException">Throws if tree is missing</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                                     "{0}, Count = {1}",
                                     Id.ToString(7),
                                     Count);
            }
        }
    }
}
