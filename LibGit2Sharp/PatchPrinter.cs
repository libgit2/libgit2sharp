using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    internal class PatchPrinter
    {
        private readonly IDictionary<string, TreeEntryChanges> filesChanges;
        private readonly StringBuilder fullPatchBuilder;
        private string currentFilePath;

        private static readonly Regex oldFilePathRegex = new Regex(@"\-\-\-\s*a/(.*)\n");
        private static readonly Regex newFilePathRegex = new Regex(@"\+\+\+\s*b/(.*)\n");

        internal PatchPrinter(IDictionary<string, TreeEntryChanges> filesChanges, StringBuilder fullPatchBuilder)
        {
            this.filesChanges = filesChanges;
            this.fullPatchBuilder = fullPatchBuilder;
        }

        internal int PrintCallBack(IntPtr data, GitDiffLineOrigin lineorigin, string formattedoutput)
        {
            switch (lineorigin)
            {
                case GitDiffLineOrigin.GIT_DIFF_LINE_FILE_HDR:
                    ExtractAndUpdateFilePath(formattedoutput);
                    break;
            }

            filesChanges[currentFilePath].PatchBuilder.Append(formattedoutput);
            fullPatchBuilder.Append(formattedoutput);

            return 0;
        }

        // We are walking around a bug in libgit2: when a file is deleted, the oldFilePath and the newFilePath are inverted (this has been recently fixed in one of the latest commit, see https://github.com/libgit2/libgit2/pull/643)
        private void ExtractAndUpdateFilePath(string formattedoutput)
        {
            var match = oldFilePathRegex.Match(formattedoutput);
            if (match.Success)
            {
                currentFilePath = match.Groups[1].Value;
            }

            match = newFilePathRegex.Match(formattedoutput);
            if (match.Success)
            {
                currentFilePath = match.Groups[1].Value;
            }
        }
    }
}