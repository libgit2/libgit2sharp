using System.Collections.Generic;
using LibGit2Sharp.Core.Compat;
using Xunit;

namespace LibGit2Sharp.Tests.TestHelpers
{
    /// <summary>
    ///   Class to verify the update_tips callback of the git_remote_callbacks structure.
    /// </summary>
    public class RemoteUpdateTipsCallbackHelper
    {
        private Dictionary<string, Tuple<ObjectId, ObjectId>> ExpectedReferenceUpdates;
        private Dictionary<string, Tuple<ObjectId, ObjectId>> ObservedReferenceUpdates;

        /// <summary>
        ///   Constructor.
        /// </summary>
        /// <param name="expectedCallbacks">Dictionary of expected reference name => tuple of (old ObjectId, new ObjectID) that should be updated.</param>
        public RemoteUpdateTipsCallbackHelper(Dictionary<string, Tuple<ObjectId, ObjectId>> expectedCallbacks)
        {
            ExpectedReferenceUpdates = new Dictionary<string, Tuple<ObjectId, ObjectId>>(expectedCallbacks);
            ObservedReferenceUpdates = new Dictionary<string, Tuple<ObjectId, ObjectId>>();
        }

        /// <summary>
        ///   Handler to hook up to UpdateTips callback.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void RemoteUpdateTipsHandler(object sender, UpdateTipsChangedEventArgs e)
        {
            // assert that we have not seen this reference before
            Assert.DoesNotContain(e.ReferenceName, ObservedReferenceUpdates.Keys);
            ObservedReferenceUpdates.Add(e.ReferenceName, new Tuple<ObjectId,ObjectId>(e.OldId, e.NewId));
            

            // verify that this reference is in the list of expected references
            Tuple<ObjectId, ObjectId> reference;
            bool referenceFound = ExpectedReferenceUpdates.TryGetValue(e.ReferenceName, out reference);
            Assert.True(referenceFound, string.Format("Could not find reference {0} in list of expected reference updates.", e.ReferenceName));
            
            // verify that the old / new Object IDs
            if(referenceFound)
            {
                Assert.Equal(reference.Item1, e.OldId);
                Assert.Equal(reference.Item2, e.NewId);
            }
        }

        /// <summary>
        ///   Check that all expected references have been updated.
        /// </summary>
        public void CheckUpdatedReferences()
        {
            // we have already verified that all observed reference updates are expected,
            // verify that we have seen all expected reference updates
            Assert.Equal(ExpectedReferenceUpdates.Count, ObservedReferenceUpdates.Count);
        }
    }
}
