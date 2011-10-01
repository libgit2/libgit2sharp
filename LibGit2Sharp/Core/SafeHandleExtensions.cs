namespace LibGit2Sharp.Core
{
    internal static class SafeHandleExtensions
    {
        public static void SafeDispose(this SafeHandleBase handle)
        {
            if (handle == null || handle.IsInvalid)
            {
                return;
            }

            handle.Dispose();
        }
    }
}