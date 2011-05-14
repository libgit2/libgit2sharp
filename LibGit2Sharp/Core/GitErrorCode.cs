namespace LibGit2Sharp.Core
{
    internal enum GitErrorCode
    {
        /// <summary>
        /// Operation completed successfully.
        /// </summary>
        GIT_SUCCESS = 0,

        /// <summary>
        /// Operation failed, with unspecified reason.
        /// This value also serves as the base error code; all other
        /// error codes are subtracted from it such that all errors
        /// are &lt; 0, in typical POSIX C tradition.
        /// </summary>
        GIT_ERROR = -1,

        /// <summary>
        /// Input was not a properly formatted Git object id.
        /// </summary>
        GIT_ENOTOID = (GIT_ERROR - 1),

        /// <summary>
        /// Input does not exist in the scope searched.
        /// </summary>
        GIT_ENOTFOUND = (GIT_ERROR - 2),

        /// <summary>
        /// Not enough space available.
        /// </summary>
        GIT_ENOMEM = (GIT_ERROR - 3),

        /// <summary>
        /// Consult the OS error information.
        /// </summary>
        GIT_EOSERR = (GIT_ERROR - 4),

        /// <summary>
        /// The specified object is of invalid type
        /// </summary>
        GIT_EOBJTYPE = (GIT_ERROR - 5),

        /// <summary>
        /// The specified object has its data corrupted
        /// </summary>
        GIT_EOBJCORRUPTED = (GIT_ERROR - 6),

        /// <summary>
        /// The specified repository is invalid
        /// </summary>
        GIT_ENOTAREPO = (GIT_ERROR - 7),

        /// <summary>
        /// The object type is invalid or doesn't match
        /// </summary>
        GIT_EINVALIDTYPE = (GIT_ERROR - 8),

        /// <summary>
        /// The object cannot be written that because it's missing internal data
        /// </summary>
        GIT_EMISSINGOBJDATA = (GIT_ERROR - 9),

        /// <summary>
        /// The packfile for the ODB is corrupted
        /// </summary>
        GIT_EPACKCORRUPTED = (GIT_ERROR - 10),

        /// <summary>
        /// Failed to adquire or release a file lock
        /// </summary>
        GIT_EFLOCKFAIL = (GIT_ERROR - 11),

        /// <summary>
        /// The Z library failed to inflate/deflate an object's data
        /// </summary>
        GIT_EZLIB = (GIT_ERROR - 12),

        /// <summary>
        /// The queried object is currently busy
        /// </summary>
        GIT_EBUSY = (GIT_ERROR - 13),

        /// <summary>
        /// The index file is not backed up by an existing repository
        /// </summary>
        GIT_EBAREINDEX = (GIT_ERROR - 14),

        /// <summary>
        /// The name of the reference is not valid
        /// </summary>
        GIT_EINVALIDREFNAME = (GIT_ERROR - 15),

        /// <summary>
        /// The specified reference has its data corrupted
        /// </summary>
        GIT_EREFCORRUPTED = (GIT_ERROR - 16),

        /// <summary>
        /// The specified symbolic reference is too deeply nested
        /// </summary>
        GIT_ETOONESTEDSYMREF = (GIT_ERROR - 17),

        /// <summary>
        /// The pack-refs file is either corrupted of its format is not currently supported
        /// </summary>
        GIT_EPACKEDREFSCORRUPTED = (GIT_ERROR - 18),

        /// <summary>
        /// The path is invalid
        /// </summary>
        GIT_EINVALIDPATH = (GIT_ERROR - 19),

        /// <summary>
        /// The revision walker is empty; there are no more commits left to iterate
        /// </summary>
        GIT_EREVWALKOVER = (GIT_ERROR - 20),

        /// <summary>
        /// The state of the reference is not valid
        /// </summary>
        GIT_EINVALIDREFSTATE = (GIT_ERROR - 21),

        /// <summary>
        /// This feature has not been implemented yet
        /// </summary>
        GIT_ENOTIMPLEMENTED = (GIT_ERROR - 22),

        /// <summary>
        /// A reference with this name already exists
        /// </summary>
        GIT_EEXISTS = (GIT_ERROR - 23),

        /// <summary>
        /// The given integer literal is too large to be parsed
        /// </summary>
        GIT_EOVERFLOW = (GIT_ERROR - 24),

        /// <summary>
        /// The given literal is not a valid number
        /// </summary>
        GIT_ENOTNUM = (GIT_ERROR - 25),

        /// <summary>
        /// Streaming error
        /// </summary>
        GIT_ESTREAM = (GIT_ERROR - 26),

        /// <summary>
        /// invalid arguments to function
        /// </summary>
        GIT_EINVALIDARGS = (GIT_ERROR - 27),
    }
}