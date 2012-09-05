using System;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Uniquely identifies a <see cref = "GitObject" />.
    /// </summary>
    public class ObjectId : IEquatable<ObjectId>
    {
        private readonly GitOid oid;
        private const int rawSize = 20;
        private readonly string sha;

        /// <summary>
        ///   Size of the string-based representation of a SHA-1.
        /// </summary>
        protected const int HexSize = rawSize * 2;

        private const string hexDigits = "0123456789abcdef";
        private static readonly byte[] reverseHexDigits = BuildReverseHexDigits();
        private static readonly Func<int, byte> byteConverter = i => reverseHexDigits[i - '0'];

        private static readonly LambdaEqualityHelper<ObjectId> equalityHelper =
            new LambdaEqualityHelper<ObjectId>(new Func<ObjectId, object>[] { x => x.Sha });

        /// <summary>
        ///   Zero ObjectId
        /// </summary>
        public static ObjectId Zero = new ObjectId(new string('0', HexSize));

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ObjectId" /> class.
        /// </summary>
        /// <param name = "oid">The oid.</param>
        internal ObjectId(GitOid oid)
        {
            this.oid = oid;
            sha = ToString(oid.Id);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ObjectId" /> class.
        /// </summary>
        /// <param name = "rawId">The byte array.</param>
        public ObjectId(byte[] rawId)
            : this(new GitOid { Id = rawId })
        {
            Ensure.ArgumentNotNull(rawId, "rawId");
            Ensure.ArgumentConformsTo(rawId, b => b.Length == rawSize, "rawId");
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ObjectId" /> class.
        /// </summary>
        /// <param name = "sha">The sha.</param>
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
        ///   Gets the raw id.
        /// </summary>
        public byte[] RawId
        {
            get { return oid.Id; }
        }

        /// <summary>
        ///   Gets the sha.
        /// </summary>
        public virtual string Sha
        {
            get { return sha; }
        }

        /// <summary>
        ///   Converts the specified string representation of a Sha-1 to its <see cref = "ObjectId" /> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name = "sha">A string containing a Sha-1 to convert. </param>
        /// <param name = "result">When this method returns, contains the <see cref = "ObjectId" /> value equivalent to the Sha-1 contained in <paramref name = "sha" />, if the conversion succeeded, or <code>null</code> if the conversion failed.</param>
        /// <returns>true if the <paramref name = "sha" /> parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParse(string sha, out ObjectId result)
        {
            result = BuildFrom(sha, false);

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

        private static ObjectId BuildFrom(string sha, bool shouldThrowIfInvalid)
        {
            GitOid? oid = BuildOidFrom(sha, shouldThrowIfInvalid);

            if (!oid.HasValue)
            {
                return null;
            }

            var objectId = new ObjectId(oid.Value);

            return objectId;
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "Object" /> is equal to the current <see cref = "ObjectId" />.
        /// </summary>
        /// <param name = "obj">The <see cref = "Object" /> to compare with the current <see cref = "ObjectId" />.</param>
        /// <returns>True if the specified <see cref = "Object" /> is equal to the current <see cref = "ObjectId" />; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as ObjectId);
        }

        /// <summary>
        ///   Determines whether the specified <see cref = "ObjectId" /> is equal to the current <see cref = "ObjectId" />.
        /// </summary>
        /// <param name = "other">The <see cref = "ObjectId" /> to compare with the current <see cref = "ObjectId" />.</param>
        /// <returns>True if the specified <see cref = "ObjectId" /> is equal to the current <see cref = "ObjectId" />; otherwise, false.</returns>
        public bool Equals(ObjectId other)
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
        ///   Returns the <see cref = "Sha" />, a <see cref = "String" /> representation of the current <see cref = "ObjectId" />.
        /// </summary>
        /// <returns>The <see cref = "Sha" /> that represents the current <see cref = "ObjectId" />.</returns>
        public override string ToString()
        {
            return Sha;
        }

        /// <summary>
        ///   Returns the <see cref = "Sha" />, a <see cref = "String" /> representation of the current <see cref = "ObjectId" />.
        /// </summary>
        /// <param name = "prefixLength">The number of chars the <see cref = "Sha" /> should be truncated to.</param>
        /// <returns>The <see cref = "Sha" /> that represents the current <see cref = "ObjectId" />.</returns>
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
        ///   Tests if two <see cref = "ObjectId" /> are equal.
        /// </summary>
        /// <param name = "left">First <see cref = "ObjectId" /> to compare.</param>
        /// <param name = "right">Second <see cref = "ObjectId" /> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(ObjectId left, ObjectId right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///   Tests if two <see cref = "ObjectId" /> are different.
        /// </summary>
        /// <param name = "left">First <see cref = "ObjectId" /> to compare.</param>
        /// <param name = "right">Second <see cref = "ObjectId" /> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(ObjectId left, ObjectId right)
        {
            return !Equals(left, right);
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

        private static string ToString(byte[] id)
        {
            if (id == null || id.Length != rawSize)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "A non null array of {0} bytes is expected.", rawSize), "id");
            }

            // Inspired from http://stackoverflow.com/questions/623104/c-byte-to-hex-string/3974535#3974535

            var c = new char[HexSize];

            for (int i = 0; i < HexSize; i++)
            {
                int index0 = i >> 1;
                var b = ((byte)(id[index0] >> 4));
                c[i++] = hexDigits[b];

                b = ((byte)(id[index0] & 0x0F));
                c[i] = hexDigits[b];
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

                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "'{0}' is not a valid object identifier. Its length should be {1}.", objectId, HexSize),
                    "objectId");
            }

            return objectId.All(c => hexDigits.Contains(c.ToString(CultureInfo.InvariantCulture)));
        }
    }
}
