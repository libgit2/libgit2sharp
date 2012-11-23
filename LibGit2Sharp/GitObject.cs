using System;
using System.Diagnostics;
using System.Globalization;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A GitObject
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public abstract class GitObject : IEquatable<GitObject>
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
            new LambdaEqualityHelper<GitObject>(x => x.Id);

        /// <summary>
        ///   The <see cref = "Repository" /> containing the object. 
        /// </summary>
        protected readonly Repository repo;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected GitObject()
        { }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "GitObject" /> class.
        /// </summary>
        /// <param name = "repo">The <see cref = "Repository" /> containing the object.</param>
        /// <param name = "id">The <see cref = "ObjectId" /> it should be identified by.</param>
        protected GitObject(Repository repo, ObjectId id)
        {
            this.repo = repo;
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

        internal static GitObject BuildFrom(Repository repo, ObjectId id, GitObjectType type, FilePath path)
        {
            switch (type)
            {
                case GitObjectType.Commit:
                    return new Commit(repo, id);
                case GitObjectType.Tree:
                    return new Tree(repo, id, path);
                case GitObjectType.Tag:
                    return new TagAnnotation(repo, id);
                case GitObjectType.Blob:
                    return new Blob(repo, id);
                default:
                    throw new LibGit2SharpException(string.Format(CultureInfo.InvariantCulture, "Unexpected type '{0}' for object '{1}'.", type, id));
            }
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

        private string DebuggerDisplay
        {
            get { return Id.ToString(7); }
        }
    }
}
