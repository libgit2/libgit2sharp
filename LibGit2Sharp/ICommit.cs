using System.Collections.Generic;

namespace LibGit2Sharp
{
    public interface ICommit
    {
        /// <summary>
        ///   Gets the <see cref = "TreeEntry" /> pointed at by the <paramref name = "relativePath" /> in the <see cref = "Tree" />.
        /// </summary>
        /// <param name = "relativePath">The relative path to the <see cref = "TreeEntry" /> from the <see cref = "Commit" /> working directory.</param>
        /// <returns><c>null</c> if nothing has been found, the <see cref = "TreeEntry" /> otherwise.</returns>
        TreeEntry this[string relativePath] { get; }

        /// <summary>
        ///   Gets the commit message.
        /// </summary>
        string Message { get; }

        /// <summary>
        ///   Gets the short commit message which is usually the first line of the commit.
        /// </summary>
        string MessageShort { get; }

        /// <summary>
        ///   Gets the encoding of the message.
        /// </summary>
        string Encoding { get; }

        /// <summary>
        ///   Gets the author of this commit.
        /// </summary>
        Signature Author { get; }

        /// <summary>
        ///   Gets the committer.
        /// </summary>
        Signature Committer { get; }

        /// <summary>
        ///   Gets the Tree associated to this commit.
        /// </summary>
        Tree Tree { get; }

        /// <summary>
        ///   Gets the parents of this commit. This property is lazy loaded and can throw an exception if the commit no longer exists in the repo.
        /// </summary>
        IEnumerable<ICommit> Parents { get; }

        /// <summary>
        ///   Gets The count of parent commits.
        /// </summary>
        int ParentsCount { get; }

        /// <summary>
        ///   Gets the id of this object
        /// </summary>
        ObjectId Id { get; }

        /// <summary>
        ///   Gets the 40 character sha1 of this object.
        /// </summary>
        string Sha { get; }
    }
}