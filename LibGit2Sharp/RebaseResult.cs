using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    /// The status of the rebase.
    /// </summary>
    public enum RebaseStatus
    {
        /// <summary>
        /// The rebase operation was run to completion
        /// </summary>
        Complete,

        /// <summary>
        /// The rebase operation hit a conflict and stopped.
        /// </summary>
        Conflicts,

        /// <summary>
        /// The rebase operation has hit a user requested stop point
        /// (edit, reword, ect.)
        /// </summary>
        Stop,
    };

    /// <summary>
    /// Information on a rebase operation.
    /// </summary>
    public class RebaseResult
    {
        /// <summary>
        /// Needed for mocking.
        /// </summary>
        protected RebaseResult()
        { }

        internal RebaseResult(RebaseStatus status,
                              long stepNumber,
                              long totalSteps,
                              RebaseStepInfo currentStepInfo)
        {
            Status = status;
            CompletedStepCount = stepNumber;
            TotalStepCount = totalSteps;
            CurrentStepInfo = currentStepInfo;
        }

        /// <summary>
        /// Information on the operation to be performed in the current step.
        /// If the overall Rebase operation has completed successfully, this will
        /// be null.
        /// </summary>
        public virtual RebaseStepInfo CurrentStepInfo { get; private set; }

        /// <summary>
        /// Did the rebase operation run until it should stop
        /// (completed the rebase, or the operation for the current step
        /// is one that sequencing should stop.
        /// </summary>
        public virtual RebaseStatus Status { get; protected set; }

        /// <summary>
        /// The number of completed steps.
        /// </summary>
        public virtual long CompletedStepCount { get; protected set; }

        /// <summary>
        /// The total number of steps in the rebase.
        /// </summary>
        public virtual long TotalStepCount { get; protected set; }
    }
}
