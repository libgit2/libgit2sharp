namespace LibGit2Sharp.Core
{
    public enum GitErrorCode
    {
        Ok = 0,
        Error = -1,

        /// <summary>
        ///   Input does not exist in the scope searched.
        /// </summary>
        NotFound = -3,

        /// <summary>
        ///   Input already exists in the processed scope.
        /// </summary>
        Exists = -4,

        /// <summary>
        ///   The given short oid is ambiguous.
        /// </summary>
        Ambiguous = -5,

        /// <summary>
        ///   Buffer related issue.
        /// </summary>
        Buffer = -6,

        /// <summary>
        ///   Skip and passthrough the given ODB backend.
        /// </summary>
        PassThrough = -30,

        /// <summary>
        ///   The revision walker is empty; there are no more commits left to iterate.
        /// </summary>
        RevWalkOver = -31,
    }
}
