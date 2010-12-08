namespace libgit2sharp
{
    public class GitObject
    {
        public GitObject(string objectId, ObjectType type)
        {
            Id = objectId;
            Type = type;
        }

        public object Id { get; private set; }
        public ObjectType Type { get; private set; }
    }
}