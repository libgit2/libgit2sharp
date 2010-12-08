namespace libgit2sharp
{
    public class Tree : GitObject
    {
        public Tree(string objectId)
            : base(objectId, ObjectType.Tree)
        {
        }
    }
}