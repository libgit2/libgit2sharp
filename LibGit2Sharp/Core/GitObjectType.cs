using System;
using System.Globalization;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Underlying type of a <see cref="GitObject"/>
    /// </summary>
    internal enum GitObjectType
    {
        /// <summary>
        /// Object can be of any type.
        /// </summary>
        Any = -2,

        /// <summary>
        /// Object is invalid.
        /// </summary>
        Bad = -1,

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        Ext1 = 0,

        /// <summary>
        /// A commit object.
        /// </summary>
        Commit = 1,

        /// <summary>
        /// A tree (directory listing) object.
        /// </summary>
        Tree = 2,

        /// <summary>
        /// A file revision object.
        /// </summary>
        Blob = 3,

        /// <summary>
        /// An annotated tag object.
        /// </summary>
        Tag = 4,

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        Ext2 = 5,

        /// <summary>
        /// A delta, base is given by an offset.
        /// </summary>
        OfsDelta = 6,

        /// <summary>
        /// A delta, base is given by object id.
        /// </summary>
        RefDelta = 7
    }

    internal static class GitObjectTypeExtensions
    {
        public static TreeEntryTargetType ToTreeEntryTargetType(this GitObjectType type)
        {
            switch (type)
            {
                case GitObjectType.Commit:
                    return TreeEntryTargetType.GitLink;

                case GitObjectType.Tree:
                    return TreeEntryTargetType.Tree;

                case GitObjectType.Blob:
                    return TreeEntryTargetType.Blob;

                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                      "Cannot map {0} to a TreeEntryTargetType.",
                                                                      type));
            }
        }

        public static ObjectType ToObjectType(this GitObjectType type)
        {
            switch (type)
            {
                case GitObjectType.Commit:
                    return ObjectType.Commit;

                case GitObjectType.Tree:
                    return ObjectType.Tree;

                case GitObjectType.Blob:
                    return ObjectType.Blob;

                case GitObjectType.Tag:
                    return ObjectType.Tag;

                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                      "Cannot map {0} to a ObjectType.",
                                                                      type));
            }
        }
    }
}
