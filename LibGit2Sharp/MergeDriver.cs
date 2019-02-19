using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using System;
using System.IO;

namespace LibGit2Sharp
{
    /// <summary>
    /// The result produced when applying a merge driver to conflicting commits
    /// </summary>
    public class MergeDriverResult
    {
        /// <summary>
        /// The status of what happened as a result of a merge.
        /// </summary>
        public MergeStatus Status;

        /// <summary>
        /// The resulting stream of data of the merge.
        /// <para>This will return <code>null</code> if the merge has been unsuccessful due to non-mergeable conflicts.</para>
        /// <para>The returned stream will be freed automatically.</para>
        /// </summary>
        public Stream Content;

        /// <summary>
        /// Specific mode of the resolved file. If null, an appropriate default mode is chosen.
        /// </summary>
        public Mode? ResolvedMode;

        /// <summary>
        /// Path to select for the resolved file
        /// </summary>
        public enum Path
        {
            /// <summary>
            /// No path
            /// </summary>
            None,
            /// <summary>
            /// Use ancestor path
            /// </summary>
            Ancestor,
            /// <summary>
            /// Use "ours" path
            /// </summary>
            Ours,
            /// <summary>
            /// Use "theirs" path
            /// </summary>
            Theirs
        };

        /// <summary>
        /// Specific path to use for the resolved file. If null, an appropriate default path is chosen.
        /// </summary>
        public Path? ResolvedPath;
    }

    /// <summary>
    /// Base class for a custom merge driver implementation
    /// </summary>
    public abstract class MergeDriver : IEquatable<MergeDriver>
    {
        private static readonly LambdaEqualityHelper<MergeDriver> equalityHelper =
            new LambdaEqualityHelper<MergeDriver>(x => x.Name);

        // 64K is optimal buffer size per https://technet.microsoft.com/en-us/library/cc938632.aspx
        private const int BufferSize = 64 * 1024;

        /// <summary>
        /// Initializes a new instance of the <see cref="MergeDriver"/> class.
        /// And allocates the merge driver natively.
        /// <param name="name">The unique name with which this merge driver is registered with</param>
        /// </summary>
        protected MergeDriver(string name)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");

            Name = name;
            GitMergeDriver = new GitMergeDriver
            {
                initialize = InitializeCallback,
                apply = ApplyMergeCallback
            };
        }

        /// <summary>
        /// Finalizer called by the <see cref="GC"/>, deregisters and frees native memory associated with the registered merge driver in libgit2.
        /// </summary>
        ~MergeDriver()
        {
            GlobalSettings.DeregisterMergeDriver(this);
        }

        /// <summary>
        /// Initialize callback on merge driver
        ///
        /// Specified as `driver.initialize`, this is an optional callback invoked
        /// before a merge driver is first used.  It will be called once at most
        /// per library lifetime.
        ///
        /// If non-NULL, the merge driver's `initialize` callback will be invoked
        /// right before the first use of the driver, so you can defer expensive
        /// initialization operations (in case libgit2 is being used in a way that
        /// doesn't need the merge driver).
        /// </summary>
        protected abstract void Initialize();

        /// <summary>
        /// Callback to perform the merge.
        ///
        /// Specified as `driver.apply`, this is the callback that actually does the
        /// merge.  If it can successfully perform a merge, it should populate
        /// `path_out` with a pointer to the filename to accept, `mode_out` with
        /// the resultant mode, and `merged_out` with the buffer of the merged file
        /// and then return 0.  If the driver returns `GIT_PASSTHROUGH`, then the
        /// default merge driver should instead be run.  It can also return
        /// `GIT_EMERGECONFLICT` if the driver is not able to produce a merge result,
        /// and the file will remain conflicted.  Any other errors will fail and
        /// return to the caller.
        ///
        /// The `driver_name` contains the name of the merge driver that was invoked, as
        /// specified by the file's attributes.
        ///
        /// The `src` contains the data about the file to be merged.
        /// </summary>
        protected abstract MergeDriverResult Apply(MergeDriverSource source);

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="MergeDriver"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="MergeDriver"/>.</param>
        /// <returns>True if the specified <see cref="Object"/> is equal to the current <see cref="MergeDriver"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as MergeDriver);
        }

        /// <summary>
        /// Determines whether the specified <see cref="MergeDriver"/> is equal to the current <see cref="MergeDriver"/>.
        /// </summary>
        /// <param name="other">The <see cref="MergeDriver"/> to compare with the current <see cref="MergeDriver"/>.</param>
        /// <returns>True if the specified <see cref="MergeDriver"/> is equal to the current <see cref="MergeDriver"/>; otherwise, false.</returns>
        public bool Equals(MergeDriver other)
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
        /// Tests if two <see cref="MergeDriver"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="MergeDriver"/> to compare.</param>
        /// <param name="right">Second <see cref="MergeDriver"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(MergeDriver left, MergeDriver right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="MergeDriver"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="MergeDriver"/> to compare.</param>
        /// <param name="right">Second <see cref="MergeDriver"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(MergeDriver left, MergeDriver right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Initialize callback on merge
        ///
        /// Specified as `driver.initialize`, this is an optional callback invoked
        /// before a merge driver is first used.  It will be called once at most.
        ///
        /// If non-NULL, the merge driver's `initialize` callback will be invoked right
        /// before the first use of the merge driver, so you can defer expensive
        /// initialization operations (in case libgit2 is being used in a way that doesn't need the merge driver).
        /// </summary>
        int InitializeCallback(IntPtr mergeDriverPointer)
        {
            int result = 0;
            try
            {
                Initialize();
            }
            catch (Exception exception)
            {
                Log.Write(LogLevel.Error, "MergeDriver.InitializeCallback exception");
                Log.Write(LogLevel.Error, exception.ToString());
                NativeMethods.git_error_set_str(GitErrorCategory.Merge, exception.ToString());
                result = (int)GitErrorCode.Error;
            }
            return result;
        }

        unsafe int ApplyMergeCallback(IntPtr merge_driver, IntPtr path_out, UIntPtr mode_out, IntPtr merged_out, IntPtr driver_name, IntPtr merge_driver_source)
        {
            try
            {
                using (var mergeDriverSource = MergeDriverSource.FromNativePtr(merge_driver_source))
                {
                    var result = Apply(mergeDriverSource);

                    if (result.Status == MergeStatus.Conflicts)
                    {
                        merged_out = IntPtr.Zero;
                        return (int)GitErrorCode.MergeConflict;
                    }

                    var len = result.Content.Length;
                    Proxy.git_buf_grow(merged_out, (uint)len);
                    var buffer = (git_buf*)merged_out.ToPointer();
                    using (var unsafeStream = new UnmanagedMemoryStream((byte*)buffer->ptr.ToPointer(), len, len, FileAccess.Write))
                        result.Content.CopyTo(unsafeStream);
                    buffer->size = (UIntPtr)len;
                    result.Content.SafeDispose();
                    result.Content = null;

                    // Decide which source to use for path_out
                    var driver_source = (git_merge_driver_source*)merge_driver_source.ToPointer();
                    var ancestorPath = mergeDriverSource.Ancestor != null ? mergeDriverSource.Ancestor.Path : null;
                    var oursPath = mergeDriverSource.Ours != null ? mergeDriverSource.Ours.Path : null;
                    var theirsPath = mergeDriverSource.Theirs != null ? mergeDriverSource.Theirs.Path : null;
                    var best = SelectPath(result.ResolvedPath, ancestorPath, oursPath, theirsPath);

                    // Since there is no memory management of the returned character array 'path_out',
                    // we can only set it to one of the incoming argument strings
                    if (best == null)
                        *(char**)path_out.ToPointer() = null;
                    if (best == ancestorPath)
                        *(char**)path_out.ToPointer() = driver_source->ancestor->path;
                    else if (best == oursPath)
                        *(char**)path_out.ToPointer() = driver_source->ours->path;
                    else if (best == theirsPath)
                        *(char**)path_out.ToPointer() = driver_source->theirs->path;

                    // Decide which source to use for mode_out
                    Mode resolvedMode;
                    if (result.ResolvedMode == null)
                    {
                        var ancestorMode = mergeDriverSource.Ancestor != null ? mergeDriverSource.Ancestor.Mode : Mode.Nonexistent;
                        var oursMode = mergeDriverSource.Ours != null ? mergeDriverSource.Ours.Mode : Mode.Nonexistent;
                        var theirsMode = mergeDriverSource.Theirs != null ? mergeDriverSource.Theirs.Mode : Mode.Nonexistent;
                        resolvedMode = BestMode(ancestorMode, oursMode, theirsMode);
                    }
                    else
                        resolvedMode = result.ResolvedMode.Value;
                    *(uint*)mode_out.ToPointer() = (uint)resolvedMode;
                }
                return 0;
            }
            catch (Exception)
            {
                merged_out = IntPtr.Zero;
                return (int)GitErrorCode.Invalid;
            }
        }

        private string SelectPath(MergeDriverResult.Path? preferredPath, string ancestor, string ours, string theirs)
        {
            switch (preferredPath)
            {
                case MergeDriverResult.Path.None:
                    return null;
                case MergeDriverResult.Path.Ancestor:
                    return ancestor;
                case MergeDriverResult.Path.Ours:
                    return ours;
                case MergeDriverResult.Path.Theirs:
                    return theirs;
            }

            if (ancestor == null)
            {
                if (ours != null && theirs != null && ours == theirs)
                    return ours;
                return null;
            }

            if (ours != null && ancestor == ours)
                return theirs;
            if (theirs != null && ancestor == theirs)
                return ours;

            return null;
        }

        private Mode BestMode(Mode ancestor, Mode ours, Mode theirs)
        {
            if (ancestor == Mode.Nonexistent)
            {
                if (ours == Mode.ExecutableFile ||
                    theirs == Mode.ExecutableFile)
                    return Mode.ExecutableFile;

                return Mode.NonExecutableFile;
            }
            else if (ours != Mode.Nonexistent && theirs != Mode.Nonexistent)
            {
                if (ancestor == ours)
                    return theirs;
                return ours;
            }

            return Mode.Nonexistent;
        }

        /// <summary>
        /// The marshalled merge driver
        /// </summary>
        internal GitMergeDriver GitMergeDriver { get; private set; }

        /// <summary>
        /// The name that this merge driver was registered with
        /// </summary>
        public string Name { get; private set; }
    }
}
