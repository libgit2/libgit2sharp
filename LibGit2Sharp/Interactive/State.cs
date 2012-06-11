using System.IO;

namespace LibGit2Sharp.Interactive
{
    /// <summary>
    ///   Provides information about the repository's interactive state.
    /// </summary>
    public class State
    {
        private readonly Repository repo;
        private readonly string path;

        internal State(Repository repo)
        {
            this.repo = repo;
            path = repo.Info.Path;
        }

        /// <summary>
        ///   The name of HEAD when the pending operation began, or the current HEAD.
        /// </summary>
        public string HeadName
        {
            get
            {
                if (!repo.Info.IsHeadDetached)
                    return repo.Head.Name;

                if (Exists("rebase-merge/head-name"))
                    return File.ReadAllText(Path.Combine(path, "rebase-merge/head-name")).Replace("refs/heads/", "");

                var tip = repo.Head.Tip;
                var detachedName = tip == null ? "unknown" : tip.Sha.Substring(0, 7) + "...";
                return "(" + detachedName + ")";
            }
        }

        /// <summary>
        ///   The pending interactive operation.
        /// </summary>
        public Operation PendingOperation
        {
            get
            {
                if (!repo.Info.IsHeadDetached)
                    return Operation.None;

                if (DirectoryExists("rebase-merge"))
                    if (Exists("rebase-merge/interactive"))
                        return Operation.RebaseInteractive;
                    else
                        return Operation.Merge;

                if (DirectoryExists("rebase-apply"))
                    if (Exists("rebase-apply/rebasing"))
                        return Operation.Rebase;
                    else if (Exists("rebase-apply/applying"))
                        return Operation.ApplyMailbox;
                    else
                        return Operation.ApplyMailboxOrRebase;

                if (Exists("MERGE_HEAD"))
                    return Operation.Merge;

                if (Exists("CHERRY_PICK_HEAD"))
                    return Operation.CherryPick;

                if (Exists("BISECT_LOG"))
                    return Operation.Bisect;

                return Operation.None;
            }
        }

        private bool DirectoryExists(string relativePath)
        {
            return Directory.Exists(Path.Combine(path, relativePath));
        }

        private bool Exists(string relativePath)
        {
            return File.Exists(Path.Combine(path, relativePath));
        }
    }
}