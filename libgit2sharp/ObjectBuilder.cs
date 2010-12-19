using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using libgit2sharp.Wrapper;

namespace libgit2sharp
{
    public class ObjectBuilder : IBuilder
    {
        private readonly IDictionary<ObjectType, Func<IntPtr, GitObject>> _builderMapper;

        public ObjectBuilder()
        {
            _builderMapper = InitMapper();
        }

        private IDictionary<ObjectType, Func<IntPtr, GitObject>> InitMapper()
        {
            var map =  new Dictionary<ObjectType, Func<IntPtr, GitObject>>();
            map.Add(ObjectType.Commit, BuildCommit);
            map.Add(ObjectType.Tag, BuildTag);
            map.Add(ObjectType.Tree, BuildTree);
            map.Add(ObjectType.Blob, BuildBlob);
            return map;
        }

        private GitObject BuildTag(IntPtr gitObjectPtr)
        {
            var gitTag = (git_tag)Marshal.PtrToStructure(gitObjectPtr, typeof(git_tag));
            
            Signature tagTagger = BuildSignature(gitTag.tagger);
            GitObject tagTarget = BuildFrom(gitTag.target, (ObjectType)gitTag.type);

            return new Tag(ObjectId.ToString(gitTag.tag.id.id), gitTag.tag_name, tagTarget, tagTagger, gitTag.message);
        }

        private static GitObject BuildCommit(IntPtr gitObjectPtr)
        {
            var gitCommit = (git_commit)Marshal.PtrToStructure(gitObjectPtr, typeof(git_commit));

            Signature commitAuthor = BuildSignature(gitCommit.author);
            Signature commitCommitter = BuildSignature(gitCommit.committer);
            var commitTree = (Tree)BuildTree(gitCommit.tree);

            return new Commit(ObjectId.ToString(gitCommit.commit.id.id), commitAuthor, commitCommitter, gitCommit.message, gitCommit.message_short, commitTree);
        }

        private static GitObject BuildTree(IntPtr gitObjectPtr)
        {
            var gitTree = (git_tree)Marshal.PtrToStructure(gitObjectPtr, typeof(git_tree));
            return new Tree(ObjectId.ToString(gitTree.tree.id.id));
        }


        private static GitObject BuildBlob(IntPtr gitObjectPtr)
        {
            throw new NotImplementedException();
            //var gitBlob = (git_blob)Marshal.PtrToStructure(gitObjectPtr, typeof(git_blob));
            //return new Blob(...)
        }

        private static Signature BuildSignature(IntPtr gitObjectPtr)
        {
            if (gitObjectPtr == IntPtr.Zero)
            {
                return null; // TODO: Fix full parsing of commits.
            }

            var gitPerson = (git_signature)Marshal.PtrToStructure(gitObjectPtr, typeof(git_signature));
            return new Signature(gitPerson.name, gitPerson.email, (DateTimeOffset)new GitDate((int)gitPerson.time, gitPerson.offset));
        }

        private static GitObject BuildGitObject(IntPtr gitObjectPtr)
        {
            var gitObject = (git_object)Marshal.PtrToStructure(gitObjectPtr, typeof(git_object));
            return new GitObject(ObjectId.ToString(gitObject.id.id), (ObjectType)gitObject.source.raw.type);
        }

        public GitObject BuildFrom(IntPtr gitObjectPtr, ObjectType type)
        {
            if (gitObjectPtr == IntPtr.Zero)
            {
                throw new ArgumentNullException("gitObjectPtr");
            }

            if (!_builderMapper.Keys.Contains(type))
            {
                return BuildGitObject(gitObjectPtr);
            }

            return _builderMapper[type](gitObjectPtr);
        }
    }
}