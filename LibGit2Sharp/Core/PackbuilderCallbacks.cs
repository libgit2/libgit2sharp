using System;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp.Core
{
    internal class PackbuilderCallbacks
    {
        private readonly PackBuilderProgressHandler onPackBuilderProgress;

        /// <summary>S
        /// Constructor to set up the native callback given managed delegate.
        /// </summary>
        /// <param name="onPackBuilderProgress">The <see cref="PackBuilderProgressHandler"/> delegate that the git_packbuilder_progress will call.</param>
        internal PackbuilderCallbacks(PackBuilderProgressHandler onPackBuilderProgress)
        {
            this.onPackBuilderProgress = onPackBuilderProgress;
        }

        /// <summary>
        /// Generates a delegate that matches the native git_packbuilder_progress function's signature and wraps the <see cref="PackBuilderProgressHandler"/> delegate.
        /// </summary>
        /// <returns>A delegate method with a signature that matches git_transfer_progress_callback.</returns>
        internal NativeMethods.git_packbuilder_progress GenerateCallback()
        {
            if (onPackBuilderProgress == null)
            {
                return null;
            }

            return new PackbuilderCallbacks(onPackBuilderProgress).OnGitPackBuilderProgress;
        }

        private int OnGitPackBuilderProgress(int stage, uint current, uint total, IntPtr payload)
        {
            return Proxy.ConvertResultToCancelFlag(onPackBuilderProgress((PackBuilderStage)stage, (int)current, (int)total));
        }
    }
}
