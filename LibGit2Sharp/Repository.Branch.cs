using System;

namespace LibGit2Sharp
{
    public partial class Repository
    {
        /// <summary>
        /// Creates a branch with the specified name. This branch will point at the commit pointed at by the <see cref="Repository.Head"/>.
        /// </summary>
        /// <param name="branchName">The name of the branch to create.</param>
        public Branch CreateBranch(string branchName)
        {
            var head = Head;
            var reflogName = head is DetachedHead ? head.Tip.Sha : head.FriendlyName;

            return CreateBranch(branchName, reflogName);
        }

        /// <summary>
        /// Creates a branch with the specified name. This branch will point at <paramref name="target"/>.
        /// </summary>
        /// <param name="branchName">The name of the branch to create.</param>
        /// <param name="target">The commit which should be pointed at by the Branch.</param>
        public Branch CreateBranch(string branchName, Commit target)
        {
            return Branches.Add(branchName, target);
        }

        /// <summary>
        /// Creates a branch with the specified name. This branch will point at the commit pointed at by the <see cref="Repository.Head"/>.
        /// </summary>
        /// <param name="branchName">The name of the branch to create.</param>
        /// <param name="committish">The revparse spec for the target commit.</param>
        public Branch CreateBranch( string branchName, string committish)
        {
            return Branches.Add(branchName, committish);
        }
    }
}

