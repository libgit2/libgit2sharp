using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core.Handles
{
    internal abstract class SafeHandleBase : SafeHandle
    {
#if LEAKS
        private readonly string trace;
#endif

        protected SafeHandleBase()
            : base(IntPtr.Zero, true)
        {
            NativeMethods.AddHandle();
#if LEAKS
            trace = new StackTrace(2, true).ToString();
#endif
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
        }

        public override bool IsInvalid
        {
            get { return (handle == IntPtr.Zero); }
        }

        protected abstract bool InternalReleaseHandle();

        protected override bool ReleaseHandle()
        {
            try
            {
                return InternalReleaseHandle();
            }
            finally
            {
                NativeMethods.RemoveHandle();
            }
        }
    }
}
