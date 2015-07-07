using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// An object representing the registration of a Filter type with libgit2
    /// </summary>
    public abstract class FilterRegistration : IEquatable<FilterRegistration>
    {
        private static readonly LambdaEqualityHelper<FilterRegistration> equalityHelper =
            new LambdaEqualityHelper<FilterRegistration>(x => x.Name);

        /// <summary>
        /// The default priority of a filter callback in libgit2
        /// </summary>
        public const int DefaultFilterPriority = 200;

        internal FilterRegistration(string name, string attribute, int priority)
        {
            System.Diagnostics.Debug.Assert(!String.IsNullOrWhiteSpace(name));
            System.Diagnostics.Debug.Assert(attribute != null);

            Attribute = attribute;
            Name = name;
            Priority = priority;
        }

        /// <summary>
        /// <para>List of attribute names to check for this filter (e.g. "eol crlf text"). </para>
        /// <para>If the attribute name is bare, it will be simply loaded and passed to the `check` callback.</para>
        /// <para>If it has a value (i.e. "name=value"), the attribute must match that value for the filter to be applied.</para>
        /// </summary>
        public readonly string Attribute;
        /// <summary>
        /// Gets if the registration and underlying filter are valid.
        /// </summary>
        public abstract bool IsValid { get; }
        /// <summary>
        /// The name of the filter in the registry
        /// </summary>
        public readonly string Name;
        /// <summary>
        /// The priority of the registered filter
        /// </summary>
        public readonly int Priority;

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="FilterRegistration"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="FilterRegistration"/>.</param>
        /// <returns>True if the specified <see cref="Object"/> is equal to the current <see cref="FilterRegistration"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as FilterRegistration);
        }

        /// <summary>
        /// Determines whether the specified <see cref="FilterRegistration"/> is equal to the current <see cref="FilterRegistration"/>.
        /// </summary>
        /// <param name="other">The <see cref="FilterRegistration"/> to compare with the current <see cref="FilterRegistration"/>.</param>
        /// <returns>True if the specified <see cref="FilterRegistration"/> is equal to the current <see cref="FilterRegistration"/>; otherwise, false.</returns>
        public bool Equals(FilterRegistration other)
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
        /// Tests if two <see cref="FilterRegistration"/> are equal.
        /// </summary>
        /// <param name="left">First <see cref="FilterRegistration"/> to compare.</param>
        /// <param name="right">Second <see cref="FilterRegistration"/> to compare.</param>
        /// <returns>True if the two objects are equal; false otherwise.</returns>
        public static bool operator ==(FilterRegistration left, FilterRegistration right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests if two <see cref="FilterRegistration"/> are different.
        /// </summary>
        /// <param name="left">First <see cref="FilterRegistration"/> to compare.</param>
        /// <param name="right">Second <see cref="FilterRegistration"/> to compare.</param>
        /// <returns>True if the two objects are different; false otherwise.</returns>
        public static bool operator !=(FilterRegistration left, FilterRegistration right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// A filter specific object representing the registration of a Filter type with libgit2
    /// </summary>
    public sealed class FilterRegistration<TFilter> : FilterRegistration
        where TFilter : Filter, new()
    {
        /// <summary>
        /// The catch-all value for filters to match any-and-all filter values starting with "filter=" in /.gitattributes.
        /// </summary>
        public const string WildCardFilterAttribute = "*";

        public readonly char[] InvalidAttributeCharacters = { ',', ';' };
        private const string FilterAttributePrefix = "filter=";

        internal FilterRegistration(string name, string attribute, int priority, FilterRegistrationIntializationCallback initializationCallback)
            : base(name, attribute, priority)
        {
            filters = new HashSet<Filter>();
            verbs = new Dictionary<IntPtr, string>();

            if (attribute.IndexOfAny(InvalidAttributeCharacters) > -1)
            {
                throw new LibGit2SharpException("Invalid characters in attribute.");
            }

            if (!attribute.StartsWith(FilterAttributePrefix, StringComparison.CurrentCultureIgnoreCase))
            {
                attribute = FilterAttributePrefix + attribute;
            }

            //attribute += ',';

            gitFilter = new GitFilter
            {
                attributes = StrictUtf8Marshaler.FromManaged(attribute),
                check = StreamCheckCallback,
                init = InitializeCallback,
                stream = StreamCreateCallback,
            };

            // marshal the git_filter strucutre into native memory
            filterPointer = Marshal.AllocHGlobal(Marshal.SizeOf(gitFilter));
            Marshal.StructureToPtr(gitFilter, filterPointer, false);

            // register the filter with the native libary
            Proxy.git_filter_register(name, filterPointer, priority);

            _initialization = initializationCallback;
        }

        /// <summary>
        /// Finalizer called by the <see cref="GC"/>, deregisters and frees native memory associated with the registered filter in libgit2.
        /// </summary>
        ~FilterRegistration()
        {
            // clean up native allocations
            Free();
        }

        /// <summary>
        /// Gets if the registration and underlying filter are valid.
        /// </summary>
        public override bool IsValid { get { return !freed; } }

        private readonly object @lock = new object();

        private readonly IntPtr filterPointer;
        private readonly GitFilter gitFilter;
        private readonly HashSet<Filter> filters;
        private readonly Dictionary<IntPtr, string> verbs;

        private volatile bool freed;

        private readonly FilterRegistrationIntializationCallback _initialization;

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="FilterRegistration"/>.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with the current <see cref="FilterRegistration"/>.</param>
        /// <returns>True if the specified <see cref="Object"/> is equal to the current <see cref="FilterRegistration"/>; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as FilterRegistration<TFilter>);
        }

        /// <summary>
        /// Determines whether the specified <see cref="FilterRegistration"/> is equal to the current <see cref="FilterRegistration"/>.
        /// </summary>
        /// <param name="other">The <see cref="FilterRegistration"/> to compare with the current <see cref="FilterRegistration"/>.</param>
        /// <returns>True if the specified <see cref="FilterRegistration"/> is equal to the current <see cref="FilterRegistration"/>; otherwise, false.</returns>
        public bool Equals(FilterRegistration<TFilter> other)
        {
            return base.Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal void Free()
        {
            lock (@lock)
            {
                if (!freed)
                {
                    try
                    {
                        // unregister the filter with the native libary
                        Proxy.git_filter_unregister(Name);
                        // release native memory
                        Marshal.FreeHGlobal(filterPointer);
                        // release all active filter handles
                        foreach (var filter in filters)
                        {
                            filter.Freed -= Filter_Freed;
                        }
                        filters.Clear();
                    }
                    finally
                    {
                        // remember to not do this twice
                        freed = true;
                    }
                }
            }
        }

        /// <summary>
        /// Initialize callback on filter
        ///
        /// Specified as `filter.initialize`, this is an optional callback invoked
        /// before a filter is first used.  It will be called once at most per filter type.
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
                if (_initialization != null)
                {
                    _initialization();
                }
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
            Debug.Assert(filterSourcePtr != IntPtr.Zero, "The `filterSourcePtr` parameter is null.");
            Debug.Assert(git_writestream_next != IntPtr.Zero, "The `git_writestream_next` parameter is null.");
            Debug.Assert(verbs.ContainsKey(self.attributes), "The `self.attributes` parameter is unknown to the `verbs` lookup.");

            try
            {
                string verb = verbs[self.attributes];

                TFilter filter = new TFilter();
                int result = filter.StreamCreateCallback(out git_writestream_out, self, payload, filterSourcePtr, git_writestream_next, verb);
                if (result == 0)
                {
                    filter.Freed += Filter_Freed;
                    filters.Add(filter);
                }
                return result;
            }
            catch (Exception exception)
            {
                Log.Write(LogLevel.Error, "FilterRegistration.StreamCreateCallback failed.");
                Log.Write(LogLevel.Error, exception.ToString());

                Proxy.giterr_set_str(GitErrorCategory.Filter, exception);

                git_writestream_out = IntPtr.Zero;
                return (int)GitErrorCode.Error;
            }
        }

        int StreamCheckCallback(GitFilter gitFilter, IntPtr payload, IntPtr filterSource, IntPtr attributeValues)
        {
            Debug.Assert(filterSource != IntPtr.Zero, "The `filterSource` parameter is null.");
            Debug.Assert(attributeValues != IntPtr.Zero, "The `attributeValues` parameter is null.");
            Debug.Assert(gitFilter.attributes != IntPtr.Zero, "The `gitFilter.attributes` parameter is null.");

            try
            {
                // no attribute means pass-through
                if (attributeValues == IntPtr.Zero)
                    return (int)GitErrorCode.PassThrough;

                string verb = null;

                unsafe
                {
                    byte** ptr = (byte**)(attributeValues);
                    byte* start = *ptr;
                    byte* walk = start;

                    if (start == null)
                        return (int)GitErrorCode.PassThrough;

                    while (*walk != 0)
                    {
                        walk += 1;
                    }

                    int length = (int)(walk - start);

                    if (length == 0)
                    {
                        verb = String.Empty;
                    }
                    else
                    {
                        verb = new string((sbyte*)start, 0, length, Encoding.UTF8);
                    }
                }

                // if the registered attribute isn't the catch-all and the verb doesn't match the attribute, pass-through
                if (!String.Equals(Attribute, WildCardFilterAttribute, StringComparison.Ordinal)
                    && !String.Equals(verb, this.Attribute, StringComparison.InvariantCultureIgnoreCase))
                    return (int)GitErrorCode.PassThrough;

                var key = gitFilter.attributes;
                if (verbs.ContainsKey(key))
                {
                    verbs[key] = verb;
                }
                else
                {
                    verbs.Add(key, verb);
                }
            }
            catch (Exception exception)
            {
                Log.Write(LogLevel.Error, "FilterRegistration.StreamCheckCallback failed.");
                Log.Write(LogLevel.Error, exception.ToString());

                Proxy.giterr_set_str(GitErrorCategory.Filter, exception);

                return (int)GitErrorCode.Error;
            }

            return (int)GitErrorCode.Ok;
        }

        private void Filter_Freed(Filter filter)
        {
            Debug.Assert(filter != null, "The `filter` parameter is null.");

            filters.Remove(filter);
            
            if (verbs.ContainsKey(filter.Key))
            {
                verbs.Remove(filter.Key);
            }
        }
    }

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
    public delegate void FilterRegistrationIntializationCallback();
}
