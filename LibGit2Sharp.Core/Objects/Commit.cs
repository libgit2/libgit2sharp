/*
 * This file is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License, version 2,
 * as published by the Free Software Foundation.
 *
 * In addition to the permissions in the GNU General Public License,
 * the authors give you unlimited permission to link the compiled
 * version of this file into combinations with other programs,
 * and to distribute those combinations without any restriction
 * coming from the use of this file.  (The General Public License
 * restrictions do apply in other respects; for example, they cover
 * modification of the file, and distribution when not linked into
 * a combined executable.)
 *
 * This file is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 51 Franklin Street, Fifth Floor,
 * Boston, MA 02110-1301, USA.
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
    
        public Commit(Repository repository)
            : base(repository, git_otype.GIT_OBJ_COMMIT)
        {
            commit = (git_commit *)obj;
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
                return new string(NativeMethods.git_commit_message_short(commit));
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
            git_commit *commit = NativeMethods.git_commit_parent(this.commit, n);

            if (commit == null)
                return null;
    
            return new Commit(commit);
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
        
        public Tree Tree
        {
            get {
                git_tree *tree = NativeMethods.git_commit_tree(commit);
    
                if (tree == null)
                    return null;

                return new Tree(tree);
            }
        }
    
        public Signature Author
        {
            get {
                return new Signature(commit->author);
            }
        }
    
        public Signature Committer
        {
            get {
                return new Signature(commit->committer);
            }
        }
    }
}
