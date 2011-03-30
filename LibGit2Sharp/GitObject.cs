using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A GitObject
    /// </summary>
    public class GitObject
    {
        public static GitObjectTypeMap TypeToTypeMap =
            new GitObjectTypeMap
                {
                    {typeof (Commit), GitObjectType.Commit},
                    {typeof (Tree), GitObjectType.Tree},
                    {typeof (Blob), GitObjectType.Blob},
                    {typeof (Tag), GitObjectType.Tag},
                    {typeof (GitObject), GitObjectType.Any},
                };

        protected GitObject(IntPtr obj, ObjectId id = null)
        {
            if (id == null)
            {
                var ptr = NativeMethods.git_object_id(obj);
                id = new ObjectId((GitOid) Marshal.PtrToStructure(ptr, typeof (GitOid)));
            }
            Id = id;
        }

        /// <summary>
        ///   Gets the id of this object
        /// </summary>
        public ObjectId Id { get; private set; }

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
                        return new Commit(obj, repo, id);
                    case GitObjectType.Tree:
                        return new Tree(obj, id);
                    case GitObjectType.Tag:
                        return new Tag(obj, id);
                    case GitObjectType.Blob:
                        return new Blob(obj, id);
                    default:
                        return new GitObject(obj, id);
                }
            }
            finally
            {
                NativeMethods.git_object_close(obj);
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (GitObject)) return false;
            return Equals((GitObject) obj);
        }

        public bool Equals(GitObject other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Id.Equals(Id);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}