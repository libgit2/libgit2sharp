using System;
using System.Diagnostics;
using System.Globalization;
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
        /// been called for this handle.
        /// </summary>
        private int registered = 0;

        protected SafeHandleBase()
            : base(IntPtr.Zero, true)
        {
            NativeMethods.AddHandle();
            registered = 1;
#if LEAKS
            trace = new StackTrace(2, true).ToString();
#endif
        }

        //SafeHandle inherits from CriticalFinalizerObject
        //thus there is strong guarantee that finalizer will be called
        //unless GC.SuppressFinalize was called from Dispose
        //for that reason handle is unregistered from finalizer either Dispose
        ~SafeHandleBase()
        {
            UnregisterHandle();
        }

        protected override void Dispose(bool disposing)
        {
#if DEBUG
            if (!disposing && !IsInvalid)
            {
                Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "A {0} handle wrapper has not been properly disposed.", GetType().Name));
#if LEAKS
                Trace.WriteLine(trace);
#endif
                Trace.WriteLine("");
            }
#endif
            base.Dispose(disposing);
            UnregisterHandle();
        }

        private void UnregisterHandle()
        {
            int n = Interlocked.Decrement(ref registered);
            if (n == 0)
                NativeMethods.RemoveHandle();
        }

        public override bool IsInvalid
        {
            get { return (handle == IntPtr.Zero); }
        }

        protected abstract override bool ReleaseHandle();
    }
}
