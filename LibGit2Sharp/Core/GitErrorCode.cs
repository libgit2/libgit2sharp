namespace LibGit2Sharp.Core
{


    internal enum GitErrorCode
    {
        /// <summary>
        ///   Operation completed successfully.
        /// </summary>
        GIT_SUCCESS = 0,

        /// <summary>
        ///   Operation failed, with unspecified reason.
        ///   This value also serves as the base error code; all other
        ///   error codes are subtracted from it such that all errors
        ///   are &lt; 0, in typical POSIX C tradition.
        /// </summary>
        GIT_ERROR = -1,

        /// <summary>
        ///   Input does not exist in the scope searched.
        /// </summary>
        GIT_ENOTFOUND = -3,

        /// <summary>
        ///   A reference with this name already exists
        /// </summary>
        GIT_EEXISTS = -23,

        /// <summary>
        ///   The given integer literal is too large to be parsed
        /// </summary>
        GIT_EOVERFLOW = -24,

        /// <summary>
        ///   The given short oid is ambiguous
        /// </summary>
        GIT_EAMBIGUOUS = -29,

        /// <summary>
        ///   Skip and passthrough the given ODB backend
        /// </summary>
        GIT_EPASSTHROUGH = -30,
        
        /// <summary>
        ///   The buffer is too short to satisfy the request
        /// </summary>
        GIT_ESHORTBUFFER = -32,

        /// <summary>
        ///   The revision walker is empty; there are no more commits left to iterate
        /// </summary>
        GIT_EREVWALKOVER = -33,
    }
}
