namespace libgit2sharp.Wrapper
{
    internal enum OperationResult
    {
        /** Operation completed successfully. */
        GIT_SUCCESS = 0,

        /**
         * Operation failed, with unspecified reason.
         * This value also serves as the base error code; all other
         * error codes are subtracted from it such that all errors
         * are < 0, in typical POSIX C tradition.
         */
        GIT_ERROR = Constants.GIT_ERROR,

        /** Input was not a properly formatted Git object id. */
        GIT_ENOTOID = (Constants.GIT_ERROR - 1),

        /** Input does not exist in the scope searched. */
        GIT_ENOTFOUND = (Constants.GIT_ERROR - 2),

        /** Not enough space available. */
        GIT_ENOMEM = (Constants.GIT_ERROR - 3),

        /** Consult the OS error information. */
        GIT_EOSERR = (Constants.GIT_ERROR - 4),

        /** The specified object is of invalid type */
        GIT_EOBJTYPE = (Constants.GIT_ERROR - 5),

        /** The specified object has its data corrupted */
        GIT_EOBJCORRUPTED = (Constants.GIT_ERROR - 6),

        /** The specified repository is invalid */
        GIT_ENOTAREPO = (Constants.GIT_ERROR - 7),

        /** The object type is invalid or doesn't match */
        GIT_EINVALIDTYPE = (Constants.GIT_ERROR - 8),

        /** The object cannot be written that because it's missing internal data */
        GIT_EMISSINGOBJDATA = (Constants.GIT_ERROR - 9),

        /** The packfile for the ODB is corrupted */
        GIT_EPACKCORRUPTED = (Constants.GIT_ERROR - 10),

        /** Failed to adquire or release a file lock */
        GIT_EFLOCKFAIL = (Constants.GIT_ERROR - 11),

        /** The Z library failed to inflate/deflate an object's data */
        GIT_EZLIB = (Constants.GIT_ERROR - 12),

        /** The queried object is currently busy */
        GIT_EBUSY = (Constants.GIT_ERROR - 13),

        /** The index file is not backed up by an existing repository */
        GIT_EBAREINDEX = (Constants.GIT_ERROR - 14),
    }
}