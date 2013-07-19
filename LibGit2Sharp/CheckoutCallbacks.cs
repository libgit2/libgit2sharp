using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Class to handle the mapping between libgit2 progress_cb callback on the git_checkout_opts
    /// structure to the CheckoutProgressHandler delegate.
    /// </summary>
    internal class CheckoutCallbacks
    {
        /// <summary>
        /// The managed delegate (e.g. from library consumer) to be called in response to the checkout progress callback.
        /// </summary>
        private readonly CheckoutProgressHandler onCheckoutProgress;

        /// <summary>
        /// The managed delegate (e.g. from library consumer) to be called in response to the checkout notify callback.
        /// </summary>
        private readonly CheckoutNotifyHandler onCheckoutNotify;

        /// <summary>
        /// Constructor to set up native callback for given managed delegate.
        /// </summary>
        /// <param name="onCheckoutProgress"><see cref="CheckoutProgressHandler"/> delegate to call in response to checkout progress_cb</param>
        /// <param name="onCheckoutNotify"><see cref="CheckoutNotifyHandler"/> delegate to call in response to checkout notification callback.</param>
        private CheckoutCallbacks(CheckoutProgressHandler onCheckoutProgress, CheckoutNotifyHandler onCheckoutNotify)
        {
            this.onCheckoutProgress = onCheckoutProgress;
            this.onCheckoutNotify = onCheckoutNotify;
        }

        /// <summary>
        /// The method to pass for the native checkout progress callback.
        /// </summary>
        public progress_cb CheckoutProgressCallback
        {
            get
            {
                if (this.onCheckoutProgress != null)
                {
                    return this.OnGitCheckoutProgress;
                }

                return null;
            }
        }

        /// <summary>
        /// The method to pass for the native checkout notify callback.
        /// </summary>
        public checkout_notify_cb CheckoutNotifyCallback
        {
            get
            {
                if (this.onCheckoutNotify != null)
                {
                    return this.OnGitCheckoutNotify;
                }

                return null;
            }
        }

        /// <summary>
        /// Generate a delegate matching the signature of the native progress_cb callback and wraps the <see cref="CheckoutProgressHandler"/> delegate.
        /// </summary>
        /// <param name="onCheckoutProgress"><see cref="CheckoutProgressHandler"/> that should be wrapped in the native callback.</param>
        /// <param name="onCheckoutNotify"><see cref="CheckoutNotifyHandler"/> delegate to call in response to checkout notification callback.</param>
        /// <returns>The delegate with signature matching the expected native callback.</returns>
        internal static CheckoutCallbacks GenerateCheckoutCallbacks(CheckoutProgressHandler onCheckoutProgress, CheckoutNotifyHandler onCheckoutNotify)
        {
            return new CheckoutCallbacks(onCheckoutProgress, onCheckoutNotify);
        }

        /// <summary>
        /// The delegate with a signature that matches the native checkout progress_cb function's signature.
        /// </summary>
        /// <param name="str">The path that was updated.</param>
        /// <param name="completedSteps">The number of completed steps.</param>
        /// <param name="totalSteps">The total number of steps.</param>
        /// <param name="payload">Payload object.</param>
        private void OnGitCheckoutProgress(IntPtr str, UIntPtr completedSteps, UIntPtr totalSteps, IntPtr payload)
        {
            if (onCheckoutProgress != null)
            {
                // Convert null strings into empty strings.
                string path = (str != IntPtr.Zero) ? Utf8Marshaler.FromNative(str) : string.Empty;

                onCheckoutProgress(path, (int)completedSteps, (int)totalSteps);
            }
        }

        private int OnGitCheckoutNotify(
            CheckoutNotifyFlags why,
            IntPtr pathPtr,
            IntPtr baselinePtr,
            IntPtr targetPtr,
            IntPtr workdirPtr,
            IntPtr payloadPtr)
        {
            int result = 0;
            if (this.onCheckoutNotify != null)
            {
                string path = (pathPtr != IntPtr.Zero) ? FilePathMarshaler.FromNative(pathPtr).Native : string.Empty;
                result = onCheckoutNotify(path, why) ? 0 : 1;
            }

            return result;
        }
    }
}
