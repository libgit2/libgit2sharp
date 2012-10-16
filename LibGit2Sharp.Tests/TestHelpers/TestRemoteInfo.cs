using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibGit2Sharp.Core.Compat;

namespace LibGit2Sharp.Tests.TestHelpers
{
    /// <summary>
    ///   This is the state of the test repository based at:
    ///   github.com/libgit2/TestGitRepository
    /// </summary>
    public class TestRemoteInfo
    {
        /// <summary>
        ///   Expected Branch tips of the remote repository.
        /// </summary>
        public Dictionary<string, ObjectId> BranchTips = new Dictionary<string, ObjectId>();

        /// <summary>
        ///   Expected Tags of the remote repository.
        /// </summary>
        public Dictionary<string, ExpectedTagInfo> Tags = new Dictionary<string, ExpectedTagInfo>();

        public static TestRemoteInfo TestRemoteInstance = new TestRemoteInfo();

        private TestRemoteInfo()
        {
            BranchTips.Add("master", new ObjectId("49322bb17d3acc9146f98c97d078513228bbf3c0"));
            BranchTips.Add("first-merge", new ObjectId("0966a434eb1a025db6b71485ab63a3bfbea520b6"));
            BranchTips.Add("no-parent", new ObjectId("42e4e7c5e507e113ebbb7801b16b52cf867b7ce1"));

            Tags.Add("annotated_tag", new ExpectedTagInfo(new ObjectId("c070ad8c08840c8116da865b2d65593a6bb9cd2a"), new ObjectId("d96c4e80345534eccee5ac7b07fc7603b56124cb")));
            Tags.Add("blob", new ExpectedTagInfo(new ObjectId("55a1a760df4b86a02094a904dfa511deb5655905")));
            Tags.Add("commit_tree", new ExpectedTagInfo(new ObjectId("8f50ba15d49353813cc6e20298002c0d17b0a9ee")));
            Tags.Add("nearly-dangling", new ExpectedTagInfo(new ObjectId("6e0c7bdb9b4ed93212491ee778ca1c65047cab4e")));
        }

        #region ExpectedTagInfo class

        /// <summary>
        ///   Helper class to contain expected info of tags in the test repository.
        /// </summary>
        public class ExpectedTagInfo
        {

            public bool IsAnnotated
            {
                get
                {
                    return AnnotationId != null;
                }
            }

            public ObjectId TargetId { get; set; }
            public ObjectId AnnotationId { get; set; }

            public ExpectedTagInfo(ObjectId targetId)
                : this(targetId, null)
            {
            }

            public ExpectedTagInfo(ObjectId targetId, ObjectId annotatedId)
            {
                TargetId = targetId;
                AnnotationId = annotatedId;
            }
        }

        #endregion
    }
}
