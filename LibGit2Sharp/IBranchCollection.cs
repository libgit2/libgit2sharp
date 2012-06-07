using System.Collections.Generic;

namespace LibGit2Sharp
{
    public interface IBranchCollection : IEnumerable<IBranch>
    {
        /// <summary>
        ///   Gets the <see cref = "LibGit2Sharp.Branch" /> with the specified name.
        /// </summary>
        IBranch this[string name] { get; }

        /// <summary>
        ///   Create a new local branch with the specified name
        /// </summary>
        /// <param name = "name">The name of the branch.</param>
        /// <param name = "shaOrReferenceName">The target which can be sha or a canonical reference name.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        /// <returns></returns>
        IBranch Create(string name, string shaOrReferenceName, bool allowOverwrite = false);

        /// <summary>
        ///   Deletes the branch with the specified name.
        /// </summary>
        /// <param name = "name">The name of the branch to delete.</param>
        /// <param name = "isRemote">True if the provided <paramref name="name"/> is the name of a remote branch, false otherwise.</param>
        void Delete(string name, bool isRemote = false);

        ///<summary>
        ///  Rename an existing local branch with a new name.
        ///</summary>
        ///<param name = "currentName">The current branch name.</param>
        ///<param name = "newName">The new name of the existing branch should bear.</param>
        ///<param name = "allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        ///<returns></returns>
        IBranch Move(string currentName, string newName, bool allowOverwrite = false);
    }
}