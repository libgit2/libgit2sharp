using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace LibGit2Sharp.Core.Handles
{
    internal abstract class SafeHandleBase : SafeHandle
    {
#if LEAKS
        private readonly string trace;
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
#if LEAKS
            trace = new StackTrace(2, true).ToString();
#endif
        }

#if DEBUG
        protected override void Dispose(bool disposing)
        {
            if (!disposing && !IsInvalid)
            {
                Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "A {0} handle wrapper has not been properly disposed.", GetType().Name));
#if LEAKS
                Trace.WriteLine(trace);
#endif
                Trace.WriteLine("");
            }

            base.Dispose(disposing);
        }
#endif

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
