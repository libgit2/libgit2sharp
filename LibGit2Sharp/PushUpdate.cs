using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    /// Represents an update which will be performed on the remote during push
    /// </summary>
    public class PushUpdate
    {
        internal PushUpdate(string srcRefName, ObjectId srcOid, string dstRefName, ObjectId dstOid)
        {
            DestinationObjectId = dstOid;
            DestinationRefName = dstRefName;
            SourceObjectId = srcOid;
            SourceRefName = srcRefName;
        }
        internal PushUpdate(GitPushUpdate update)
        {
            DestinationObjectId = update.dst;
            DestinationRefName = LaxUtf8Marshaler.FromNative(update.dst_refname);
            SourceObjectId = update.src;
            SourceRefName = LaxUtf8Marshaler.FromNative(update.src_refname);
        }
        /// <summary>
        /// Empty constructor to support test suites
        /// </summary>
        protected PushUpdate()
        {
            DestinationObjectId = ObjectId.Zero;
            DestinationRefName = String.Empty;
            SourceObjectId = ObjectId.Zero;
            SourceRefName = String.Empty;
        }

        /// <summary>
        /// The source name of the reference
        /// </summary>
        public readonly string SourceRefName;
        /// <summary>
        /// The name of the reference to update on the server
        /// </summary>
        public readonly string DestinationRefName;
        /// <summary>
        /// The current target of the reference
        /// </summary>
        public readonly ObjectId SourceObjectId;
        /// <summary>
        /// The new target for the reference
        /// </summary>
        public readonly ObjectId DestinationObjectId;
    }
}
