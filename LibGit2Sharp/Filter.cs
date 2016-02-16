using System;
using System.IO;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// A filter is a way to execute code against a file as it moves to and from the git
    /// repository and into the working directory.
    /// </summary>
    public abstract class Filter : IEquatable<Filter>
    {
        // 64K is optimal buffer size per https://technet.microsoft.com/en-us/library/cc938632.aspx
        private const int BufferSize = 64 * 1024;

        private static readonly LambdaEqualityHelper<Filter> equalityHelper =
            new LambdaEqualityHelper<Filter>(x => x.filterSourcePtr);

        /// <summary>
        /// Releases any native memory assocated with the object upon finalization
        /// </summary>
        ~Filter()
        {
            lock(@lock)
            {
                if (thisStreamPtr != IntPtr.Zero)
                {
                    StreamFreeCallback(thisStreamPtr);
                }
            }
        }

        /// <summary>
        /// The verb, or attribute, associated with the filter.
        /// </summary>
        public string Verb { get; internal set; }

        internal event Action<Filter> Freed;

        internal IntPtr Key {  get { return self.attributes; } }

        private static readonly object @lock = new object();

        private GitFilter self;
        private GitWriteStream thisStream;
        private GitWriteStream nextStream;
        private IntPtr thisStreamPtr;
        private IntPtr nextStreamPtr;
        private FilterSource filterSource;
        private IntPtr filterSourcePtr;
        private Stream outputStream;

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="Filter"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="Filter"/>.</param>
        /// <returns>True if the specified <see cref="Object"/> is equal to the current <see cref="Filter"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Filter);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Filter"/> is equal to the current <see cref="Filter"/>.
        /// </summary>
        /// <param name="other">The <see cref="Filter"/> to compare with the current <see cref="Filter"/>.</param>
        /// <returns>True if the specified <see cref="Filter"/> is equal to the current <see cref="Filter"/>; otherwise, false.</returns>
        public bool Equals(Filter other)
        {
            return equalityHelper.Equals(this, other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return filterSourcePtr.GetHashCode();
        }

        /// <summary>
        /// Apply the filter to the the input stream and write to the output stream.
        /// </summary>
        /// <param name="path">The path of the file being filtered</param>
        /// <param name="root">The path of the working directory for the owning repository</param>
        /// <param name="input">Input from the upstream filter or input reader</param>
        /// <param name="output">Output to the downstream filter or output writer</param>
        /// <param name="mode">The mode indicating the direction of flow (to or from working tree)</param>
        /// <param name="verb">The verb indicated in the .gitattributes file.</param>
        protected virtual void Apply(string root, string path, Stream input, Stream output, FilterMode mode, string verb)
        {
            input.CopyTo(output);
        }

        /// <summary>
        /// Complete callback on filter
        /// 
        /// This optional callback will be invoked when the upstream filter is
        /// closed. Gives the filter a chance to perform any final actions or
        /// necissary clean up.
        /// </summary>
        /// <param name="path">The path of the file being filtered</param>
        /// <param name="root">The path of the working directory for the owning repository</param>
        /// <param name="output">Output to the downstream filter or output writer</param>
        /// <param name="mode">The mode indicating the direction of flow (to or from working tree)</param>
        /// <param name="verb">The verb indicated in the .gitattributes file.</param>
        protected virtual void Complete(string root, string path, Stream output, FilterMode mode, string verb)
        { }

        /// <summary>
        /// Indicates that a filter is going to be applied for the given file for
        /// the given mode.
        /// </summary>
        /// <param name="path">The path of the file being filtered</param>
        /// <param name="root">The path of the working directory for the owning repository</param>
        /// <param name="mode">The mode indicating the direction of flow (to or from working tree)</param>
        /// <param name="verb">The verb indicated in the .gitattributes file.</param>
        protected virtual void Create(string root, string path, FilterMode mode, string verb)
        { }

        internal int StreamCreateCallback(out IntPtr git_writestream_out, GitFilter self, IntPtr payload, IntPtr filterSourcePtr, IntPtr git_writestream_next, string verb)
        {
            int result = 0;

            try
            {
                Ensure.ArgumentNotZeroIntPtr(filterSourcePtr, "filterSourcePtr");
                Ensure.ArgumentNotZeroIntPtr(git_writestream_next, "git_writestream_next");
                Ensure.ArgumentNotNullOrEmptyString(verb, "verb");

                this.self = self;

                thisStream = new GitWriteStream();
                thisStream.close = StreamCloseCallback;
                thisStream.write = StreamWriteCallback;
                thisStream.free = StreamFreeCallback;

                thisStreamPtr = Marshal.AllocHGlobal(Marshal.SizeOf(thisStream));
                Marshal.StructureToPtr(thisStream, thisStreamPtr, false);

                nextStreamPtr = git_writestream_next;

                nextStream = new GitWriteStream();
                Marshal.PtrToStructure(nextStreamPtr, nextStream);

                this.filterSourcePtr = filterSourcePtr;
                filterSource = FilterSource.FromNativePtr(filterSourcePtr);

                outputStream = new WriteStream(nextStream, nextStreamPtr);

                Verb = verb;

                Create(filterSource.Root, filterSource.Path, filterSource.SourceMode, Verb);
            }
            catch (Exception exception)
            {
                // unexpected failures means memory clean up required
                if (thisStreamPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(thisStreamPtr);
                    thisStreamPtr = IntPtr.Zero;
                }

                Log.Write(LogLevel.Error, "Filter.StreamCreateCallback exception");
                Log.Write(LogLevel.Error, exception.ToString());

                Proxy.giterr_set_str(GitErrorCategory.Filter, exception);

                result = (int)GitErrorCode.Error;
            }

            git_writestream_out = thisStreamPtr;

            return result;
        }

        private void OnFreed()
        {
            var freed = Freed;
            if (freed != null)
            {
                freed(this);
            }
        }

        int StreamCloseCallback(IntPtr stream)
        {
            int result = 0;

            lock (@lock)
            {
                try
                {
                    Ensure.ArgumentNotZeroIntPtr(stream, "stream");
                    Ensure.ArgumentIsExpectedIntPtr(stream, thisStreamPtr, "stream");

                    using (BufferedStream outputBuffer = new BufferedStream(outputStream, BufferSize))
                    {
                        Complete(filterSource.Root, filterSource.Path, outputBuffer, filterSource.SourceMode, Verb);
                    }
                }
                catch (Exception exception)
                {
                    Log.Write(LogLevel.Error, "Filter.StreamCloseCallback exception");
                    Log.Write(LogLevel.Error, exception.ToString());

                    Proxy.giterr_set_str(GitErrorCategory.Filter, exception);

                    result = (int)GitErrorCode.Error;
                }

                result = nextStream.close(nextStreamPtr);
            }

            return result;
        }

        void StreamFreeCallback(IntPtr stream)
        {
            lock (@lock)
            {
                try
                {
                    Ensure.ArgumentNotZeroIntPtr(stream, "stream");
                    Ensure.ArgumentIsExpectedIntPtr(stream, thisStreamPtr, "stream");

                    Marshal.FreeHGlobal(thisStreamPtr);
                    thisStreamPtr = IntPtr.Zero;

                    OnFreed();
                }
                catch (Exception exception)
                {
                    Log.Write(LogLevel.Error, "Filter.StreamFreeCallback exception");
                    Log.Write(LogLevel.Error, exception.ToString());
                }
            }
        }

        unsafe int StreamWriteCallback(IntPtr stream, IntPtr buffer, UIntPtr len)
        {
            int result = 0;

            lock(@lock)
            {
                try
                {
                    Ensure.ArgumentNotZeroIntPtr(stream, "stream");
                    Ensure.ArgumentNotZeroIntPtr(buffer, "buffer");
                    Ensure.ArgumentIsExpectedIntPtr(stream, thisStreamPtr, "stream");

                    using (UnmanagedMemoryStream input = new UnmanagedMemoryStream((byte*)buffer.ToPointer(), (long)len))
                    using (BufferedStream outputBuffer = new BufferedStream(outputStream, BufferSize))
                    {
                        Apply(filterSource.Root, filterSource.Path, input, outputBuffer, filterSource.SourceMode, Verb);
                    }
                }
                catch (Exception exception)
                {
                    Log.Write(LogLevel.Error, "Filter.StreamWriteCallback exception");
                    Log.Write(LogLevel.Error, exception.ToString());
                    Proxy.giterr_set_str(GitErrorCategory.Filter, exception);
                    result = (int)GitErrorCode.Error;
                }
            }

            return result;
        }
    }
}
