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
        /// Input already exists in the processed scope.
        /// </summary>
        Exists = -4,

        /// <summary>
        /// The given short oid is ambiguous.
        /// </summary>
        Ambiguous = -5,

        /// <summary>
        /// Buffer related issue.
        /// </summary>
        Buffer = -6,

        /// <summary>
        /// Callback error.
        /// </summary>
        User = -7,

        /// <summary>
        /// Operation cannot be performed against a bare repository.
        /// </summary>
        BareRepo = -8,

        /// <summary>
        /// Operation cannot be performed against an orphaned HEAD.
        /// </summary>
        OrphanedHead = -9,

        /// <summary>
        /// Operation cannot be performed against a not fully merged index.
        /// </summary>
        UnmergedEntries = -10,

        /// <summary>
        /// Push cannot be performed against the remote without losing commits.
        /// </summary>
        NonFastForward = -11,

        /// <summary>
        /// Input is not a valid specification.
        /// </summary>
        InvalidSpecification = -12,

        /// <summary>
        /// A conflicting change has been detected in the index
        /// or working directory.
        /// </summary>
        Conflict = -13,

        /// <summary>
        /// A file operation failed because the file was locked.
        /// </summary>
        LockedFile = -14,

        /// <summary>
        /// Reference value does not match expected.
        /// </summary>
        Modified = -15,

        /// <summary>
        /// Authentication error.
        /// </summary>
        Auth = -16,

        /// <summary>
        /// Server certificate is invalid.
        /// </summary>
        Certificate = -17,

        /// <summary>
        /// Patch/merge has already been applied.
        /// </summary>
        Applied = -18,

        /// <summary>
        /// The requested peel operation is not possible.
        /// </summary>
        Peel = -19,

        /// <summary>
        /// Unexpected EOF.
        /// </summary>
        EndOfFile = -20,

        /// <summary>
        /// Invalid operation or input.
        /// </summary>
        Invalid = -21,

        /// <summary>
        /// Uncommitted changes in index prevented operation.
        /// </summary>
        Uncommitted = -22,

        /// <summary>
        /// Skip and passthrough the given ODB backend.
        /// </summary>
        PassThrough = -30,

        /// <summary>
        /// There are no more entries left to iterate.
        /// </summary>
        IterOver = -31,
    }
}
