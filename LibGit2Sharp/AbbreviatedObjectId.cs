using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    internal class AbbreviatedObjectId : ObjectId
    {
        internal AbbreviatedObjectId(GitOid oid, int length) : base(oid)
        {
            if (length < MinHexSize || length > HexSize)
            {
                throw new ArgumentException(string.Format("Expected length should be comprised between {0} and {1}.", MinHexSize, HexSize), "length");
            }

            Length = length;
        }

        public int Length { get; private set; }

        public override string Sha
        {
            get { return base.Sha.Substring(0, Length); }
        }
    }
}
