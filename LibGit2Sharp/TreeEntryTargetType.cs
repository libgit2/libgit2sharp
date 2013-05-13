using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Underlying type of the target a <see cref = "TreeEntry" />
    /// </summary>
    public enum TreeEntryTargetType
    {
        /// <summary>
        ///   A file revision object.
        /// </summary>
        Blob = 1,

        /// <summary>
        ///   A tree object.
        /// </summary>
        Tree,

        /// <summary>
        ///   An annotated tag object.
        /// </summary>
        [Obsolete("This entry will be removed in the next release as it is not a valid TreeEntryTargetType.")]
        Tag,

        /// <summary>
        ///   A pointer to a commit object in another repository.
        /// </summary>
        GitLink,
    }

    internal static class TreeEntryTargetTypeExtensions
    {
        public static GitObjectType ToGitObjectType(this TreeEntryTargetType type)
        {
            switch (type)
            {
                case TreeEntryTargetType.Tree:
                    return GitObjectType.Tree;

                case TreeEntryTargetType.Blob:
                    return GitObjectType.Blob;

                default:
                    throw new InvalidOperationException(string.Format("Cannot map {0} to a GitObjectType.", type));
            }
        }
    }
}
