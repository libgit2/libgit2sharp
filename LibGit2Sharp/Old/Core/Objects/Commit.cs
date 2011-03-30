/*
 * The MIT License
 *
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
using System.Collections;
using System.Collections.Generic;

namespace LibGit2Sharp.Core
{
    unsafe public class Commit : GitObject, IEnumerable<Commit>
    {
        internal git_commit *commit;
    
        internal Commit(git_object *obj)
            : this((git_commit *)obj)
        {
        }
    
        internal Commit(git_commit *commit)
            : base((git_object *)commit)
        {
            this.commit = commit;
        }
    
        public static ObjectId Create(Repository repository,
                               string updateReference,
                               Signature author,
                               Signature committer,
                               string message,
                               Tree tree)
        {
            ObjectId oid = new ObjectId();


            ObjectId treeoid = tree.ObjectId;

            int ret = NativeMethods.git_commit_create(&oid.oid,
                                                      repository.repository,
                                                      updateReference,
                                                      author.signature,
                                                      committer.signature,
                                                      message,
                                                      &treeoid.oid,
                                                      0,
                                                      null);

            GitError.Check(ret);

            return oid;
        }
    
        public string MessageShort
        {
            get {
                return new string(NativeMethods.git_commit_message_short(commit));
            }
        }

        public string Message
        {
            get {
                return new string(NativeMethods.git_commit_message(commit));
            }
        }
        
        public uint ParentCount
        {
            get {
                return NativeMethods.git_commit_parentcount(commit);
            }
        }
        
        public Commit GetParent(uint n)
        {
            git_commit *parent = null;
            int ret = NativeMethods.git_commit_parent(&parent, commit, n);
            GitError.Check(ret);

            if (parent == null)
                return null;
    
            return new Commit(parent);
        }
        
        public IEnumerator<Commit> GetEnumerator()
        {
            uint count = ParentCount;
            for (uint i = 0; i < count; i++)
            {
                yield return GetParent(i);
            }
        }
    
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public List<Commit> Parents
        {
            get {
                List<Commit> commits = new List<Commit>();
                foreach (Commit commit in this)
                    commits.Add(commit);
                return commits;
            }
        }
        
        public Tree Tree
        {
            get {
                git_tree *tree = null;
                int ret = NativeMethods.git_commit_tree(&tree, commit);
                GitError.Check(ret);

                if (tree == null)
                    return null;

                return new Tree(tree);
            }
        }
    
        public Signature Author
        {
            get {
                return new Signature(NativeMethods.git_commit_author(commit));
            }
        }
    
        public Signature Committer
        {
            get {
                return new Signature(NativeMethods.git_commit_committer(commit));
            }
        }
    }
}
