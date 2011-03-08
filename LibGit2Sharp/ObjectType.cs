using LibGit2Sharp.Wrapper;

namespace LibGit2Sharp
{
    public enum ObjectType
    {
        Commit = Core.git_otype.GIT_OBJ_COMMIT, // A commit object.
        Tree   = Core.git_otype.GIT_OBJ_TREE,   // A tree (directory listing) object.
        Blob   = Core.git_otype.GIT_OBJ_BLOB,   // A file revision object.
        Tag    = Core.git_otype.GIT_OBJ_TAG,    // An annotated tag object.
    }
}