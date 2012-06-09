using System.IO;

namespace LibGit2Sharp
{
    internal class DetachedHead : Branch
    {
        internal DetachedHead(Repository repo, Reference reference, string sha)
            : base(repo, reference, _ => HeadName(repo.Info.Path, sha))
        {
        }

        protected override string Shorten(string branchName)
        {
            return branchName;
        }

        public static string HeadName(string path, string tipSha)
        {
            if (File.Exists(Path.Combine(path, "rebase-merge/head-name")))
            {
                return File.ReadAllText(Path.Combine(path, "rebase-merge/head-name")).Replace("refs/heads/", "");
            }

            return string.Format("({0}...)", tipSha.Substring(0, 7));
        }
    }
}
