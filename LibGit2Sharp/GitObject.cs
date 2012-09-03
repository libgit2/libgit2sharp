using System;
using System.Globalization;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A GitObject
    /// </summary>
    public class GitObject : IEquatable<GitObject>
    {
        internal static GitObjectTypeMap TypeToTypeMap =
            new GitObjectTypeMap
                {
                    { typeof(Commit), GitObjectType.Commit },
                    { typeof(Tree), GitObjectType.Tree },
                    { typeof(Blob), GitObjectType.Blob },
                    { typeof(TagAnnotation), GitObjectType.Tag },
                    { typeof(GitObject), GitObjectType.Any },
                };

        private static readonly LambdaEqualityHelper<GitObject> equalityHelper =
            new LambdaEqualityHelper<GitObject>(new Func<GitObject, object>[] { x => x.Id });

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected GitObject()
        { }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "GitObject" /> class.
        /// </summary>
        /// <param name = "id">The <see cref = "ObjectId" /> it should be identified by.</param>
        protected GitObject(ObjectId id)
        {
            Id = id;
        }

        /// <summary>
        ///   Gets the id of this object
        /// </summary>
        public virtual ObjectId Id { get; private set; }

        /// <summary>
        ///   Gets the 40 character sha1 of this object.
        /// </summary>
        public virtual string Sha
        {
            get { return Id.Sha; }
        }

        internal static GitObject BuildFromPtr(GitObjectSafeHandle obj, ObjectId id, Repository repo, FilePath path)
        {
            GitObjectType type = Proxy.git_object_type(obj);
            switch (type)
            {
                case GitObjectType.Commit:
                    return Commit.BuildFromPtr(obj, id, repo);
                case GitObjectType.Tree:
                    return Tree.BuildFromPtr(obj, id, repo, path);
                case GitObjectType.Tag:
                    return TagAnnotation.BuildFromPtr(obj, id, repo);
                case GitObjectType.Blob:
                    return Blob.BuildFromPtr(obj, id, repo);
                default:
                    throw new LibGit2SharpException(string.Format(CultureInfo.InvariantCulture, "Unexpected type '{0}' for object '{1}'.", type, id));
            }
        }

        internal static ObjectId ObjectIdOf(GitObjectSafeHandle gitObjHandle)
        {
            return Proxy.git_object_id(gitObjHandle);
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "Object" /> is equal to the current <see cref = "GitObject" />.
        /// </summary>
        /// <param name = "obj">The <see cref = "Object" /> to compare with the current <see cref = "GitObject" />.</param>
        /// <returns>True if the specified <see cref = "Object" /> is equal to the current <see cref = "GitObject" />; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as GitObject);
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "GitObject" /> is equal to the current <see cref = "GitObject" />.
        /// </summary>
        /// <param name = "other">The <see cref = "GitObject" /> to compare with the current <see cref = "GitObject" />.</param>
        /// <returns>True if the specified <see cref = "GitObject" /> is equal to the current <see cref = "GitObject" />; otherwise, false.</returns>
        public bool Equals(GitObject other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        ///   Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        ///   Tests if two <see cref = "GitObject" /> are equal.
        /// </summary>
        /// <param name = "left">First <see cref = "GitObject" /> to compare.</param>
        /// <param name = "right">Second <see cref = "GitObject" /> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(GitObject left, GitObject right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///   Tests if two <see cref = "GitObject" /> are different.
        /// </summary>
        /// <param name = "left">First <see cref = "GitObject" /> to compare.</param>
        /// <param name = "right">Second <see cref = "GitObject" /> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(GitObject left, GitObject right)
        {
            return !Equals(left, right);
        }
    }
}
