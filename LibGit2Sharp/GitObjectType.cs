using System;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Underlying type of a <see cref = "GitObject" />
    /// </summary>
    [Obsolete("This type will be removed in the next release.")]
    public enum GitObjectType
    {
        /// <summary>
        ///   Object can be of any type.
        /// </summary>
        Any = -2,

        /// <summary>
        ///   Object is invalid.
        /// </summary>
        Bad = -1,

        /// <summary>
        ///   Reserved for future use.
        /// </summary>
        Ext1 = 0,

        /// <summary>
        ///   A commit object.
        /// </summary>
        Commit = 1,

        /// <summary>
        ///   A tree (directory listing) object.
        /// </summary>
        Tree = 2,

        /// <summary>
        ///   A file revision object.
        /// </summary>
        Blob = 3,

        /// <summary>
        ///   An annotated tag object.
        /// </summary>
        Tag = 4,

        /// <summary>
        ///   Reserved for future use.
        /// </summary>
        Ext2 = 5,

        /// <summary>
        ///   A delta, base is given by an offset.
        /// </summary>
        OfsDelta = 6,

        /// <summary>
        ///   A delta, base is given by object id.
        /// </summary>
        RefDelta = 7
    }

    internal static class GitObjectTypeExtensions
    {
        public static Core.GitObjectType ToCoreGitObjectType(this GitObjectType type)
        {
            return (Core.GitObjectType)type;
        }
    }
}
