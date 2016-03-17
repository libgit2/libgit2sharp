using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public partial class Repository
    {
        /// <summary>
        /// Sets the current <see cref="Repository.Head"/> and resets the <see cref="Index"/> and
        /// the content of the working tree to match.
        /// </summary>
        /// <param name="resetMode">Flavor of reset operation to perform.</param>
        public void Reset(ResetMode resetMode)
        {
            Reset(resetMode, "HEAD");
        }

        /// <summary>
        /// Sets the current <see cref="Repository.Head"/> to the specified commitish and optionally resets the <see cref="Index"/> and
        /// the content of the working tree to match.
        /// </summary>
        /// <param name="resetMode">Flavor of reset operation to perform.</param>
        /// <param name="committish">A revparse spec for the target commit object.</param>
        public void Reset(ResetMode resetMode, string committish)
        {
            Ensure.ArgumentNotNullOrEmptyString(committish, "committish");

            Commit commit = LookUpCommit(this, committish);

            Reset(resetMode, commit);
        }

        private Commit LookUpCommit(IRepository repository, string committish)
        {
            GitObject obj = repository.Lookup(committish);
            Ensure.GitObjectIsNotNull(obj, committish);
            return obj.DereferenceToCommit(true);
        }

        /// <summary>
        /// Sets the current <see cref="Head"/> to the specified commit and optionally resets the <see cref="Index"/> and
        /// the content of the working tree to match.
        /// </summary>
        /// <param name="resetMode">Flavor of reset operation to perform.</param>
        /// <param name="commit">The target commit object.</param>
        public void Reset(ResetMode resetMode, Commit commit)
        {
            Reset(resetMode, commit, new CheckoutOptions());
        }

        /// <summary>
        /// Sets <see cref="Head"/> to the specified commit and optionally resets the <see cref="Index"/> and
        /// the content of the working tree to match.
        /// </summary>
        /// <param name="resetMode">Flavor of reset operation to perform.</param>
        /// <param name="commit">The target commit object.</param>
        /// <param name="opts">Collection of parameters controlling checkout behavior.</param>
        public void Reset(ResetMode resetMode, Commit commit, CheckoutOptions opts)
        {
            Ensure.ArgumentNotNull(commit, "commit");
            Ensure.ArgumentNotNull(opts, "opts");

            using (GitCheckoutOptsWrapper checkoutOptionsWrapper = new GitCheckoutOptsWrapper(opts))
            {
                var options = checkoutOptionsWrapper.Options;
                Proxy.git_reset(handle, commit.Id, resetMode, ref options);
            }
        }
    }
}

