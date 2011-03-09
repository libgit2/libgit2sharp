/*
 * The MIT License
 *
 * Copyright (c) 2011 Emeric Fermas
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
