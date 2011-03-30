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
    unsafe public class IndexEntry
    {
        internal git_index_entry *index_entry = null;
    
        internal IndexEntry(git_index_entry *index_entry)
        {
            this.index_entry = index_entry;
        }
    
        public string Path
        {
            get {
                return new string(index_entry->path);
            }
        }
    
        public long FileSize
        {
            get {
                return index_entry->file_size2 << sizeof(int) | index_entry->file_size1;
            }
        }

        public DateTime MTime
        {
            get {
                return index_entry->mtime.ToDateTime();
            }
        }

        public DateTime CTime
        {
            get {
                return index_entry->ctime.ToDateTime();
            }
        }
    }
}
