using System.Collections.Generic;

namespace LibGit2Sharp
{
    public interface IRemoteCollection : IEnumerable<IRemote>
    {
        /// <summary>
        ///   Gets the <see cref = "Remote" /> with the specified name.
        /// </summary>
        /// <param name = "name">The name of the remote to retrieve.</param>
        /// <returns>The retrived <see cref = "Remote" /> if it has been found, null otherwise.</returns>
        IRemote this[string name] { get; }

        /// <summary>
        ///   Creates a <see cref="Remote"/> with the specified name and for the repository at the specified location.
        ///   <para>
        ///     A default fetch refspec will be added for this remote.
        ///   </para>
        /// </summary>
        /// <param name = "name">The name of the remote to create.</param>
        /// <param name = "url">The location of the repository.</param>
        /// <returns>A new <see cref = "Remote" />.</returns>
        IRemote Create(string name, string url);

        /// <summary>
        ///   Creates a <see cref="Remote"/> with the specified name and for the repository at the specified location.
        /// </summary>
        /// <param name = "name">The name of the remote to create.</param>
        /// <param name = "url">The location of the repository.</param>
        /// <param name = "fetchRefSpec">The refSpec to be used when fetching from this remote..</param>
        /// <returns>A new <see cref = "Remote" />.</returns>
        IRemote Create(string name, string url, string fetchRefSpec);
    }
}