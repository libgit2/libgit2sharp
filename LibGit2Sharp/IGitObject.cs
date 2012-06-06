using System;

namespace LibGit2Sharp
{
    public interface IGitObject : IEquatable<IGitObject>
    {
        /// <summary>
        ///   Gets the id of this object
        /// </summary>
        ObjectId Id { get; }

        /// <summary>
        ///   Gets the 40 character sha1 of this object.
        /// </summary>
        string Sha { get; }
    }
}