using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

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
                    {typeof (Commit), GitObjectType.Commit},
                    {typeof (Tree), GitObjectType.Tree},
                    {typeof (Blob), GitObjectType.Blob},
                    {typeof (TagAnnotation), GitObjectType.Tag},
                    {typeof (GitObject), GitObjectType.Any},
                };

        protected GitObject(ObjectId id)
        {
            Id = id;
        }

        /// <summary>
        ///   Gets the id of this object
        /// </summary>
        public ObjectId Id { get; protected set; }

        /// <summary>
        ///   Gets the 40 character sha1 of this object.
        /// </summary>
        public string Sha
        {
            get { return Id.Sha; }
        }

        internal static GitObject CreateFromPtr(IntPtr obj, ObjectId id, Repository repo)
        {
            try
            {
                var type = NativeMethods.git_object_type(obj);
                switch (type)
                {
                    case GitObjectType.Commit:
                        return Commit.BuildFromPtr(obj, id, repo);
                    case GitObjectType.Tree:
                        return Tree.BuildFromPtr(obj, id, repo);
                    case GitObjectType.Tag:
                        return TagAnnotation.BuildFromPtr(obj, id);
                    case GitObjectType.Blob:
                        return Blob.BuildFromPtr(obj, id, repo);
                    default:
                        return new GitObject(id);
                }
            }
            finally
            {
                NativeMethods.git_object_close(obj);
            }
        }

        internal static ObjectId RetrieveObjectIfOf(IntPtr obj)
        {
            var ptr = NativeMethods.git_object_id(obj);
            return new ObjectId((GitOid)Marshal.PtrToStructure(ptr, typeof(GitOid)));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as GitObject);
        }

        public bool Equals(GitObject other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (GetType() != other.GetType())
            {
                return false;
            }

            return Equals(Id, other.Id);
        }

        public override int GetHashCode()
        {
            int hashCode = GetType().GetHashCode();

            unchecked
            {
                hashCode = (hashCode * 397) ^ Id.GetHashCode();
            }

            return hashCode;
        }

        public static bool operator ==(GitObject left, GitObject right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(GitObject left, GitObject right)
        {
            return !Equals(left, right);
        }
    }
}