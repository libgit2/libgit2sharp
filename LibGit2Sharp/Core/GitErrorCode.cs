namespace LibGit2Sharp.Core
{
    internal enum GitErrorCode
    {
        Ok = 0,
        Error = -1,

        /// <summary>
        /// Input does not exist in the scope searched.
        /// </summary>
        NotFound = -3,

        /// <summary>
        /// A reference with this name already exists.
        /// </summary>
        Exists = -4,

        /// <summary>
        /// The given short oid is ambiguous.
        /// </summary>
        Ambiguous = -5,

        /// <summary>
        /// Bufs
        /// </summary>
        Bufs = -6,

        /// <summary>
        /// Skip and passthrough the given ODB backend.
        /// </summary>
        PassThrough = -30,

        /// <summary>
        /// The revision walker is empty; there are no more commits left to iterate.
        /// </summary>
        RevWalkOver = -31
    }
}
