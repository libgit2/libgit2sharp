using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// A filter is a way to execute code against a file as it moves to and from the git
    /// repository and into the working directory.
    /// </summary>
    public abstract class Filter : IEquatable<Filter>
    {
        private static readonly LambdaEqualityHelper<Filter> equalityHelper =
            new LambdaEqualityHelper<Filter>(x => x.Name, x => x.Attributes);
        // 64K is optimal buffer size per https://technet.microsoft.com/en-us/library/cc938632.aspx
        private const int BufferSize = 64 * 1024;

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter"/> class.
        /// And allocates the filter natively.
        /// <param name="name">The unique name with which this filtered is registered with</param>
        /// <param name="attributes">A list of attributes which this filter applies to</param>
        /// </summary>
        protected Filter(string name, IEnumerable<FilterAttributeEntry> attributes)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(attributes, "attributes");

            this.name = name;
            this.attributes = attributes;
            var attributesAsString = string.Join(",", this.attributes.Select(attr => attr.FilterDefinition));

            gitFilter = new GitFilter
            {
                attributes = EncodingMarshaler.FromManaged(Encoding.UTF8, attributesAsString),
                init = InitializeCallback,
                stream = StreamCreateCallback,
            };
        }
        /// <summary>
        /// Finalizer called by the <see cref="GC"/>, deregisters and frees native memory associated with the registered filter in libgit2.
        /// </summary>
        ~Filter()
        {
            GlobalSettings.DeregisterFilter(this);

#if LEAKS_IDENTIFYING
            int activeStreamCount = activeStreams.Count;
            if (activeStreamCount > 0)
            {
                Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} leaked {1} stream handles at finalization", GetType().Name, activeStreamCount));
            }
#endif
        }

        private readonly string name;
        private readonly IEnumerable<FilterAttributeEntry> attributes;
        private readonly GitFilter gitFilter;
        private readonly ConcurrentDictionary<IntPtr, StreamState> activeStreams = new ConcurrentDictionary<IntPtr, StreamState>();

        /// <summary>
        /// State bag used to keep necessary reference from being
        /// garbage collected during filter processing.
        /// </summary>
        private class StreamState
        {
            public GitWriteStream thisStream;
            public GitWriteStream nextStream;
            public IntPtr thisPtr;
            public IntPtr nextPtr;
            public FilterSource filterSource;
            public Stream output;
        }

        /// <summary>
        /// The name that this filter was registered with
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// The filter filterForAttributes.
        /// </summary>
        public IEnumerable<FilterAttributeEntry> Attributes
        {
            get { return attributes; }
        }

        /// <summary>
        /// The marshalled filter
        /// </summary>
        internal GitFilter GitFilter
        {
            get { return gitFilter; }
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
        protected virtual void Complete(string path, string root, Stream output)
        { }

        /// <summary>
        /// Initialize callback on filter
        ///
        /// Specified as `filter.initialize`, this is an optional callback invoked
        /// before a filter is first used.  It will be called once at most.
        ///
        /// If non-NULL, the filter's `initialize` callback will be invoked right
        /// before the first use of the filter, so you can defer expensive
        /// initialization operations (in case the library is being used in a way
        /// that doesn't need the filter.
        /// </summary>
        protected virtual void Initialize()
        { }

        /// <summary>
        /// Indicates that a filter is going to be applied for the given file for
        /// the given mode.
        /// </summary>
        /// <param name="path">The path of the file being filtered</param>
        /// <param name="root">The path of the working directory for the owning repository</param>
        /// <param name="mode">The filter mode</param>
        protected virtual void Create(string path, string root, FilterMode mode)
        { }

        /// <summary>
        /// Clean the input stream and write to the output stream.
        /// </summary>
        /// <param name="path">The path of the file being filtered</param>
        /// <param name="root">The path of the working directory for the owning repository</param>
        /// <param name="input">Input from the upstream filter or input reader</param>
        /// <param name="output">Output to the downstream filter or output writer</param>
        protected virtual void Clean(string path, string root, Stream input, Stream output)
        {
            input.CopyTo(output);
        }

        /// <summary>
        /// Smudge the input stream and write to the output stream.
        /// </summary>
        /// <param name="path">The path of the file being filtered</param>
        /// <param name="root">The path of the working directory for the owning repository</param>
        /// <param name="input">Input from the upstream filter or input reader</param>
        /// <param name="output">Output to the downstream filter or output writer</param>
        protected virtual void Smudge(string path, string root, Stream input, Stream output)
        {
            input.CopyTo(output);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="Filter"/>.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with the current <see cref="Filter"/>.</param>
        /// <returns>True if the specified <see cref="object"/> is equal to the current <see cref="Filter"/>; otherwise, false.</returns>
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
            return equalityHelper.GetHashCode(this);
        }

        /// <summary>
        /// Tests if two <see cref="Filter"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="Filter"/> to compare.</param>
        /// <param name="right">Second <see cref="Filter"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(Filter left, Filter right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="Filter"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="Filter"/> to compare.</param>
        /// <param name="right">Second <see cref="Filter"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(Filter left, Filter right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Initialize callback on filter
        ///
        /// Specified as `filter.initialize`, this is an optional callback invoked
        /// before a filter is first used.  It will be called once at most.
        ///
        /// If non-NULL, the filter's `initialize` callback will be invoked right
        /// before the first use of the filter, so you can defer expensive
        /// initialization operations (in case libgit2 is being used in a way that doesn't need the filter).
        /// </summary>
        int InitializeCallback(IntPtr filterPointer)
        {
            int result = 0;
            try
            {
                Initialize();
            }
            catch (Exception exception)
            {
                Log.Write(LogLevel.Error, "Filter.InitializeCallback exception");
                Log.Write(LogLevel.Error, exception.ToString());
                Proxy.git_error_set_str(GitErrorCategory.Filter, exception);
                result = (int)GitErrorCode.Error;
            }
            return result;
        }

        int StreamCreateCallback(out IntPtr git_writestream_out, GitFilter self, IntPtr payload, IntPtr filterSourcePtr, IntPtr git_writestream_next)
        {
            int result = 0;
            var state = new StreamState();

            try
            {
                Ensure.ArgumentNotZeroIntPtr(filterSourcePtr, "filterSourcePtr");
                Ensure.ArgumentNotZeroIntPtr(git_writestream_next, "git_writestream_next");

                state.thisStream = new GitWriteStream();
                state.thisStream.close = StreamCloseCallback;
                state.thisStream.write = StreamWriteCallback;
                state.thisStream.free = StreamFreeCallback;

                state.thisPtr = Marshal.AllocHGlobal(Marshal.SizeOf(state.thisStream));
                Marshal.StructureToPtr(state.thisStream, state.thisPtr, false);

                state.nextPtr = git_writestream_next;
                state.nextStream = Marshal.PtrToStructure<GitWriteStream>(state.nextPtr);

                state.filterSource = FilterSource.FromNativePtr(filterSourcePtr);
                state.output = new WriteStream(state.nextStream, state.nextPtr);

                Create(state.filterSource.Path, state.filterSource.Root, state.filterSource.SourceMode);

                if (!activeStreams.TryAdd(state.thisPtr, state))
                {
                    // AFAICT this is a theoretical error that could only happen if we manage
                    // to free the stream pointer but fail to remove the dictionary entry.
                    throw new InvalidOperationException("Overlapping stream pointers");
                }
            }
            catch (Exception exception)
            {
                // unexpected failures means memory clean up required
                if (state.thisPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(state.thisPtr);
                    state.thisPtr = IntPtr.Zero;
                }

                Log.Write(LogLevel.Error, "Filter.StreamCreateCallback exception");
                Log.Write(LogLevel.Error, exception.ToString());
                Proxy.git_error_set_str(GitErrorCategory.Filter, exception);
                result = (int)GitErrorCode.Error;
            }

            git_writestream_out = state.thisPtr;

            return result;
        }

        int StreamCloseCallback(IntPtr stream)
        {
            int result = 0;
            StreamState state;

            try
            {
                Ensure.ArgumentNotZeroIntPtr(stream, "stream");

                if (!activeStreams.TryGetValue(stream, out state))
                {
                    throw new ArgumentException("Unknown stream pointer", nameof(stream));
                }

                Ensure.ArgumentIsExpectedIntPtr(stream, state.thisPtr, "stream");

                using (BufferedStream outputBuffer = new BufferedStream(state.output, BufferSize))
                {
                    Complete(state.filterSource.Path, state.filterSource.Root, outputBuffer);
                }

                result = state.nextStream.close(state.nextPtr);
            }
            catch (Exception exception)
            {
                Log.Write(LogLevel.Error, "Filter.StreamCloseCallback exception");
                Log.Write(LogLevel.Error, exception.ToString());
                Proxy.git_error_set_str(GitErrorCategory.Filter, exception);
                result = (int)GitErrorCode.Error;
            }

            return result;
        }

        void StreamFreeCallback(IntPtr stream)
        {
            StreamState state;

            try
            {
                Ensure.ArgumentNotZeroIntPtr(stream, "stream");

                if (!activeStreams.TryRemove(stream, out state))
                {
                    throw new ArgumentException("Double free or invalid stream pointer", nameof(stream));
                }

                Ensure.ArgumentIsExpectedIntPtr(stream, state.thisPtr, "stream");

                Marshal.FreeHGlobal(state.thisPtr);
            }
            catch (Exception exception)
            {
                Log.Write(LogLevel.Error, "Filter.StreamFreeCallback exception");
                Log.Write(LogLevel.Error, exception.ToString());
            }
        }

        unsafe int StreamWriteCallback(IntPtr stream, IntPtr buffer, UIntPtr len)
        {
            int result = 0;
            StreamState state;

            try
            {
                Ensure.ArgumentNotZeroIntPtr(stream, "stream");
                Ensure.ArgumentNotZeroIntPtr(buffer, "buffer");

                if (!activeStreams.TryGetValue(stream, out state))
                {
                    throw new ArgumentException("Invalid or already freed stream pointer", nameof(stream));
                }

                Ensure.ArgumentIsExpectedIntPtr(stream, state.thisPtr, "stream");

                using (UnmanagedMemoryStream input = new UnmanagedMemoryStream((byte*)buffer.ToPointer(), (long)len))
                using (BufferedStream outputBuffer = new BufferedStream(state.output, BufferSize))
                {
                    switch (state.filterSource.SourceMode)
                    {
                        case FilterMode.Clean:
                            Clean(state.filterSource.Path, state.filterSource.Root, input, outputBuffer);
                            break;

                        case FilterMode.Smudge:
                            Smudge(state.filterSource.Path, state.filterSource.Root, input, outputBuffer);
                            break;

                        default:
                            Proxy.git_error_set_str(GitErrorCategory.Filter, "Unexpected filter mode.");
                            return (int)GitErrorCode.Ambiguous;
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Write(LogLevel.Error, "Filter.StreamWriteCallback exception");
                Log.Write(LogLevel.Error, exception.ToString());
                Proxy.git_error_set_str(GitErrorCategory.Filter, exception);
                result = (int)GitErrorCode.Error;
            }

            return result;
        }
    }
}
