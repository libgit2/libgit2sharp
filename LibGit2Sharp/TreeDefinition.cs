﻿using System;
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Holds the meta data of a <see cref = "Tree" />.
    /// </summary>
    public class TreeDefinition
    {
        private readonly Dictionary<string, TreeEntryDefinition> entries = new Dictionary<string, TreeEntryDefinition>();
        private readonly Dictionary<string, TreeDefinition> unwrappedTrees = new Dictionary<string, TreeDefinition>();

        /// <summary>
        ///   Builds a <see cref = "TreeDefinition" /> from an existing <see cref = "Tree" />.
        /// </summary>
        /// <param name = "tree">The <see cref = "Tree" /> to be processed.</param>
        /// <returns>A new <see cref = "TreeDefinition" /> holding the meta data of the <paramref name = "tree" />.</returns>
        public static TreeDefinition From(Tree tree)
        {
            Ensure.ArgumentNotNull(tree, "tree");

            var td = new TreeDefinition();

            foreach (TreeEntry treeEntry in tree)
            {
                td.AddEntry(treeEntry.Name, TreeEntryDefinition.From(treeEntry));
            }

            return td;
        }

        private void AddEntry(string targetTreeEntryName, TreeEntryDefinition treeEntryDefinition)
        {
            if (entries.ContainsKey(targetTreeEntryName))
            {
                WrapTree(targetTreeEntryName, treeEntryDefinition);
                return;
            }

            entries.Add(targetTreeEntryName, treeEntryDefinition);
        }

        /// <summary>
        ///   Removes a <see cref="TreeEntryDefinition"/> located the specified <paramref name="treeEntryPath"/> path.
        /// </summary>
        /// <param name="treeEntryPath">The path within this <see cref="TreeDefinition"/>.</param>
        /// <returns>The current <see cref="TreeDefinition"/>.</returns>
        public TreeDefinition Remove(string treeEntryPath)
        {
            Ensure.ArgumentNotNullOrEmptyString(treeEntryPath, "treeEntryPath");

            if (this[treeEntryPath] == null)
            {
                return this;
            }

            Tuple<string, string> segments = ExtractPosixLeadingSegment(treeEntryPath);

            if (segments.Item2 == null)
            {
                entries.Remove(segments.Item1);
            }

            if (!unwrappedTrees.ContainsKey(segments.Item1))
            {
                return this;
            }

            if (segments.Item2 != null)
            {
                unwrappedTrees[segments.Item1].Remove(segments.Item2);
            }

            if (unwrappedTrees[segments.Item1].entries.Count == 0)
            {
                unwrappedTrees.Remove(segments.Item1);
                entries.Remove(segments.Item1);
            }

            return this;
        }

        /// <summary>
        ///   Adds or replaces a <see cref="TreeEntryDefinition"/> at the specified <paramref name="targetTreeEntryPath"/> location.
        /// </summary>
        /// <param name="targetTreeEntryPath">The path within this <see cref="TreeDefinition"/>.</param>
        /// <param name="treeEntryDefinition">The <see cref="TreeEntryDefinition"/> to be stored at the described location.</param>
        /// <returns>The current <see cref="TreeDefinition"/>.</returns>
        public TreeDefinition Add(string targetTreeEntryPath, TreeEntryDefinition treeEntryDefinition)
        {
            Ensure.ArgumentNotNullOrEmptyString(targetTreeEntryPath, "targetTreeEntryPath");
            Ensure.ArgumentNotNull(treeEntryDefinition, "treeEntryDefinition");

            if (Path.IsPathRooted(targetTreeEntryPath))
            {
                throw new ArgumentException("The provided path is an absolute path.");
            }

            if (treeEntryDefinition is TransientTreeTreeEntryDefinition)
            {
                throw new InvalidOperationException(string.Format("The {0} references a target which hasn't been created in the {1} yet. This situation can occur when the target is a whole new {2} being created, or when an existing {2} is being updated because some of its children were added/removed.", typeof(TreeEntryDefinition).Name, typeof(ObjectDatabase).Name, typeof(Tree).Name));
            }

            Tuple<string, string> segments = ExtractPosixLeadingSegment(targetTreeEntryPath);

            if (segments.Item2 != null)
            {
                TreeDefinition td = RetrieveOrBuildTreeDefinition(segments.Item1, true);
                td.Add(segments.Item2, treeEntryDefinition);
            }
            else
            {
                AddEntry(segments.Item1, treeEntryDefinition);
            }

            return this;
        }

        /// <summary>
        ///   Adds or replaces a <see cref="TreeEntryDefinition"/>, dynamically built from the provided <see cref="Blob"/>, at the specified <paramref name="targetTreeEntryPath"/> location.
        /// </summary>
        /// <param name="targetTreeEntryPath">The path within this <see cref="TreeDefinition"/>.</param>
        /// <param name="blob">The <see cref="Blob"/> to be stored at the described location.</param>
        /// <param name="mode">The file related <see cref="Mode"/> attributes.</param>
        /// <returns>The current <see cref="TreeDefinition"/>.</returns>
        public TreeDefinition Add(string targetTreeEntryPath, IBlob blob, Mode mode)
        {
            Ensure.ArgumentNotNull(blob, "blob");
            Ensure.ArgumentConformsTo(mode,
                                      m => m.HasAny(new[] { Mode.ExecutableFile, Mode.NonExecutableFile, Mode.NonExecutableGroupWriteableFile }), "mode");

            TreeEntryDefinition ted = TreeEntryDefinition.From(blob, mode);

            return Add(targetTreeEntryPath, ted);
        }

        /// <summary>
        ///   Adds or replaces a <see cref="TreeEntryDefinition"/>, dynamically built from the content of the file, at the specified <paramref name="targetTreeEntryPath"/> location.
        /// </summary>
        /// <param name="targetTreeEntryPath">The path within this <see cref="TreeDefinition"/>.</param>
        /// <param name="filePath">The path to the file from which a <see cref="Blob"/> will be built and stored at the described location.</param>
        /// <param name="mode">The file related <see cref="Mode"/> attributes.</param>
        /// <returns>The current <see cref="TreeDefinition"/>.</returns>
        public TreeDefinition Add(string targetTreeEntryPath, string filePath, Mode mode)
        {
            Ensure.ArgumentNotNullOrEmptyString(filePath, "filePath");

            TreeEntryDefinition ted = TreeEntryDefinition.TransientBlobFrom(filePath, mode);

            return Add(targetTreeEntryPath, ted);
        }

        /// <summary>
        ///   Adds or replaces a <see cref="TreeEntryDefinition"/>, dynamically built from the provided <see cref="Tree"/>, at the specified <paramref name="targetTreeEntryPath"/> location.
        /// </summary>
        /// <param name="targetTreeEntryPath">The path within this <see cref="TreeDefinition"/>.</param>
        /// <param name="tree">The <see cref="Tree"/> to be stored at the described location.</param>
        /// <returns>The current <see cref="TreeDefinition"/>.</returns>
        public TreeDefinition Add(string targetTreeEntryPath, Tree tree)
        {
            Ensure.ArgumentNotNull(tree, "tree");

            TreeEntryDefinition ted = TreeEntryDefinition.From(tree);

            return Add(targetTreeEntryPath, ted);
        }

        private TreeDefinition RetrieveOrBuildTreeDefinition(string treeName, bool shouldOverWrite)
        {
            TreeDefinition td;

            if (unwrappedTrees.TryGetValue(treeName, out td))
            {
                return td;
            }

            TreeEntryDefinition treeEntryDefinition;
            bool hasAnEntryBeenFound = entries.TryGetValue(treeName, out treeEntryDefinition);

            if (hasAnEntryBeenFound)
            {
                switch (treeEntryDefinition.Type)
                {
                    case GitObjectType.Tree:
                        td = From(treeEntryDefinition.Target as Tree);
                        break;

                    case GitObjectType.Blob:
                        if (shouldOverWrite)
                        {
                            td = new TreeDefinition();
                            break;
                        }

                        return null;

                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                if (!shouldOverWrite)
                {
                    return null;
                }

                td = new TreeDefinition();
            }

            entries[treeName] = new TransientTreeTreeEntryDefinition();

            unwrappedTrees.Add(treeName, td);
            return td;
        }

        internal Tree Build(Repository repository)
        {
            WrapAllTreeDefinitions(repository);

            using (var builder = new TreeBuilder())
            {
                var builtTreeEntryDefinitions = new List<Tuple<string, TreeEntryDefinition>>();

                foreach (KeyValuePair<string, TreeEntryDefinition> kvp in entries)
                {
                    string name = kvp.Key;
                    TreeEntryDefinition ted = kvp.Value;

                    var transient = ted as TransientBlobTreeEntryDefinition;

                    if (transient == null)
                    {
                        builder.Insert(name, ted);
                        continue;
                    }

                    IBlob blob = transient.Builder(repository.ObjectDatabase);
                    TreeEntryDefinition ted2 = TreeEntryDefinition.From(blob, ted.Mode);
                    builtTreeEntryDefinitions.Add(new Tuple<string, TreeEntryDefinition>(name, ted2));

                    builder.Insert(name, ted2);
                }

                builtTreeEntryDefinitions.ForEach(t => entries[t.Item1] = t.Item2);

                ObjectId treeId = builder.Write(repository);
                return repository.Lookup<Tree>(treeId);
            }
        }

        private void WrapAllTreeDefinitions(Repository repository)
        {
            foreach (KeyValuePair<string, TreeDefinition> pair in unwrappedTrees)
            {
                Tree tree = pair.Value.Build(repository);
                entries[pair.Key] = TreeEntryDefinition.From(tree);
            }

            unwrappedTrees.Clear();
        }

        private void WrapTree(string entryName, TreeEntryDefinition treeEntryDefinition)
        {
            entries[entryName] = treeEntryDefinition;
            unwrappedTrees.Remove(entryName);
        }

        /// <summary>
        ///   Retrieves the <see cref="TreeEntryDefinition"/> located the specified <paramref name="treeEntryPath"/> path.
        /// </summary>
        /// <param name="treeEntryPath">The path within this <see cref="TreeDefinition"/>.</param>
        /// <returns>The found <see cref="TreeEntryDefinition"/> if any; null otherwise.</returns>
        public TreeEntryDefinition this[string treeEntryPath]
        {
            get
            {
                Ensure.ArgumentNotNullOrEmptyString(treeEntryPath, "treeEntryPath");

                Tuple<string, string> segments = ExtractPosixLeadingSegment(treeEntryPath);

                if (segments.Item2 != null)
                {
                    TreeDefinition td = RetrieveOrBuildTreeDefinition(segments.Item1, false);
                    return td == null ? null : td[segments.Item2];
                }

                TreeEntryDefinition treeEntryDefinition;
                return !entries.TryGetValue(segments.Item1, out treeEntryDefinition) ? null : treeEntryDefinition;
            }
        }

        private static Tuple<string, string> ExtractPosixLeadingSegment(FilePath targetPath)
        {
            string[] segments = targetPath.Posix.Split(new[] { '/' }, 2);

            if (segments[0] == string.Empty || (segments.Length == 2 && (segments[1] == string.Empty || segments[1].StartsWith("/"))))
            {
                throw new ArgumentException(string.Format("'{0}' is not a valid path.", targetPath));
            }

            return new Tuple<string, string>(segments[0], segments.Length == 2 ? segments[1] : null);
        }

        private class TreeBuilder : IDisposable
        {
            private readonly TreeBuilderSafeHandle handle;

            public TreeBuilder()
            {
                Ensure.Success(NativeMethods.git_treebuilder_create(out handle, IntPtr.Zero));
            }

            public void Insert(string name, TreeEntryDefinition treeEntryDefinition)
            {
                GitOid oid = treeEntryDefinition.TargetId.Oid;

                Ensure.Success(NativeMethods.git_treebuilder_insert(IntPtr.Zero, handle, name, ref oid, (uint)treeEntryDefinition.Mode));
            }

            public ObjectId Write(Repository repo)
            {
                GitOid oid;
                Ensure.Success(NativeMethods.git_treebuilder_write(out oid, repo.Handle, handle));

                return new ObjectId(oid);
            }

            public void Dispose()
            {
                handle.SafeDispose();
            }
        }
    }
}
