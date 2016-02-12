using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// A filter source - describes the direction of filtering and the file being filtered.
    /// </summary>
    public class FilterSource
    {
        /// <summary>
        /// Needed for mocking purposes
        /// </summary>
        protected FilterSource() { }

        internal FilterSource(FilePath path, FilterMode mode, GitFilterSource source)
        {
            SourceMode = mode;
            ObjectId = new ObjectId(source.oid);
            Path = path.Native;
            Root = Proxy.git_repository_workdir(source.repository).Native;
        }

        /// <summary>
        /// Take an unmanaged pointer and convert it to filter source callback paramater
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        internal static FilterSource FromNativePtr(IntPtr ptr)
        {
            var source = ptr.MarshalAs<GitFilterSource>();
            FilePath path = LaxFilePathMarshaler.FromNative(source.path) ?? FilePath.Empty;
            FilterMode gitFilterSourceMode = Proxy.git_filter_source_mode(ptr);
            return new FilterSource(path, gitFilterSourceMode, source);
        }

        /// <summary>
        /// The filter mode for current file being filtered
        /// </summary>
        public virtual FilterMode SourceMode { get; private set; }

        /// <summary>
        /// The relative path to the file
        /// </summary>
        public virtual string Path { get; private set; }

        /// <summary>
        /// The blob id
        /// </summary>
        public virtual ObjectId ObjectId { get; private set; }

        /// <summary>
        /// The working directory
        /// </summary>
        public virtual string Root { get; private set; }
    }
}
