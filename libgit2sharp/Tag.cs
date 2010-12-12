namespace libgit2sharp
{
    public class Tag : GitObject
    {
        public Tag(string objectId, string name, GitObject target, Signature tagger, string message)
            : base(objectId, ObjectType.Tag)
        {
            Name = name;
            Target = target;
            Tagger = tagger;
            Message = message;
        }

        public string Name { get; private set; }
        public string Message { get; private set; }
        public Signature Tagger { get; private set; }
        public GitObject Target { get; private set; }
    }
}