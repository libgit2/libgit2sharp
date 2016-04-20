using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    /// Information about a rebase step that is about to be performed.
    /// </summary>
    public class BeforeRebaseStepInfo
    {
        /// <summary>
        /// Needed for mocking.
        /// </summary>
        protected BeforeRebaseStepInfo()
        { }

        internal BeforeRebaseStepInfo(RebaseStepInfo stepInfo, long stepIndex, long totalStepCount)
        {
            StepInfo = stepInfo;
            StepIndex = stepIndex;
            TotalStepCount = totalStepCount;
        }

        /// <summary>
        /// Information on the step that is about to be performed.
        /// </summary>
        public virtual RebaseStepInfo StepInfo { get; private set; }

        /// <summary>
        /// The index of the step that is to be run.
        /// </summary>
        public virtual long StepIndex { get; private set; }

        /// <summary>
        /// The total number of steps in the rebase operation.
        /// </summary>
        public virtual long TotalStepCount { get; private set; }
    }
}
