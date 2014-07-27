using System.Collections.Generic;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides helper overloads to a <see cref="TreeDefinition"/>.
    /// </summary>
    public static class TreeDefinitionExtensions
    {
        /// <summary>
        /// Removes the <see cref="TreeEntryDefinition"/> located at each of the
        /// specified <paramref name="treeEntryPaths"/>.
        /// </summary>
        /// <param name="td">The <see cref="TreeDefinition"/>.</param>
        /// <param name="treeEntryPaths">The paths within this <see cref="TreeDefinition"/>.</param>
        /// <returns>The current <see cref="TreeDefinition"/>.</returns>
        public static TreeDefinition Remove(this TreeDefinition td, IEnumerable<string> treeEntryPaths)
        {
            Ensure.ArgumentNotNull(td, "td");
            Ensure.ArgumentNotNull(treeEntryPaths, "treeEntryPaths");

            foreach (var treeEntryPath in treeEntryPaths)
            {
                td.Remove(treeEntryPath);
            }

            return td;
        }
    }
}
