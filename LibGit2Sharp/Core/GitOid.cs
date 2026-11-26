namespace LibGit2Sharp.Core
{
    internal struct git_oid
    {
        public const int Size = 20;
        public unsafe fixed byte Id[20];
    }

    /// <summary>
    /// Represents a unique id in git which is the sha1 hash of this id's content.
    /// </summary>
    /// <remarks>
    /// The bytes are stored inline, so the struct is blittable: it is passed to and
    /// from libgit2 without being copied or allocating a managed array.
    /// </remarks>
    internal unsafe struct GitOid
    {
        /// <summary>
        /// Number of bytes in the Id.
        /// </summary>
        public const int Size = 20;

        /// <summary>
        /// The raw binary 20 byte Id.
        /// </summary>
        public fixed byte Id[Size];

        public static implicit operator ObjectId(GitOid oid)
        {
            return new ObjectId(oid);
        }

        public static implicit operator ObjectId(GitOid? oid)
        {
            return oid == null ? null : new ObjectId(oid.Value);
        }

        /// <summary>
        /// Static convenience property to return an id (all zeros).
        /// </summary>
        public static GitOid Empty
        {
            get { return new GitOid(); }
        }
    }
}
