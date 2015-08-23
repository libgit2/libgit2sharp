using System;
using System.Collections.Generic;
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
        }

        private readonly string name;
        private readonly IEnumerable<FilterAttributeEntry> attributes;
        private readonly GitFilter gitFilter;
        private readonly object @lock = new object();

        private GitWriteStream thisStream;
        private GitWriteStream nextStream;
        private IntPtr thisPtr;
        private IntPtr nextPtr;
        private FilterSource filterSource;
        private Stream output;

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
                Proxy.giterr_set_str(GitErrorCategory.Filter, exception);
                result = (int)GitErrorCode.Error;
            }
            return result;
        }

        int StreamCreateCallback(out IntPtr git_writestream_out, GitFilter self, IntPtr payload, IntPtr filterSourcePtr, IntPtr git_writestream_next)
        {
            int result = 0;

            try
            {
                Ensure.ArgumentNotZeroIntPtr(filterSourcePtr, "filterSourcePtr");
                Ensure.ArgumentNotZeroIntPtr(git_writestream_next, "git_writestream_next");

                thisStream = new GitWriteStream();
                thisStream.close = StreamCloseCallback;
                thisStream.write = StreamWriteCallback;
                thisStream.free = StreamFreeCallback;
                thisPtr = Marshal.AllocHGlobal(Marshal.SizeOf(thisStream));
                Marshal.StructureToPtr(thisStream, thisPtr, false);
                nextPtr = git_writestream_next;
                nextStream = new GitWriteStream();
                Marshal.PtrToStructure(nextPtr, nextStream);
                filterSource = FilterSource.FromNativePtr(filterSourcePtr);
                output = new WriteStream(nextStream, nextPtr);

                Create(filterSource.Path, filterSource.Root, filterSource.SourceMode);
            }
            catch (Exception exception)
            {
                // unexpected failures means memory clean up required
                if (thisPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(thisPtr);
                    thisPtr = IntPtr.Zero;
                }

                Log.Write(LogLevel.Error, "Filter.StreamCreateCallback exception");
                Log.Write(LogLevel.Error, exception.ToString());
                Proxy.giterr_set_str(GitErrorCategory.Filter, exception);
                result = (int)GitErrorCode.Error;
            }

            git_writestream_out = thisPtr;

            return result;
        }

        int StreamCloseCallback(IntPtr stream)
        {
            int result = 0;

            try
            {
                Ensure.ArgumentNotZeroIntPtr(stream, "stream");
                Ensure.ArgumentIsExpectedIntPtr(stream, thisPtr, "stream");

                using (BufferedStream outputBuffer = new BufferedStream(output, BufferSize))
                {
                    Complete(filterSource.Path, filterSource.Root, outputBuffer);
                }
            }
            catch (Exception exception)
            {
                Log.Write(LogLevel.Error, "Filter.StreamCloseCallback exception");
                Log.Write(LogLevel.Error, exception.ToString());
                Proxy.giterr_set_str(GitErrorCategory.Filter, exception);
                result = (int)GitErrorCode.Error;
            }

            result = nextStream.close(nextPtr);

            return result;
        }

        void StreamFreeCallback(IntPtr stream)
        {
            try
            {
                Ensure.ArgumentNotZeroIntPtr(stream, "stream");
                Ensure.ArgumentIsExpectedIntPtr(stream, thisPtr, "stream");

                Marshal.FreeHGlobal(thisPtr);
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

            try
            {
                Ensure.ArgumentNotZeroIntPtr(stream, "stream");
                Ensure.ArgumentNotZeroIntPtr(buffer, "buffer");
                Ensure.ArgumentIsExpectedIntPtr(stream, thisPtr, "stream");

                using (UnmanagedMemoryStream input = new UnmanagedMemoryStream((byte*)buffer.ToPointer(), (long)len))
                using (BufferedStream outputBuffer = new BufferedStream(output, BufferSize))
                {
                    switch (filterSource.SourceMode)
                    {
                        case FilterMode.Clean:
                            Clean(filterSource.Path, filterSource.Root, input, outputBuffer);
                            break;

                        case FilterMode.Smudge:
                            Smudge(filterSource.Path, filterSource.Root, input, outputBuffer);
                            break;

                        default:
                            Proxy.giterr_set_str(GitErrorCategory.Filter, "Unexpected filter mode.");
                            return (int)GitErrorCode.Ambiguous;
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Write(LogLevel.Error, "Filter.StreamWriteCallback exception");
                Log.Write(LogLevel.Error, exception.ToString());
                Proxy.giterr_set_str(GitErrorCategory.Filter, exception);
                result = (int)GitErrorCode.Error;
            }

            return result;
        }
    }
}
