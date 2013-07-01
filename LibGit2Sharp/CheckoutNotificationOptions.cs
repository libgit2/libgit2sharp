using System;
using LibGit2Sharp.Handlers;

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

    /// <summary>
    /// Class to specify options and callback on CheckoutNotifications.
    /// </summary>
    public class CheckoutNotificationOptions
    {
        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected CheckoutNotificationOptions()
        {
        }

        /// <summary>
        /// The delegate that will be called for files that match the
        /// options specified in NotifyFlags.
        /// </summary>
        public virtual CheckoutNotifyHandler CheckoutNotifyHandler { get; private set; }

        /// <summary>
        /// The Flags specifying what notifications are reported.
        /// </summary>
        public virtual CheckoutNotifyFlags NotifyFlags { get; private set; }

        /// <summary>
        /// Construct the CheckoutNotificationOptions class.
        /// </summary>
        /// <param name="checkoutNotifyHandler"><see cref="CheckoutNotifyHandler"/> that checkout notifications are reported through.</param>
        /// <param name="notifyFlags">The checkout notification type.</param>
        public CheckoutNotificationOptions(CheckoutNotifyHandler checkoutNotifyHandler, CheckoutNotifyFlags notifyFlags)
        {
            CheckoutNotifyHandler = checkoutNotifyHandler;
            NotifyFlags = notifyFlags;
        }
    }
}
