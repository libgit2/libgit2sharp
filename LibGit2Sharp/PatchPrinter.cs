using System;
using System.Collections.Generic;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    internal class PatchPrinter
    {
        private readonly IDictionary<string, TreeEntryChanges> filesChanges;
        private readonly StringBuilder fullPatchBuilder;
        private string currentFilePath;

        private static readonly Utf8Marshaler marshaler = (Utf8Marshaler)Utf8Marshaler.GetInstance(string.Empty);

        internal PatchPrinter(IDictionary<string, TreeEntryChanges> filesChanges, StringBuilder fullPatchBuilder)
        {
            this.filesChanges = filesChanges;
            this.fullPatchBuilder = fullPatchBuilder;
        }


        private static string NativeToString(IntPtr content, IntPtr contentlen)
        {
            return ((Utf8Marshaler)(Utf8Marshaler.GetInstance(string.Empty))).NativeToString(content, contentlen.ToInt32());
        }

        internal int PrintCallBack(IntPtr data, GitDiffDelta delta, GitDiffRange range, GitDiffLineOrigin lineorigin, IntPtr content, IntPtr contentlen)
        {
            string formattedoutput = NativeToString(content, contentlen);

            switch (lineorigin)
            {
                case GitDiffLineOrigin.GIT_DIFF_LINE_FILE_HDR:
                    currentFilePath = (string)marshaler.MarshalNativeToManaged(delta.NewFile.Path);
                    break;
            }

            filesChanges[currentFilePath].PatchBuilder.Append(formattedoutput);
            fullPatchBuilder.Append(formattedoutput);

            return 0;
        }
    }
}
