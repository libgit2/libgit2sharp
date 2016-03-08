using System.Diagnostics;
using System.Globalization;

namespace LibGit2Sharp
{
    /// <summary>
    /// The full representation of a config option.
    /// </summary>
    /// <typeparam name="T">The configuration value type</typeparam>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ConfigurationEntry<T>
    {
        /// <summary>
        /// The fully-qualified option name.
        /// </summary>
        public virtual string Key { get; set; }

        /// <summary>
        /// The option value.
        /// </summary>
        public virtual T Value { get; set; }

        /// <summary>
        /// The origin store.
        /// </summary>
        public virtual ConfigurationLevel Level { get; set; }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0} = \"{1}\"", Key, Value);
            }
        }
    }
}
