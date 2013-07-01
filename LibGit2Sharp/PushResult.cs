using System.Collections.Generic;

namespace LibGit2Sharp
{
    /// <summary>
    /// Contains the results of a push operation.
    /// </summary>
    public class PushResult
    {
        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected PushResult()
        { }

        /// <summary>
        /// <see cref="PushStatusError"/>s that failed to update.
        /// </summary>
        public virtual IEnumerable<PushStatusError> FailedPushUpdates
        {
            get
            {
                return failedPushUpdates;
            }
        }

        /// <summary>
        /// Flag indicating if there were errors reported
        /// when updating references on the remote.
        /// </summary>
        public virtual bool HasErrors
        {
            get
            {
                return failedPushUpdates.Count > 0;
            }
        }

        internal PushResult(List<PushStatusError> failedPushUpdates)
        {
            this.failedPushUpdates = failedPushUpdates ?? new List<PushStatusError>();
        }

        private readonly List<PushStatusError> failedPushUpdates;
    }
}
