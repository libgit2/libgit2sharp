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
    unsafe public class Tree : GitObject, IEnumerable<TreeEntry>
    {
        internal git_tree *tree = null;
    
        internal Tree(git_object *obj)
            : this((git_tree *)obj)
        {
        }
    
        internal Tree(git_tree *tree)
            : base((git_object *)tree)
        {
            this.tree = tree;
        }

        public uint TreeEntryCount
        {
            get {
                return NativeMethods.git_tree_entrycount(tree);
            }
        }
        
        public TreeEntry Get(uint index)
        {
            return Get(NativeMethods.git_tree_entry_byindex(tree, (int)index));

        }
        
        public TreeEntry Get(string filename)
        {
            return Get(NativeMethods.git_tree_entry_byname(tree, filename));
        }

        internal TreeEntry Get(git_tree_entry *treeEntry)
        {
            if (treeEntry == null)
                return null;

            return new TreeEntry(treeEntry);
        }
        
        public TreeEntry this[uint index]
        {
            get {
                return Get(index);
            }
        }
        
        public TreeEntry this[string filename]
        {
            get {
                return Get(filename);
            }
        }

        public IEnumerator<TreeEntry> GetEnumerator()
        {
            uint count = TreeEntryCount;
            for (uint i = 0; i < count; i++)
            {
                yield return Get(i);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public List<TreeEntry> Entries
        {
            get {
                List<TreeEntry> entries = new List<TreeEntry>();
                foreach (TreeEntry entry in this)
                    entries.Add(entry);
                return entries;
            }
        }
    }
}
