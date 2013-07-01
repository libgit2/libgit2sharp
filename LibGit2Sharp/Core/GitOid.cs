using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Represents a unique id in git which is the sha1 hash of this id's content.
    /// </summary>
    internal struct GitOid
    {
        /// <summary>
        /// The raw binary 20 byte Id.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] Id;

        public static implicit operator ObjectId(GitOid oid)
        {
            return new ObjectId(oid);
        }

        public static implicit operator ObjectId(GitOid? oid)
        {
            return oid == null ? null : new ObjectId(oid.Value);
        }
    }
}
