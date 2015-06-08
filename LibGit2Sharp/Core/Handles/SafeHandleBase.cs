
// This activates a lightweight mode which will help put under the light
// incorrectly released handles by outputing a warning message in the console.
//
// This should be activated when tests are being run of the CI server.
//
// Uncomment the line below or add a conditional symbol to activate this mode

//#define LEAKS_IDENTIFYING

// This activates a more throrough mode which will show the stack trace of the
// allocation code path for each handle that has been improperly released.
//
// This should be manually activated when some warnings have been raised as
// a result of LEAKS_IDENTIFYING mode activation.
//
// Uncomment the line below or add a conditional symbol to activate this mode

//#define LEAKS_TRACKING

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

#if LEAKS_IDENTIFYING
namespace LibGit2Sharp.Core
{
    /// <summary>
    /// Holds leaked handle type names reported by <see cref="Core.Handles.SafeHandleBase"/>
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
    internal abstract class SafeHandleBase : SafeHandle
    {

#if LEAKS_TRACKING
        private readonly string trace;
        private readonly Guid id;
#endif

        /// <summary>
        /// This is set to non-zero when <see cref="NativeMethods.AddHandle"/> has
        /// been called for this handle, but <see cref="NativeMethods.RemoveHandle"/>
        /// has not yet been called.
        /// </summary>
        private int registered;

        protected SafeHandleBase()
            : base(IntPtr.Zero, true)
        {
            NativeMethods.AddHandle();
            registered = 1;

#if LEAKS_TRACKING
            id = Guid.NewGuid();
            Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Allocating {0} handle ({1})", GetType().Name, id));
            trace = new StackTrace(2, true).ToString();
#endif
        }

        protected override void Dispose(bool disposing)
        {
            bool leaked = !disposing && !IsInvalid;

#if LEAKS_IDENTIFYING
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

        // Prevent the debugger from evaluating this property because it has side effects
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public override sealed bool IsInvalid
        {
            get
            {
                bool invalid = IsInvalidImpl();
                if (invalid && Interlocked.CompareExchange(ref registered, 0, 1) == 1)
                {
                    /* Unregister the handle because we know ReleaseHandle won't be called
                     * to do it for us.
                     */
                    NativeMethods.RemoveHandle();
                }

                return invalid;
            }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        protected virtual bool IsInvalidImpl()
        {
            return handle == IntPtr.Zero;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        protected abstract bool ReleaseHandleImpl();

        protected override sealed bool ReleaseHandle()
        {
            bool result;

            try
            {
                result = ReleaseHandleImpl();
            }
            finally
            {
                if (Interlocked.CompareExchange(ref registered, 0, 1) == 1)
                {
                    // if the handle is still registered at this point, we definitely
                    // want to unregister it
                    NativeMethods.RemoveHandle();
                }
                else
                {
                    /* For this to be called, the following sequence of events must occur:
                     *
                     *  1. The handle is created
                     *  2. The IsInvalid property is evaluated, and the result is false
                     *  3. The IsInvalid property is evaluated by the runtime to determine if
                     *     finalization is necessary, and the result is now true
                     *
                     * This can only happen if the value of `handle` is manipulated in an unexpected
                     * way (through the Reflection API or by a specially-crafted derived type that
                     * does not currently exist). The only safe course of action at this point in
                     * the shutdown process is returning false, which will trigger the
                     * releaseHandleFailed MDA but have no other impact on the CLR state.
                     * http://msdn.microsoft.com/en-us/library/85eak4a0.aspx
                     */
                    result = false;
                }
            }

            return result;
        }
    }
}
