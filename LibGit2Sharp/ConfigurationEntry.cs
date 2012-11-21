using System.Diagnostics;

namespace LibGit2Sharp
{
    /// <summary>
    /// An enumerated configuration entry.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ConfigurationEntry
    {
        /// <summary>
        /// The option name.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// The option value.
        /// </summary>
        public string Value { get; private set; }


        /// <summary>
        /// The origin store.
        /// </summary>
        public ConfigurationLevel Level { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationEntry"/> class with a given key and value
        /// </summary>
        /// <param name="key">The option name</param>
        /// <param name="value">The option value, as a string</param>
        /// <param name="level">The origin store</param>
        public ConfigurationEntry(string key, string value, ConfigurationLevel level)
        {
            Key = key;
            Value = value;
            Level = level;
        }

        private string DebuggerDisplay
        {
            get { return string.Format("{0} = \"{1}\"", Key, Value); }
        }
    }
}
