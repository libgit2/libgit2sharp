using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Flags controlling checkout notification behavior.
    /// </summary>
    [Flags]
    public enum CheckoutNotifyFlags
    {

        /// <summary>
        /// No checkout notification.
        /// </summary>
        None = 0, /* GIT_CHECKOUT_NOTIFY_NONE */

        /// <summary>
        /// Notify on conflicting paths.
        /// </summary>
        Conflict = (1 << 0), /* GIT_CHECKOUT_NOTIFY_CONFLICT */

        /// <summary>
        /// Notify about dirty files. These are files that do not need
        /// an update, but no longer match the baseline.
        /// </summary>
        Dirty = (1 << 1), /* GIT_CHECKOUT_NOTIFY_DIRTY */

        /// <summary>
        /// Notify for files that will be updated.
        /// </summary>
        Updated = (1 << 2), /* GIT_CHECKOUT_NOTIFY_UPDATED */

        /// <summary>
        /// Notify for untracked files.
        /// </summary>
        Untracked = (1 << 3), /* GIT_CHECKOUT_NOTIFY_UNTRACKED */

        /// <summary>
        /// Notify about ignored file.
        /// </summary>
        Ignored = (1 << 4), /* GIT_CHECKOUT_NOTIFY_IGNORED */
    }
}
