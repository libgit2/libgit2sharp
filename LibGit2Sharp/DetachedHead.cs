using System.IO;

namespace LibGit2Sharp
{
    internal class DetachedHead : Branch
    {
        internal DetachedHead(Repository repo, Reference reference)
            : base(repo, reference, r => HeadName(repo, r))
        {
        }

        protected override string Shorten(string branchName)
        {
            return branchName;
        }

        public static string HeadName(Repository repo, Reference reference)
        {
            var rebaseMergeHeadName = Path.Combine(repo.Info.Path, "rebase-merge/head-name");
            if (File.Exists(rebaseMergeHeadName))
            {
                return File.ReadAllText(rebaseMergeHeadName).Replace("refs/heads/", "");
            }

            return string.Format("({0}...)", reference.TargetIdentifier.Substring(0, 7));
        }
    }
}
