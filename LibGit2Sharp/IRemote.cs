using System;

namespace LibGit2Sharp
{
    public interface IRemote : IEquatable<IRemote>
    {
        /// <summary>
        ///   Gets the alias of this remote repository.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///   Gets the url to use to communicate with this remote repository.
        /// </summary>
        string Url { get; }
    }
}