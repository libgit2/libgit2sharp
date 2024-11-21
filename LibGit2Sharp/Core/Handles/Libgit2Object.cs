// This activates a lightweight mode which will help put under the light
// incorrectly released handles by outputting a warning message in the console.
//
// This should be activated when tests are being run on the CI server.
//
// Uncomment the line below or add a conditional symbol to activate this mode

//#define LEAKS_IDENTIFYING

// This activates a more thorough mode which will show the stack trace of the
// allocation code path for each handle that has been improperly released.
//
// This should be manually activated when some warnings have been raised as
// a result of LEAKS_IDENTIFYING mode activation.
//
// Uncomment the line below or add a conditional symbol to activate this mode

//#define LEAKS_TRACKING

using System;
using Microsoft.Win32.SafeHandles;

#if LEAKS_IDENTIFYING
namespace LibGit2Sharp.Core
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Holds leaked handle type names reported by <see cref="Core.Handles.Libgit2Object"/>
    /// </summary>
    public static class LeaksContainer
    {
        private static readonly HashSet<string> _typeNames = new HashSet<string>();
        private static readonly object _lockpad = new object();

        /// <summary>
        /// Report a new leaked handle type name
        /// </summary>
        /// <param name="typeName">Short name of the leaked handle type.</param>
        public static void Add(string typeName)
        {
            lock (_lockpad)
            {
                _typeNames.Add(typeName);
            }
        }

        /// <summary>
        /// Removes all previously reported leaks.
        /// </summary>
        public static void Clear()
        {
            lock (_lockpad)
            {
                _typeNames.Clear();
            }
        }

        /// <summary>
        /// Returns all reported leaked handle type names.
        /// </summary>
        public static IEnumerable<string> TypeNames
        {
            get
            {
                string[] result = null;
                lock (_lockpad)
                {
                    result = _typeNames.ToArray();
                }
                return result;
            }
        }
    }
}
#endif

namespace LibGit2Sharp.Core.Handles
{
#if LEAKS_TRACKING
    using System.Diagnostics;
    using System.Globalization;
#endif

    internal unsafe abstract class Libgit2Object : SafeHandleZeroOrMinusOneIsInvalid
    {
#if LEAKS_TRACKING
        private readonly string trace;
        private readonly Guid id;
#endif

        internal unsafe Libgit2Object(void* ptr, bool owned)
            : this(new IntPtr(ptr), owned)
        {
        }

        internal unsafe Libgit2Object(IntPtr ptr, bool owned)
            : base(owned)
        {
            SetHandle(ptr);

#if LEAKS_TRACKING
            id = Guid.NewGuid();
            Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Allocating {0} handle ({1})", GetType().Name, id));

            trace = new StackTrace(2, true).ToString();
#endif
        }

        internal IntPtr AsIntPtr() => DangerousGetHandle();

        protected override void Dispose(bool disposing)
        {
#if LEAKS_IDENTIFYING
            bool leaked = !disposing && DangerousGetHandle() != IntPtr.Zero;

            if (leaked)
            {
                LeaksContainer.Add(GetType().Name);
            }
#endif

            base.Dispose(disposing);

#if LEAKS_TRACKING
            if (!leaked)
            {
                Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Disposing {0} handle ({1})", GetType().Name, id));
            }
            else
            {
                Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Unexpected finalization of {0} handle ({1})", GetType().Name, id));
                Trace.WriteLine(trace);
                Trace.WriteLine("");
            }
#endif
        }
    }
}

