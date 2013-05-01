using System;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Underlying type of a <see cref = "GitObject" />
    /// </summary>
    public enum ObjectType
    {
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
    }

    internal static class ObjectTypeExtensions
    {
        public static Core.GitObjectType ToGitObjectType(this ObjectType type)
        {
            switch (type)
            {
                case ObjectType.Commit:
                    return Core.GitObjectType.Commit;

                case ObjectType.Tree:
                    return Core.GitObjectType.Tree;

                case ObjectType.Blob:
                    return Core.GitObjectType.Blob;

                case ObjectType.Tag:
                    return Core.GitObjectType.Tag;

                default:
                    throw new InvalidOperationException(string.Format("Cannot map {0} to a GitObjectType.", type));
            }
        }
    }
}
