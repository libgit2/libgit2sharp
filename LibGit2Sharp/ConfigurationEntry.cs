namespace LibGit2Sharp
{
    /// <summary>
    /// An enumerated configuration entry.
    /// </summary>
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
        /// Initializes a new instance of the <see cref="ConfigurationEntry"/> class with a given key and value
        /// </summary>
        /// <param name="key">The option name</param>
        /// <param name="value">The option value, as a string</param>
        public ConfigurationEntry(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
