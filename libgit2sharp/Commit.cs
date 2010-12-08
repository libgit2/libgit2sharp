namespace libgit2sharp
{
    public class Commit : GitObject
    {
        public Person Author { get; private set; }
        public Person Committer { get; private set; }
        public ulong Time { get; private set; }
        public string Message { get; private set; }
        public string MessageShort { get; private set; }
        public Tree Tree { get; private set; }

        public Commit(string objectId, Person author, Person committer, ulong time, string message, string messageShort, Tree tree)
            : base(objectId, ObjectType.Commit)
        {
            Author = author;
            Committer = committer;
            Time = time;
            Message = message;
            MessageShort = messageShort;
            Tree = tree;
        }
    }
}