using System;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Collection of parameters controlling Checkout behavior.
    /// </summary>
    public class CheckoutOptions
    {
        /// <summary>
        /// Options controlling checkout behavior.
        /// </summary>
        public virtual CheckoutModifiers CheckoutModifiers { get; set; }

        /// <summary>
        /// Callback method to report checkout progress updates through.
        /// </summary>
        public virtual CheckoutProgressHandler OnCheckoutProgress { get; set; }

        /// <summary>
        /// Options to manage checkout notifications.
        /// </summary>
        public virtual CheckoutNotificationOptions CheckoutNotificationOptions { get; set; }

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        public CheckoutOptions()
        { }

        /// <summary>
        /// CheckoutOptions constructor.
        /// </summary>
        /// <param name="checkoutModifieers">CheckoutModifiers property.</param>
        /// <param name="onCheckoutProgress">OnCheckoutProgress property.</param>
        /// <param name="checkoutNotificationOptions">CheckoutNotificationsOptions property.</param>
        public CheckoutOptions(
            CheckoutModifiers checkoutModifieers,
            CheckoutProgressHandler onCheckoutProgress,
            CheckoutNotificationOptions checkoutNotificationOptions)
        {
            this.CheckoutModifiers = checkoutModifieers;
            this.OnCheckoutProgress = onCheckoutProgress;
            this.CheckoutNotificationOptions = checkoutNotificationOptions;
        }
    }
}
