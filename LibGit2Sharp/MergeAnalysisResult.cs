using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// The result of analyzing the possibilities of merging a commit or branch into HEAD.
    /// </summary>
    public struct MergeAnalysisResult
    {
        /// <summary>
        /// The result of the analysis done on the commits given.
        /// </summary>
        public MergeAnalysis Analysis;
        /// <summary>
        /// The user's preference for the type of merge to perform.
        /// </summary>
        public MergePreference Preference;

        static MergeAnalysis MergeAnalysisFromGitMergeAnalysis(GitMergeAnalysis analysisIn)
        {
            MergeAnalysis analysis = default(MergeAnalysis);

            if (analysisIn.HasFlag(GitMergeAnalysis.GIT_MERGE_ANALYSIS_NORMAL))
            {
                analysis |= MergeAnalysis.Normal;
            }
            if (analysisIn.HasFlag(GitMergeAnalysis.GIT_MERGE_ANALYSIS_UP_TO_DATE))
            {
                analysis |= MergeAnalysis.UpToDate;
            }
            if (analysisIn.HasFlag(GitMergeAnalysis.GIT_MERGE_ANALYSIS_FASTFORWARD))
            {
                analysis |= MergeAnalysis.FastForward;
            }

            if (analysisIn.HasFlag(GitMergeAnalysis.GIT_MERGE_ANALYSIS_UNBORN))
            {
                analysis |= MergeAnalysis.Unborn;
            }

            return analysis;
        }

        static MergePreference MergePreferenceFromGitMergePreference(GitMergePreference preference)
        {
            switch (preference)
            {
                case GitMergePreference.GIT_MERGE_PREFERENCE_NONE:
                    return MergePreference.Default;
                case GitMergePreference.GIT_MERGE_PREFERENCE_FASTFORWARD_ONLY:
                    return MergePreference.FastForwardOnly;
                case GitMergePreference.GIT_MERGE_PREFERENCE_NO_FASTFORWARD:
                    return MergePreference.NoFastForward;
                default:
                    throw new InvalidOperationException(String.Format("Unknown merge preference: {0}", preference));
            }
        }

        internal MergeAnalysisResult(GitMergeAnalysis analysis, GitMergePreference preference)
        {
            Analysis = MergeAnalysisFromGitMergeAnalysis(analysis);
            Preference = MergePreferenceFromGitMergePreference(preference);
        }
    }
}
