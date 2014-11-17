using System;
using System.IO;
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
        protected FilterSource() {  }

        internal FilterSource(FilePath path, FilterMode mode)
        {
            SourceMode = mode;
            Path = GetPathSafely(path);
        }

        /// <summary>
        /// Take an unmanaged pointer and convert it to filter source callback paramater
        /// </summary>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public static FilterSource FromNativePtr(IntPtr ptr)
        {
            var source = ptr.MarshalAs<GitFilterSource>();
            FilePath path = LaxFilePathMarshaler.FromNative(source.path) ?? FilePath.Empty;
            FilterMode gitFilterSourceMode = Proxy.git_filter_source_mode(ptr);
            return new FilterSource(path, gitFilterSourceMode);
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
        /// When cleaning and smudging, relative and absolute paths are returned.
        /// Try and just return the relative path if we can. 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        string GetPathSafely(FilePath path)
        {
            try
            {
                var fileInfo = new FileInfo(path.Native);
                return fileInfo.Name;
            }
            catch (Exception)
            {
                return path.Native;;
            }
        }
    }
}