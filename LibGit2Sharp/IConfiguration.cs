using System;
using System.Collections.Generic;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides access to configuration variables for a repository.
    /// </summary>
    public interface IConfiguration : IDisposable,
        IEnumerable<ConfigurationEntry<string>>
    {
    }
}
