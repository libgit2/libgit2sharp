namespace libgit2sharp.Wrapper
{
    internal enum git_otype
    {
        GIT_OBJ_ANY = -2,		/**< Object can be any of the following */
        GIT_OBJ_BAD = -1,       /**< Object is invalid. */
        GIT_OBJ__EXT1 = 0,      /**< Reserved for future use. */
        GIT_OBJ_COMMIT = 1,     /**< A commit object. */
        GIT_OBJ_TREE = 2,       /**< A tree (directory listing) object. */
        GIT_OBJ_BLOB = 3,       /**< A file revision object. */
        GIT_OBJ_TAG = 4,        /**< An annotated tag object. */
        GIT_OBJ__EXT2 = 5,      /**< Reserved for future use. */
        GIT_OBJ_OFS_DELTA = 6,  /**< A delta, base is given by an offset. */
        GIT_OBJ_REF_DELTA = 7,  /**< A delta, base is given by object id. */
    }
}