using System;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options controlling Checkout behavior.
    /// </summary>
    [Flags]
    public enum CheckoutModifiers
    {
        /// <summary>
        /// No checkout flags - use default behavior.
        /// </summary>
        None = 0,

        /// <summary>
        /// Proceed with checkout even if the index or the working tree differs from HEAD.
        /// This will throw away local changes.
        /// </summary>
        Force,

        /// <summary>
        /// This will try to merge any local changes from source branch into target branch.
        /// </summary>
        Merge,
    }
}
