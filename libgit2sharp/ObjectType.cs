namespace libgit2net
{
    public enum ObjectType
    {
        Commit = 1,     /**< A commit object. */
        Tree = 2,       /**< A tree (directory listing) object. */
        Blob = 3,       /**< A file revision object. */
        Tag = 4,        /**< An annotated tag object. */
    }
}