using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A reference to a <see cref="Blob"/> known by the <see cref="Index"/>.
    /// </summary>
    public class IndexEntry
    {
        private Func<FileStatus> state;

        /// <summary>
        ///   State of the version of the <see cref="Blob"/> pointed at by this <see cref="IndexEntry"/>, 
        ///   compared against the <see cref="Blob"/> known from the <see cref="Repository.Head"/> and the file in the working directory.
        /// </summary>
        public FileStatus State
        {
            get { return state(); }
        }

        /// <summary>
        ///   Gets the relative path to the file within the working directory.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        ///   Gets the id of the <see cref="Blob"/> pointed at by this index entry.
        /// </summary>
        public ObjectId Id { get; private set; }

        internal static IndexEntry CreateFromPtr(Repository repo, IntPtr ptr)
        {
            var entry = (GitIndexEntry)Marshal.PtrToStructure(ptr, typeof(GitIndexEntry));
            return new IndexEntry
                       {
                           Path = entry.Path,
                           Id = new ObjectId(entry.oid),
                           state = () => repo.Index.RetrieveStatus(entry.Path)
                       };
        }
    }
}
