/*
 * The MIT License
 *
 * Copyright (c) 2011 LibGit2Sharp committers
 * Copyright (c) 2011 Andrius Bentkus
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class ObjectBuilder
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
            var coreTag = (Core.Tag)gitObject;
            
            Signature tagTagger = BuildSignature(coreTag.Tagger);
            GitObject tagTarget = BuildFrom(coreTag.Target);
            
            return new Tag(coreTag.ObjectId.ToString(), coreTag.Name, tagTarget, tagTagger, coreTag.Message);
        }

        private static GitObject BuildCommit(Core.GitObject gitObject)
        {
            var commit = (Core.Commit)gitObject;
            Signature commitAuthor = BuildSignature(commit.Author);
            Signature commitCommitter = BuildSignature(commit.Committer);
            var commitTree = (Tree)BuildTree(commit.Tree);

            var list = commit.Parents.Select(BuildGitObject).ToList();

            return new Commit(gitObject.ObjectId.ToString(), commitAuthor, commitCommitter, commit.Message, commit.MessageShort, commitTree, list);
        }

        private static GitObject BuildTree(Core.GitObject gitObject)
        {
            return new Tree(gitObject.ObjectId.ToString());
        }

        private static GitObject BuildBlob(Core.GitObject gitObjectPtr)
        {
            throw new NotImplementedException();
        }

        private static Signature BuildSignature(Core.Signature sig)
        {           
            return new Signature(sig.Name, sig.Email, sig.When);
        }

        private static GitObject BuildGitObject(Core.GitObject gitObject)
        {
            return new GitObject(gitObject.ObjectId.ToString(), (ObjectType)gitObject.Type);
        }

        public unsafe GitObject BuildFrom(Core.GitObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            var type = (ObjectType)obj.Type;

            Func<Core.GitObject, GitObject> builder = BuildGitObject;
            
            if (_builderMapper.Keys.Contains(type))
            {
                builder = _builderMapper[type];
            }

            GitObject gitObject = builder(obj);
            
            NativeMethods.git_object_close(obj.obj);
            
            return gitObject;
        }
    }
}
