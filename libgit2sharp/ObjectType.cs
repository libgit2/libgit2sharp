using libgit2sharp.Wrapper;

namespace libgit2sharp
{
    public enum ObjectType
    {
        Commit = git_otype.GIT_OBJ_COMMIT,     /**< A commit object. */
        Tree = git_otype.GIT_OBJ_TREE,       /**< A tree (directory listing) object. */
        Blob = git_otype.GIT_OBJ_BLOB,       /**< A file revision object. */
        Tag = git_otype.GIT_OBJ_TAG,        /**< An annotated tag object. */
    }
}