using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Class to handle the mapping between libgit2 progress_cb callback on the git_checkout_opts
    ///   structure to the CheckoutProgressHandler delegate.
    /// </summary>
    internal class CheckoutCallbacks
    {
        /// <summary>
        ///   Managed delegate to call in response to checkout progress_cb callback.
        /// </summary>
        private CheckoutProgressHandler onCheckoutProgress;

        /// <summary>
        ///   Constructor to set up native callback for given managed delegate.
        /// </summary>
        /// <param name="onCheckoutProgress"><see cref="CheckoutProgressHandler"/> delegate to call in response to checkout progress_cb</param>
        private CheckoutCallbacks(CheckoutProgressHandler onCheckoutProgress)
        {
            this.onCheckoutProgress = onCheckoutProgress;
        }

        /// <summary>
        ///   Generate a delegate matching the signature of the native progress_cb callback and wraps the <see cref="CheckoutProgressHandler"/> delegate.
        /// </summary>
        /// <param name="onCheckoutProgress"><see cref="CheckoutProgressHandler"/> that should be wrapped in the native callback.</param>
        /// <returns>The delegate with signature matching the expected native callback. </returns>
        internal static progress_cb GenerateCheckoutCallbacks(CheckoutProgressHandler onCheckoutProgress)
        {
            if (onCheckoutProgress == null)
            {
                return null;
            }

            return new CheckoutCallbacks(onCheckoutProgress).OnGitCheckoutProgress;
        }

        /// <summary>
        ///   The delegate with a signature that matches the native checkout progress_cb function's signature.
        /// </summary>
        /// <param name="str">The path that was updated.</param>
        /// <param name="completedSteps">The number of completed steps.</param>
        /// <param name="totalSteps">The total number of steps.</param>
        /// <param name="payload">Payload object.</param>
        private void OnGitCheckoutProgress(IntPtr str, UIntPtr completedSteps, UIntPtr totalSteps, IntPtr payload)
        {
            // Convert null strings into empty strings.
            string path = (str != IntPtr.Zero) ? Utf8Marshaler.FromNative(str) : string.Empty;

            onCheckoutProgress(path, (int)completedSteps, (int)totalSteps);
        }
    }
}
