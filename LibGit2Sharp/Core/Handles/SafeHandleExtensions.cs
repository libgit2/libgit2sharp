namespace LibGit2Sharp.Core.Handles
{
    internal static class SafeHandleExtensions
    {
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
