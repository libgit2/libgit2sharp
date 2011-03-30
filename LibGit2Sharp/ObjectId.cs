using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    /// Uniquely identifies a <see cref="GitObject"/>.
    /// </summary>
    public class ObjectId
    {
        private readonly GitOid oid;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectId"/> class.
        /// </summary>
        /// <param name="oid">The oid.</param>
        public ObjectId(GitOid oid)
        {
            this.oid = oid;

            var hex = new byte[40];
            NativeMethods.git_oid_fmt(hex, ref this.oid);
            Sha = Encoding.UTF8.GetString(hex);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectId"/> class.
        /// </summary>
        /// <param name="sha">The sha.</param>
        public ObjectId(string sha)
        {
            oid = CreateFromSha(sha);
            Sha = sha;
        }

        internal GitOid Oid
        {
            get { return oid; }
        }

        /// <summary>
        /// Gets the raw id.
        /// </summary>
        public byte[] RawId
        {
            get { return oid.Id; }
        }

        /// <summary>
        /// Gets the sha.
        /// </summary>
        public string Sha { get; private set; }

        private static GitOid CreateFromSha(string sha)
        {
            GitOid oid;
            var result = NativeMethods.git_oid_mkstr(out oid, sha);
            Ensure.Success(result);
            return oid;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (ObjectId)) return false;
            return Equals((ObjectId) obj);
        }

        public bool Equals(ObjectId other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Sha, Sha);
        }

        public override int GetHashCode()
        {
            return (Sha != null ? Sha.GetHashCode() : 0);
        }

        public override string ToString()
        {
            return Sha;
        }

        public static bool operator ==(ObjectId left, ObjectId right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ObjectId left, ObjectId right)
        {
            return !Equals(left, right);
        }
    }
}