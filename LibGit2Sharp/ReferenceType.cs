using System;

namespace LibGit2Sharp
{
    /// <summary>
    ///  Underlying type of a <see cref="Reference"/>
    /// </summary>
    public enum ReferenceType
    {
        /// <summary>
        ///  An invalid reference type.
        /// </summary>
        Invalid = 0,

        /// <summary>
        ///  A direct reference, the target is an object ID.
        /// </summary>
        Oid = 1,

        /// <summary>
        ///  A symbolic reference, the target is another reference.
        /// </summary>
        Symbolic = 2,
    }
}
