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
    unsafe public class Index : IEnumerable<IndexEntry>, IDisposable
    {
        private git_index *index = null;
    
        internal Index(git_index *index)
        {
            this.index = index;
        }

        public Index(string indexPath)
        {
            int ret;
            fixed (git_index **pindex = &index)
            {
                ret = NativeMethods.git_index_open_bare(pindex, indexPath);
            }
            GitError.Check(ret);
        }
        
        public uint Count
        {
            get {
                return NativeMethods.git_index_entrycount(index);
            }
        }
        
        public void Read()
        {
            NativeMethods.git_index_read(index);
        }
        
        public void Write()
        {
            NativeMethods.git_index_write(index);
        }
        
        public Repository Repository
        {
            get {
                if (index->repository == null)
                    return null;
    
                return new Repository(index->repository);
            }
        }
    
        public IndexEntry Get(uint n)
        {
            git_index_entry *entry = NativeMethods.git_index_get(index, (int)n);
    
            if (entry == null)
                return null;
    
            return new IndexEntry(entry);
        }
    
        public IndexEntry this[uint index]
        {
            get {
                return Get(index);
            }
        }
    
        public int Get(string path)
        {
            return NativeMethods.git_index_find(index, path);
        }
        
        public int this[string path]
        {
            get {
                return Get(path);
            }
        }
        
        public string FilePath
        {
            get {
                return new string(index->index_file_path);
            }
        }
    
        public void Clear()
        {
            NativeMethods.git_index_clear(index);
        }
    
        public void Add(string path, int stage)
        {
            int ret = NativeMethods.git_index_add(index, path, stage);
            GitError.Check(ret);
        }
    
        public void Remove(int position)
        {
            int ret = NativeMethods.git_index_remove(index, position);
            GitError.Check(ret);
        }
    
        public void Insert(IndexEntry indexEntry)
        {
            int ret = NativeMethods.git_index_insert(index, indexEntry.index_entry);
            GitError.Check(ret);
        }
    
        #region IEnumerable implementation
        public IEnumerator<IndexEntry> GetEnumerator()
        {
            uint count = Count;
            for (uint i = 0; i < count; i++)
            {
                yield return Get(i);
            }
        }
    
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)this.GetEnumerator();
        }
        #endregion
    
        #region IDisposable implementation
        public void Dispose()
        {
            if (index != null)
                NativeMethods.git_index_free(index);
        }
        #endregion
    }
}
