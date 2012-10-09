using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core.Compat;

namespace LibGit2Sharp.Tests.TestHelpers
{
    /// <summary>
    ///   This is the expected information based on the test repository at:
    ///   github.com/nulltoken/TestGitRepository
    /// </summary>
    public class TestRemoteExpectedInfo
    {
        public Dictionary<string, string> ExpectedBranchTips = new Dictionary<string, string>();

        public Dictionary<string, string> ExpectedTags = new Dictionary<string, string>();

        public Dictionary<string, Tuple<ObjectId, ObjectId>> ExpectedReferenceCallbacks = new Dictionary<string, Tuple<ObjectId, ObjectId>>();

        public TestRemoteExpectedInfo(string remoteName)
        {
            ExpectedBranchTips.Add(remoteName + "/" + "master", "49322bb17d3acc9146f98c97d078513228bbf3c0");
            ExpectedBranchTips.Add(remoteName + "/" + "first-merge", "0966a434eb1a025db6b71485ab63a3bfbea520b6");
            ExpectedBranchTips.Add(remoteName + "/" + "no-parent", "42e4e7c5e507e113ebbb7801b16b52cf867b7ce1");

            ExpectedTags.Add("annotated_tag", "c070ad8c08840c8116da865b2d65593a6bb9cd2a");
            ExpectedTags.Add("blob", "55a1a760df4b86a02094a904dfa511deb5655905");
            ExpectedTags.Add("commit_tree", "8f50ba15d49353813cc6e20298002c0d17b0a9ee");
            ExpectedTags.Add("nearly-dangling", "6e0c7bdb9b4ed93212491ee778ca1c65047cab4e");

            string referenceUpdateBase = "refs/remotes/" + remoteName + "/";
            ExpectedReferenceCallbacks.Add(referenceUpdateBase + "master", new Tuple<ObjectId, ObjectId>(new ObjectId("0000000000000000000000000000000000000000"), new ObjectId("49322bb17d3acc9146f98c97d078513228bbf3c0")));
            ExpectedReferenceCallbacks.Add(referenceUpdateBase + "first-merge", new Tuple<ObjectId, ObjectId>(new ObjectId("0000000000000000000000000000000000000000"), new ObjectId("0966a434eb1a025db6b71485ab63a3bfbea520b6")));
            ExpectedReferenceCallbacks.Add(referenceUpdateBase + "no-parent", new Tuple<ObjectId, ObjectId>(new ObjectId("0000000000000000000000000000000000000000"), new ObjectId("42e4e7c5e507e113ebbb7801b16b52cf867b7ce1")));
        }
    }
}
