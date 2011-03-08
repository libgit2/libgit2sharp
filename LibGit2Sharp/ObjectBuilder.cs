using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using LibGit2Sharp.Wrapper;

namespace LibGit2Sharp
{
    public class ObjectBuilder : IBuilder
    {
        private readonly IDictionary<ObjectType, Func<Core.GitObject, GitObject>> _builderMapper;

        public ObjectBuilder()
        {
            _builderMapper = InitMapper();
        }

        private IDictionary<ObjectType, Func<Core.GitObject, GitObject>> InitMapper()
        {
            var map =  new Dictionary<ObjectType, Func<Core.GitObject, GitObject>>();
            map.Add(ObjectType.Commit, BuildCommit);
            map.Add(ObjectType.Tag, BuildTag);
            map.Add(ObjectType.Tree, BuildTree);
            map.Add(ObjectType.Blob, BuildBlob);
            return map;
        }

        private GitObject BuildTag(Core.GitObject gitObject)
        {
            var coreTag = gitObject as Core.Tag;
            
            Signature tagTagger = BuildSignature(coreTag.Tagger);
            GitObject tagTarget = BuildFrom(coreTag.Target);
            
            return new Tag(coreTag.ObjectId.ToString(), coreTag.Name, tagTarget, tagTagger, coreTag.Message);
        }

        private static GitObject BuildCommit(Core.GitObject gitObject)
        {
            Core.Commit commit = gitObject as Core.Commit;
            Signature commitAuthor = BuildSignature(commit.Author);
            Signature commitCommitter = BuildSignature(commit.Committer);
            Tree commitTree = (Tree)BuildTree(commit.Tree);
            // TODO: Do we really have to read all the commit parents, like the
            // old code did? Take the linux repo, and you are will be loading stuff
            // for ours. Last argument is now an empty list, it has to be changed
            // after making a decision.
            return new Commit(gitObject.ObjectId.ToString(), commitAuthor, commitCommitter, commit.Message, commit.MessageShort, commitTree, new List<GitObject>());
        }

        private static GitObject BuildTree(Core.GitObject gitObject)
        {
            return new Tree((gitObject as Core.Tree).ObjectId.ToString());
        }


        private static GitObject BuildBlob(Core.GitObject gitObjectPtr)
        {
            throw new NotImplementedException();
        }

        private static Signature BuildSignature(Core.Signature sig)
        {
            if (sig == null)
            {
                return null; // TODO: Fix full parsing of commits.
            }
            
            return new Signature(sig.Name, sig.Email, new GitDate((int)sig.Time, sig.Offset));
        }

        private static GitObject BuildGitObject(Core.GitObject gitObject)
        {
            return new GitObject(gitObject.ObjectId.ToString(), (ObjectType)gitObject.Type);
        }

        public GitObject BuildFrom(Core.GitObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            ObjectType type = (ObjectType)obj.Type;
            
            if (!_builderMapper.Keys.Contains(type))
            {
                return BuildGitObject(obj);
            }

            return _builderMapper[type](obj);
        }
    }
}