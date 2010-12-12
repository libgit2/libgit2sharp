using System;

namespace libgit2sharp
{
    public class Commit : GitObject
    {
        public Signature Author { get; private set; }
        public Signature Committer { get; private set; }
        public DateTimeOffset when { get; private set; }
        public string Message { get; private set; }
        public string MessageShort { get; private set; }
        public Tree Tree { get; private set; }

        public Commit(string objectId, Signature author, Signature committer, string message, string messageShort, Tree tree)
            : base(objectId, ObjectType.Commit)
        {
            Author = author;
            Committer = committer;
            when = committer.When;
            Message = message;
            MessageShort = messageShort;
            Tree = tree;
        }
    }
}