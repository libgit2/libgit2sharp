using System;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Uniquely identifies a <see cref="GitObject"/>.
    /// </summary>
    public sealed class ObjectId : IEquatable<ObjectId>
    {
        private readonly GitOid oid;
        private const int rawSize = GitOid.Size;
        private readonly string sha;

        /// <summary>
        /// Size of the string-based representation of a SHA-1.
        /// </summary>
        internal const int HexSize = rawSize * 2;

        private const string hexDigits = "0123456789abcdef";
        private static readonly byte[] reverseHexDigits = BuildReverseHexDigits();
        private static readonly Func<int, byte> byteConverter = i => reverseHexDigits[i - '0'];

        private static readonly LambdaEqualityHelper<ObjectId> equalityHelper =
            new LambdaEqualityHelper<ObjectId>(x => x.Sha);

        /// <summary>
        /// Zero ObjectId
        /// </summary>
        public static ObjectId Zero = new ObjectId(new string('0', HexSize));

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectId"/> class.
        /// </summary>
        /// <param name="oid">The oid.</param>
        internal ObjectId(GitOid oid)
        {
            if (oid.Id == null || oid.Id.Length != rawSize)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "A non null array of {0} bytes is expected.", rawSize), nameof(oid));
            }

            this.oid = oid;
            sha = ToString(oid.Id, oid.Id.Length * 2);
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

        internal static unsafe ObjectId BuildFromPtr(IntPtr ptr)
        {
            return BuildFromPtr((git_oid*) ptr.ToPointer());
        }

        internal static unsafe ObjectId BuildFromPtr(git_oid* id)
        {
            return id == null ? null : new ObjectId(id->Id);
        }

        internal unsafe ObjectId(byte* rawId)
        {
            byte[] id = new byte[GitOid.Size];

            fixed(byte* p = id)
            {
                for (int i = 0; i < rawSize; i++)
                {
                    p[i] = rawId[i];
                }
            }

            this.oid = new GitOid { Id = id };
            this.sha = ToString(oid.Id, oid.Id.Length * 2);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectId"/> class.
        /// </summary>
        /// <param name="sha">The sha.</param>
        public ObjectId(string sha)
        {
            GitOid? parsedOid = BuildOidFrom(sha, true);

            if (!parsedOid.HasValue)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "'{0}' is not a valid Sha-1.", sha));
            }

            oid = parsedOid.Value;
            this.sha = sha;
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
        public string Sha
        {
            get { return sha; }
        }

        /// <summary>
        /// Converts the specified string representation of a Sha-1 to its <see cref="ObjectId"/> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="sha">A string containing a Sha-1 to convert.</param>
        /// <param name="result">When this method returns, contains the <see cref="ObjectId"/> value equivalent to the Sha-1 contained in <paramref name="sha"/>, if the conversion succeeded, or <code>null</code> if the conversion failed.</param>
        /// <returns>true if the <paramref name="sha"/> parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParse(string sha, out ObjectId result)
        {
            result = BuildOidFrom(sha, false);

            return result != null;
        }

        private static GitOid? BuildOidFrom(string sha, bool shouldThrowIfInvalid)
        {
            if (!LooksValid(sha, shouldThrowIfInvalid))
            {
                return null;
            }

            return ToOid(sha);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="ObjectId"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="ObjectId"/>.</param>
        /// <returns>True if the specified <see cref="object"/> is equal to the current <see cref="ObjectId"/>; otherwise, false.</returns>
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
        /// Returns the <see cref="Sha"/>, a <see cref="string"/> representation of the current <see cref="ObjectId"/>.
        /// </summary>
        /// <returns>The <see cref="Sha"/> that represents the current <see cref="ObjectId"/>.</returns>
        public override string ToString()
        {
            return Sha;
        }

        /// <summary>
        /// Returns the <see cref="Sha"/>, a <see cref="string"/> representation of the current <see cref="ObjectId"/>.
        /// </summary>
        /// <param name="prefixLength">The number of chars the <see cref="Sha"/> should be truncated to.</param>
        /// <returns>The <see cref="Sha"/> that represents the current <see cref="ObjectId"/>.</returns>
        public string ToString(int prefixLength)
        {
            int normalizedLength = NormalizeLength(prefixLength);
            return Sha.Substring(0, Math.Min(Sha.Length, normalizedLength));
        }

        private static int NormalizeLength(int prefixLength)
        {
            if (prefixLength < 1)
            {
                return 1;
            }

            if (prefixLength > HexSize)
            {
                return HexSize;
            }

            return prefixLength;
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

        /// <summary>
        /// Create an <see cref="ObjectId"/> for the given <paramref name="sha"/>.
        /// </summary>
        /// <param name="sha">The object SHA.</param>
        /// <returns>An <see cref="ObjectId"/>, or null if <paramref name="sha"/> is null.</returns>
        public static explicit operator ObjectId(string sha)
        {
            return sha == null ? null : new ObjectId(sha);
        }

        private static byte[] BuildReverseHexDigits()
        {
            var bytes = new byte['f' - '0' + 1];

            for (int i = 0; i < 10; i++)
            {
                bytes[i] = (byte)i;
            }

            for (int i = 10; i < 16; i++)
            {
                bytes[i + 'a' - '0' - 0x0a] = (byte)(i);
            }

            return bytes;
        }

        internal static string ToString(byte[] id, int lengthInNibbles)
        {
            // Inspired from http://stackoverflow.com/questions/623104/c-byte-to-hex-string/3974535#3974535

            var c = new char[lengthInNibbles];

            for (int i = 0; i < (lengthInNibbles & -2); i++)
            {
                int index0 = i >> 1;
                var b = ((byte)(id[index0] >> 4));
                c[i++] = hexDigits[b];

                b = ((byte)(id[index0] & 0x0F));
                c[i] = hexDigits[b];
            }

            if ((lengthInNibbles & 1) == 1)
            {
                int index0 = lengthInNibbles >> 1;
                var b = ((byte)(id[index0] >> 4));
                c[lengthInNibbles - 1] = hexDigits[b];
            }

            return new string(c);
        }

        private static GitOid ToOid(string sha)
        {
            var bytes = new byte[rawSize];

            if ((sha.Length & 1) == 1)
            {
                sha += "0";
            }

            for (int i = 0; i < sha.Length; i++)
            {
                int c1 = byteConverter(sha[i++]) << 4;
                int c2 = byteConverter(sha[i]);

                bytes[i >> 1] = (byte)(c1 + c2);
            }

            var oid = new GitOid { Id = bytes };
            return oid;
        }

        private static bool LooksValid(string objectId, bool throwIfInvalid)
        {
            if (string.IsNullOrEmpty(objectId))
            {
                if (!throwIfInvalid)
                {
                    return false;
                }

                Ensure.ArgumentNotNullOrEmptyString(objectId, "objectId");
            }

            if ((objectId.Length != HexSize))
            {
                if (!throwIfInvalid)
                {
                    return false;
                }

                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                                                          "'{0}' is not a valid object identifier. Its length should be {1}.",
                                                          objectId,
                                                          HexSize),
                                            nameof(objectId));
            }

            return objectId.All(c => hexDigits.IndexOf(c) >= 0);
        }

        /// <summary>
        /// Determine whether <paramref name="shortSha"/> matches the hexified
        /// representation of the first nibbles of this instance.
        /// <para>
        ///   Comparison is made in a case insensitive-manner.
        /// </para>
        /// </summary>
        /// <returns>True if this instance starts with <paramref name="shortSha"/>,
        /// false otherwise.</returns>
        public bool StartsWith(string shortSha)
        {
            Ensure.ArgumentNotNullOrEmptyString(shortSha, "shortSha");

            return Sha.StartsWith(shortSha, StringComparison.OrdinalIgnoreCase);
        }
    }
}
