using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace LibGit2Sharp.Tests.TestHelpers
{
    /// <summary>
    ///   Class to verify the expected state after fetching github.com/nulltoken/TestGitRepository into an empty repository.
    ///   Includes the expected reference callbacks and the expected branches / tags after fetch is completed.
    /// </summary>
    internal class ExpectedFetchState
    {
        /// <summary>
        ///   Name of the Remote being fetched from.
        /// </summary>
        internal string RemoteName { get; private set; }

        /// <summary>
        ///   Expected branch tips after fetching into an empty repository.
        /// </summary>
        private Dictionary<string, ObjectId> ExpectedBranchTips = new Dictionary<string, ObjectId>();

        /// <summary>
        ///   Expected tags after fetching into an empty repository
        /// </summary>
        private Dictionary<string, TestRemoteInfo.ExpectedTagInfo> ExpectedTags = new Dictionary<string, TestRemoteInfo.ExpectedTagInfo>();

        /// <summary>
        ///   References that we expect to be updated in the UpdateReferenceTips callback.
        /// </summary>
        private Dictionary<string, ReferenceUpdate> ExpectedReferenceUpdates = new Dictionary<string, ReferenceUpdate>();

        /// <summary>
        ///   References that were actually updated in the UpdateReferenceTips callback.
        /// </summary>
        private Dictionary<string, ReferenceUpdate> ObservedReferenceUpdates = new Dictionary<string, ReferenceUpdate>();

        /// <summary>
        ///   Constructor.
        /// </summary>
        /// <param name="remoteName">Name of the remote being updated.</param>
        /// <param name="url">Url of the remote.</param>
        public ExpectedFetchState(string remoteName)
        {
            RemoteName = remoteName;
        }

        /// <summary>
        ///   Add information on a branch that is expected to be updated during a fetch.
        /// </summary>
        /// <param name="branchName">Name of the branch.</param>
        /// <param name="oldId">Old ID of the branch reference.</param>
        /// <param name="newId">Expected updated ID of the branch reference.</param>
        public void AddExpectedBranch(string branchName, ObjectId oldId, ObjectId newId)
        {
            string referenceUpdateBase = "refs/remotes/" + RemoteName + "/";
            ExpectedBranchTips.Add(referenceUpdateBase + branchName, newId);
            ExpectedReferenceUpdates.Add(referenceUpdateBase + branchName, new ReferenceUpdate(oldId, newId));
        }

        /// <summary>
        ///   Add information on a tag that is expected to be updated during a fetch.
        /// </summary>
        /// <param name="tagName">Name of the tag.</param>
        /// <param name="oldId">Old ID of the tag.</param>
        /// <param name="tag">Datastructure containing expected updated tag information.</param>
        public void AddExpectedTag(string tagName, ObjectId oldId, TestRemoteInfo.ExpectedTagInfo tag)
        {
            string tagReferenceBase = "refs/tags/";
            ExpectedTags.Add(tagReferenceBase + tagName, tag);

            ObjectId referenceId = tag.IsAnnotated ? tag.AnnotationId : tag.TargetId;
            ExpectedReferenceUpdates.Add(tagReferenceBase + tagName, new ReferenceUpdate(oldId, referenceId));
        }

        /// <summary>
        ///   Handler to hook up to UpdateTips callback.
        /// </summary>
        /// <param name="referenceName">Name of reference being updated.</param>
        /// <param name="oldId">Old ID of reference.</param>
        /// <param name="newId">New ID of reference.</param>
        /// <returns></returns>
        public int RemoteUpdateTipsHandler(string referenceName, ObjectId oldId, ObjectId newId)
        {
            // assert that we have not seen this reference before
            Assert.DoesNotContain(referenceName, ObservedReferenceUpdates.Keys);
            ObservedReferenceUpdates.Add(referenceName, new ReferenceUpdate(oldId, newId));

            // verify that this reference is in the list of expected references
            ReferenceUpdate referenceUpdate;
            bool isReferenceFound = ExpectedReferenceUpdates.TryGetValue(referenceName, out referenceUpdate);
            Assert.True(isReferenceFound, string.Format("Could not find the reference {0} in the list of expected reference updates.", referenceName));

            // verify that the old / new Object IDs
            if (isReferenceFound)
            {
                Assert.Equal(referenceUpdate.OldId, oldId);
                Assert.Equal(referenceUpdate.NewId, newId);
            }

            return 0;
        }

        /// <summary>
        ///   Check that all expected references have been updated.
        /// </summary>
        /// <param name="repo">Repository object whose state will be checked against expected state.</param>
        public void CheckUpdatedReferences(Repository repo)
        {
            // Verify the expected branches.
            // First, verify the expected branches have been created and
            List<string> sortedObservedBranches = repo.Branches.Select(branch => branch.CanonicalName).ToList();
            sortedObservedBranches.Sort();
            List<string> sortedExpectedBranches = ExpectedBranchTips.Keys.ToList();
            sortedExpectedBranches.Sort();
            Assert.Equal(sortedExpectedBranches, sortedObservedBranches);

            // Verify branches reference expected commits.
            foreach (KeyValuePair<string, ObjectId> kvp in ExpectedBranchTips)
            {
                Branch branch = repo.Branches[kvp.Key];
                Assert.NotNull(branch);
                Assert.Equal(kvp.Value, branch.Tip.Id);
            }

            // Verify the expected tags
            // First, verify the expected tags have been created
            List<string> sortedObservedTags = repo.Tags.Select(tag => tag.CanonicalName).ToList();
            sortedObservedTags.Sort();
            List<string> sortedExpectedTags = ExpectedTags.Keys.ToList();
            sortedExpectedTags.Sort();
            Assert.Equal(sortedExpectedTags, sortedObservedTags);

            // Verify tags reference the expected IDs.
            foreach (KeyValuePair<string, TestRemoteInfo.ExpectedTagInfo> kvp in ExpectedTags)
            {
                Tag tag = repo.Tags[kvp.Key];
                TestRemoteInfo.ExpectedTagInfo expectedTagInfo = kvp.Value;

                Assert.NotNull(tag);
                Assert.NotNull(tag.Target);

                Assert.Equal(expectedTagInfo.TargetId, tag.Target.Id);

                if (expectedTagInfo.IsAnnotated)
                {
                    Assert.NotNull(tag.Annotation);
                    Assert.Equal(expectedTagInfo.AnnotationId, tag.Annotation.Id);
                }
            }

            // We have already verified that all observed reference updates are expected,
            // verify that we have seen all expected reference updates.
            Assert.Equal(ExpectedReferenceUpdates.Count, ObservedReferenceUpdates.Count);
        }


        #region ExpectedFetchState

        /// <summary>
        ///   Structure to track a reference that has been updated.
        /// </summary>
        private struct ReferenceUpdate
        {
            /// <summary>
            ///   Old ID of the reference.
            /// </summary>
            public ObjectId OldId;

            /// <summary>
            ///   New ID of the reference.
            /// </summary>
            public ObjectId NewId;

            public ReferenceUpdate(ObjectId oldId, ObjectId newId)
            {
                OldId = oldId;
                NewId = newId;
            }
        }

        #endregion
    }
}
