using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Represents a unique id in git which is the sha1 hash of this id's content.
    /// </summary>
    public struct GitOid
    {
        /// <summary>
        ///   The raw binary 20 byte Id.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)] public byte[] Id;

        public bool Equals(GitOid other)
        {
            return NativeMethods.git_oid_cmp(ref this, ref other) == 0;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof (GitOid)) return false;
            return Equals((GitOid) obj);
        }

        /// <summary>
        ///   Create a new <see cref = "GitOid" /> from a sha1.
        /// </summary>
        /// <param name = "sha">The sha.</param>
        /// <returns></returns>
        public static GitOid FromSha(string sha)
        {
            GitOid oid;
            var result = NativeMethods.git_oid_mkstr(out oid, sha);
            Ensure.Success(result);
            return oid;
        }

        public override int GetHashCode()
        {
            return (Id != null ? Id.Sum(i => i) : 0);
        }

        /// <summary>
        ///   Convert to 40 character sha1 representation
        /// </summary>
        /// <returns></returns>
        public string ToSha()
        {
            var hex = new byte[40];
            NativeMethods.git_oid_fmt(hex, ref this);
            return Encoding.UTF8.GetString(hex);
        }

        public override string ToString()
        {
            return ToSha();
        }

        public static bool operator ==(GitOid a, GitOid b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(GitOid a, GitOid b)
        {
            return !(a == b);
        }
    }
}