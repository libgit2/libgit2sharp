using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    internal class AbbreviatedObjectId : ObjectId
    {
        internal AbbreviatedObjectId(GitOid oid, int length) : base(oid)
        {
            Length = length;
        }

        public int Length { get; private set; }

        public override string Sha
        {
            get { return base.Sha.Substring(0, Length); }
        }
    }
}
