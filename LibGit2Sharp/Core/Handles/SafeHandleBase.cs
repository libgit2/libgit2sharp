using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Interlocked = System.Threading.Interlocked;

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
                    NativeMethods.RemoveHandle();
                }
                else if (!invalid && Interlocked.CompareExchange(ref registered, 1, 0) == 0)
                {
                    // Call AddHandle at most 1 time for this handle, and only after
                    // we know that the handle is valid (i.e. ReleaseHandle will eventually
                    // be called).
                    NativeMethods.AddHandle();
                }

                return invalid;
            }
        }

        protected virtual bool IsInvalidImpl()
        {
            return handle == IntPtr.Zero;
        }

        protected abstract bool ReleaseHandleImpl();

        protected override sealed bool ReleaseHandle()
        {
            try
            {
                return ReleaseHandleImpl();
            }
            finally
            {
                NativeMethods.RemoveHandle();
            }
        }
    }
}
