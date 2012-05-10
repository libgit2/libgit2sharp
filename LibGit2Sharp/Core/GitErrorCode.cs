namespace LibGit2Sharp.Core
{
   internal enum GitErrorCode
    {
        Success = 0,
        Error = -1,
     
        /// <summary>
        /// Input does not exist in the scope searched.
        /// </summary>
        NotFound = -3,

        /// <summary>
        /// A reference with this name already exists.
        /// </summary>
        Exists = -23,

        /// <summary>
        /// The given integer literal is too large to be parsed.
        /// </summary>
        Overflow = -24,

        /// <summary>
        /// The given short oid is ambiguous.
        /// </summary>
        Ambiguous = -29,

        /// <summary>
        /// Skip and passthrough the given ODB backend.
        /// </summary>
        PassThrough = -30,

        /// <summary>
        /// The buffer is too short to satisfy the request.
        /// </summary>
        ShortBuffer = -32,

        /// <summary>
        /// The revision walker is empty; there are no more commits left to iterate.
        /// </summary>
        RevWalkOver = -33
    }
}
