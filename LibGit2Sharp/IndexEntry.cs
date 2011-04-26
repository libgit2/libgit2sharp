using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class IndexEntry
    {
        public IndexEntryState State { get; set; }

        public string Path { get; private set; }

        internal static IndexEntry CreateFromPtr(IntPtr ptr)
        {
            var entry = (GitIndexEntry) Marshal.PtrToStructure(ptr, typeof (GitIndexEntry));
            return new IndexEntry {Path = entry.Path};
        }
    }
}