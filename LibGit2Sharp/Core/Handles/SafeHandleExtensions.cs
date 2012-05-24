using System;

namespace LibGit2Sharp.Core.Handles
{
    internal static class SafeHandleExtensions
    {
        public static void SafeDispose(this IDisposable disposable)
        {
            if (disposable == null)
                return;

            var handle = disposable as SafeHandleBase;
            if (handle != null)
            {
                SafeDispose(handle);
                return;
            }

            disposable.Dispose();
        }

        public static void SafeDispose(this SafeHandleBase handle)
        {
            if (handle == null || handle.IsClosed || handle.IsInvalid)
            {
                return;
            }

            handle.Dispose();
        }
    }
}
