using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    /// A filter registry
    /// </summary>
    public class FilterRegistry
    {
        /// <summary>
        /// Looks up a registered filter by its name. 
        /// </summary>
        /// <param name="name">The name to look up</param>
        /// <returns>The found matching filter</returns>
        public virtual Filter LookupByName(string name)
        {
            GitFilterSafeHandle gitFilterLookup = Proxy.git_filter_lookup(name);
            return new Filter(name, gitFilterLookup);
        }
    }
}