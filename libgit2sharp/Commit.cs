using System;
using System.Collections.Generic;

namespace libgit2sharp
{
    public class Commit : GitObject
    {
        public IEnumerable<GitObject> Parents { get; private set; }
        public Signature Author { get; private set; }
        public Signature Committer { get; private set; }
        public DateTimeOffset When { get; private set; }
        public string Message { get; private set; }
        public string MessageShort { get; private set; }
        public Tree Tree { get; private set; }

        public Commit(string objectId, Signature author, Signature committer, string message, string messageShort, Tree tree, IEnumerable<GitObject> parents)
            : base(objectId, ObjectType.Commit)
        {
            Parents = parents;
            Author = author;
            Committer = committer;
            When = committer.When;
            Message = message;
            MessageShort = messageShort;
            Tree = tree;
        }
    }
}