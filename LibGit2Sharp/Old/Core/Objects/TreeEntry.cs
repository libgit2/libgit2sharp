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
	unsafe public class TreeEntry
	{
        internal git_tree_entry *entry;
        
        internal TreeEntry(git_tree_entry *entry)
        {
            this.entry = entry;
        }
        
        public ObjectId ObjectId
        {
            get {
                return new ObjectId(NativeMethods.git_tree_entry_id(entry));
            }
        }
        
        public string Filename
        {
            get {
                return new string(NativeMethods.git_tree_entry_name(entry));
            }
        }
        
        public uint Attributes
        {
            get {
                return NativeMethods.git_tree_entry_attributes(entry);
            }
        }
        
        public GitObject GetObject(Repository repository)
        {
            git_object *obj = null;
            NativeMethods.git_tree_entry_2object(&obj, repository.repository, entry);

            if (obj == null)
                return null;
    
            return GitObject.Create(obj);
        }
        
        public T Get<T>(Repository repository) where T : GitObject
        {
            git_object *obj = null;
            NativeMethods.git_tree_entry_2object(&obj, repository.repository, entry);

            if (obj == null)
                return default(T);
    
            return GitObject.Create<T>(obj);
        }
	}
}
