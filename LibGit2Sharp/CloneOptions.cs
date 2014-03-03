using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Options to define clone behaviour
    /// </summary>
    public sealed class CloneOptions
    {
        /// <summary>
        /// Creates default <see cref="CloneOptions"/> for a non-bare clone
        /// </summary>
        public CloneOptions()
        {
            Checkout = true;
        }

        /// <summary>
        /// True will result in a bare clone, false a full clone.
        /// </summary>
        public bool IsBare { get; set; }

        /// <summary>
        /// If true, the origin's HEAD will be checked out. This only applies
        /// to non-bare repositories.
        /// </summary>
        public bool Checkout { get; set; }

        /// <summary>
        /// Handler for network transfer and indexing progress information
        /// </summary>
        public TransferProgressHandler OnTransferProgress { get; set; }

        /// <summary>
        /// Handler for checkout progress information
        /// </summary>
        public CheckoutProgressHandler OnCheckoutProgress { get; set; }

        /// <summary>
        /// Credentials to use for user/pass authentication
        /// </summary>
        public Credentials Credentials { get; set; }
    }
}
