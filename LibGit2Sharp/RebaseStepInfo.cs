using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Information on a particular step of a rebase operation.
    /// </summary>
    public class RebaseStepInfo
    {
        internal RebaseStepInfo(RebaseStepOperation type, ObjectId id, string exec, int stepIndex, int totalStepCount)
        {
            Type = type;
            ID = id;
            Exec = exec;
            StepIndex = stepIndex;
            TotalStepCount = totalStepCount;
        }

        /// <summary>
        /// The rebase operation type.
        /// </summary>
        public RebaseStepOperation Type { get; private set; }

        /// <summary>
        /// The object ID the step is operating on.
        /// </summary>
        public ObjectId ID { get; private set; }

        /// <summary>
        /// Command to execute, if any.
        /// </summary>
        public string Exec { get; private set; }

        /// <summary>
        /// The index of this step.
        /// </summary>
        public int StepIndex { get; private set; }

        /// <summary>
        /// The total number of steps.
        /// </summary>
        public int TotalStepCount { get; private set; }
    }
}
