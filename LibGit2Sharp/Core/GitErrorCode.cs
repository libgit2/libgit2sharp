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
        GIT_ENOTOID = -2,

        /// <summary>
        /// Input does not exist in the scope searched.
        /// </summary>
        GIT_ENOTFOUND = -3,

        /// <summary>
        /// Not enough space available.
        /// </summary>
        GIT_ENOMEM = -4,

        /// <summary>
        /// Consult the OS error information.
        /// </summary>
        GIT_EOSERR = -5,

        /// <summary>
        /// The specified object is of invalid type
        /// </summary>
        GIT_EOBJTYPE = -6,

        /// <summary>
        /// The specified repository is invalid
        /// </summary>
        GIT_ENOTAREPO = -7,

        /// <summary>
        /// The object type is invalid or doesn't match
        /// </summary>
        GIT_EINVALIDTYPE = -8,

        /// <summary>
        /// The object cannot be written that because it's missing internal data
        /// </summary>
        GIT_EMISSINGOBJDATA = -9,

        /// <summary>
        /// The packfile for the ODB is corrupted
        /// </summary>
        GIT_EPACKCORRUPTED = -10,

        /// <summary>
        /// Failed to adquire or release a file lock
        /// </summary>
        GIT_EFLOCKFAIL = -11,

        /// <summary>
        /// The Z library failed to inflate/deflate an object's data
        /// </summary>
        GIT_EZLIB = -12,

        /// <summary>
        /// The queried object is currently busy
        /// </summary>
        GIT_EBUSY = -13,

        /// <summary>
        /// The index file is not backed up by an existing repository
        /// </summary>
        GIT_EBAREINDEX = -14,

        /// <summary>
        /// The name of the reference is not valid
        /// </summary>
        GIT_EINVALIDREFNAME = -15,

        /// <summary>
        /// The specified reference has its data corrupted
        /// </summary>
        GIT_EREFCORRUPTED = -16,

        /// <summary>
        /// The specified symbolic reference is too deeply nested
        /// </summary>
        GIT_ETOONESTEDSYMREF = -17,

        /// <summary>
        /// The pack-refs file is either corrupted of its format is not currently supported
        /// </summary>
        GIT_EPACKEDREFSCORRUPTED = -18,

        /// <summary>
        /// The path is invalid
        /// </summary>
        GIT_EINVALIDPATH = -19,

        /// <summary>
        /// The revision walker is empty; there are no more commits left to iterate
        /// </summary>
        GIT_EREVWALKOVER = -20,

        /// <summary>
        /// The state of the reference is not valid
        /// </summary>
        GIT_EINVALIDREFSTATE = -21,

        /// <summary>
        /// This feature has not been implemented yet
        /// </summary>
        GIT_ENOTIMPLEMENTED = -22,

        /// <summary>
        /// A reference with this name already exists
        /// </summary>
        GIT_EEXISTS = -23,

        /// <summary>
        /// The given integer literal is too large to be parsed
        /// </summary>
        GIT_EOVERFLOW = -24,

        /// <summary>
        /// The given literal is not a valid number
        /// </summary>
        GIT_ENOTNUM = -25,

        /// <summary>
        /// Streaming error
        /// </summary>
        GIT_ESTREAM = -26,

        /// <summary>
        /// invalid arguments to function
        /// </summary>
        GIT_EINVALIDARGS = -27,

        /// <summary>
        /// The specified object has its data corrupted
        /// </summary>
        GIT_EOBJCORRUPTED = -28,
    }
}