using System;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Options controlling Checkout behavior.
    /// </summary>
    [Flags]
    public enum CheckoutOptions
    {
        /// <summary>
        ///   No checkout flags - use default behavior.
        /// </summary>
        None = 0,

        /// <summary>
        ///   Proceed with checkout even if the index or the working tree differs from HEAD.
        ///   This will throw away local changes.
        /// </summary>
        Force,
    }
}
