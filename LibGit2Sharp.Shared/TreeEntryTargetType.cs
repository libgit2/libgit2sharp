using System;
using System.Globalization;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Underlying type of the target a <see cref="TreeEntry"/>
    /// </summary>
    public enum TreeEntryTargetType
    {
        /// <summary>
        /// A file revision object.
        /// </summary>
        Blob,

        /// <summary>
        /// A tree object.
        /// </summary>
        Tree,

        /// <summary>
        /// A pointer to a commit object in another repository.
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
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                      "Cannot map {0} to a GitObjectType.",
                                                                      type));
            }
        }
    }
}
