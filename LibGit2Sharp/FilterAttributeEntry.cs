using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// The definition for a given filter found in the .gitattributes file.
    /// The filter definition will result as 'filter=filterName'
    ///
    /// In the .gitattributes file a filter will be matched to a pathspec like so
    /// '*.txt filter=filterName'
    /// </summary>
    public class FilterAttributeEntry
    {
        private const string AttributeFilterDefinition = "filter=";

        private readonly string filterDefinition;

        /// <summary>
        /// For testing purposes
        /// </summary>
        protected FilterAttributeEntry() { }

        /// <summary>
        /// The name of the filter found in a .gitattributes file
        /// </summary>
        /// <param name="filterName">The name of the filter</param>
        public FilterAttributeEntry(string filterName)
        {
            Ensure.ArgumentNotNullOrEmptyString(filterName, "filterName");

            if (!filterName.Contains(AttributeFilterDefinition))
            {
                filterName = string.Format("{0}{1}", AttributeFilterDefinition, filterName);
            }

            this.filterDefinition = filterName;
        }

        /// <summary>
        /// The filter name in the form of 'filter=filterName'
        /// </summary>
        public virtual string FilterDefinition
        {
            get { return filterDefinition; }
        }
    }
}
