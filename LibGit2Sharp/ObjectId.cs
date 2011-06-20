using System;
using System.Globalization;
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

        private static readonly LambdaEqualityHelper<ObjectId> equalityHelper =
            new LambdaEqualityHelper<ObjectId>(new Func<ObjectId, object>[] { x => x.Sha });

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
            : this(new GitOid { Id = rawId })
        {
            Ensure.ArgumentNotNull(rawId, "rawId");
            Ensure.ArgumentConformsTo(rawId, b => b.Length == rawSize, "rawId");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectId"/> class.
        /// </summary>
        /// <param name="sha">The sha.</param>
        public ObjectId(string sha)
        {
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
            Ensure.ArgumentNotNullOrEmptyString(sha, "sha");

            if (sha.Length != hexSize)
            {
                if (!shouldThrow)
                {
                    return null;
                }

                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "'{0}' is not a valid sha. Expected length should equal {1}.", sha, hexSize));
            }

            GitOid oid;
            var result = NativeMethods.git_oid_fromstr(out oid, sha);

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
            return Encoding.ASCII.GetString(hex);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="ObjectId"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="ObjectId"/>.</param>
        /// <returns>True if the specified <see cref="Object"/> is equal to the current <see cref="ObjectId"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ObjectId);
        }

        /// <summary>
        /// Determines whether the specified <see cref="ObjectId"/> is equal to the current <see cref="ObjectId"/>.
        /// </summary>
        /// <param name="other">The <see cref="ObjectId"/> to compare with the current <see cref="ObjectId"/>.</param>
        /// <returns>True if the specified <see cref="ObjectId"/> is equal to the current <see cref="ObjectId"/>; otherwise, false.</returns>
        public bool Equals(ObjectId other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        ///  Returns the <see cref="Sha"/>, a <see cref="String"/> representation of the current <see cref="ObjectId"/>.
        /// </summary>
        /// <returns>The <see cref="Sha"/> that represents the current <see cref="ObjectId"/>.</returns>
        public override string ToString()
        {
            return Sha;
        }

        /// <summary>
        /// Tests if two <see cref="ObjectId"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="ObjectId"/> to compare.</param>
        /// <param name="right">Second <see cref="ObjectId"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(ObjectId left, ObjectId right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="ObjectId"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="ObjectId"/> to compare.</param>
        /// <param name="right">Second <see cref="ObjectId"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(ObjectId left, ObjectId right)
        {
            return !Equals(left, right);
        }
    }
}