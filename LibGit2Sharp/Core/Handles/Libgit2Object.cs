// This activates a lightweight mode which will help put under the light
// incorrectly released handles by outputing a warning message in the console.
//
// This should be activated when tests are being run on the CI server.
//
// Uncomment the line below or add a conditional symbol to activate this mode

// #define LEAKS_IDENTIFYING

// This activates a more throrough mode which will show the stack trace of the
// allocation code path for each handle that has been improperly released.
//
// This should be manually activated when some warnings have been raised as
// a result of LEAKS_IDENTIFYING mode activation.
//
// Uncomment the line below or add a conditional symbol to activate this mode

// #define LEAKS_TRACKING

using System;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;

#if LEAKS_IDENTIFYING
namespace LibGit2Sharp.Core
{
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
    internal unsafe abstract class Libgit2Object : IDisposable
    {
#if LEAKS_TRACKING
        private readonly string trace;
        private readonly Guid id;
#endif

        protected void* ptr;

        internal void* Handle
        {
            get
            {
                return ptr;
            }
        }

        bool owned;
        bool disposed;

        internal unsafe Libgit2Object(void* handle, bool owned)
        {
            this.ptr = handle;
            this.owned = owned;

#if LEAKS_TRACKING
            id = Guid.NewGuid();
            Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Allocating {0} handle ({1})", GetType().Name, id));

            trace = new StackTrace(2, true).ToString();
#endif
        }

        internal unsafe Libgit2Object(IntPtr ptr, bool owned)
            : this(ptr.ToPointer(), owned)
        {
        }

        ~Libgit2Object()
        {
            Dispose(false);
        }

        internal bool IsNull
        {
            get
            {
                return ptr == null;
            }
        }

        internal IntPtr AsIntPtr()
        {
            return new IntPtr(ptr);
        }

        public abstract void Free();

        void Dispose(bool disposing)
        {
#if LEAKS_IDENTIFYING
            bool leaked = !disposing && ptr != null;

            if (leaked)
            {
                LeaksContainer.Add(GetType().Name);
            }
#endif

            if (!disposed)
            {
                if (owned)
                {
                    Free();
                }

                ptr = null;
            }

            disposed = true;

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

            public void Dispose()
        {
            Dispose(true);
        }
    }
}

