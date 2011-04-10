using System;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Uniquely identifies a <see cref="GitObject"/>.
    /// </summary>
    public class ObjectId : IEquatable<ObjectId>
    {
        private readonly GitOid oid;
        private const int rawSize = 20;
        private const int hexSize = rawSize * 2;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectId"/> class.
        /// </summary>
        /// <param name="oid">The oid.</param>
        internal ObjectId(GitOid oid)
        {
            this.oid = oid;
            Sha = Stringify(this.oid);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectId"/> class.
        /// </summary>
        /// <param name="rawId">The byte array.</param>
        public ObjectId(byte[] rawId)
        {
            Ensure.ArgumentNotNull(rawId, "rawId");
            Ensure.ArgumentConformsTo(rawId, b => b.Length == rawSize, "rawId");
            
            oid = new GitOid {Id = rawId};
            Sha = Stringify(oid);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectId"/> class.
        /// </summary>
        /// <param name="sha">The sha.</param>
        public ObjectId(string sha)
        {
            Ensure.ArgumentNotNullOrEmptyString(sha, "sha"); //TODO: To be pushed downward into CreateFromSha()
            Ensure.ArgumentConformsTo(sha, s => s.Length == hexSize, "sha");   //TODO: To be pushed downward into CreateFromSha()

            oid = CreateFromSha(sha, true).GetValueOrDefault();
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

        internal static ObjectId CreateFromMaybeSha(string sha)
        {
            GitOid? oid = CreateFromSha(sha, false);
         
            if (!oid.HasValue)
            {
                return null;
            }

            return new ObjectId(oid.Value);
        }

        private static GitOid? CreateFromSha(string sha, bool shouldThrow)
        {
            GitOid oid;
            var result = NativeMethods.git_oid_mkstr(out oid, sha);

            if (!shouldThrow && result != (int)GitErrorCode.GIT_SUCCESS)
            {
                return null;
            }

            Ensure.Success(result);
            return oid;
        }

        private static string Stringify(GitOid oid)
        {
            var hex = new byte[hexSize];
            NativeMethods.git_oid_fmt(hex, ref oid);
            return Encoding.UTF8.GetString(hex);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ObjectId);
        }

        public bool Equals(ObjectId other)
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

            return Equals(Sha, other.Sha);
        }

        public override int GetHashCode()
        {
            return Sha.GetHashCode();
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