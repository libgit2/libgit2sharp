namespace LibGit2Sharp
{
    public class Tree : GitObject
    {
        public Tree(string objectId)
            : base(objectId, ObjectType.Tree)
        {
        }
    }
}