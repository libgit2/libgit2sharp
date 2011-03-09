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
    unsafe public class Blob : GitObject
    {
        internal git_blob *blob;

        internal Blob(git_object *obj)
            : this((git_blob *)obj)
        {
        }

        internal Blob(git_blob *blob)
            : base((git_object *)blob)
        {
            this.blob = blob;
        }

        public Blob(Repository repository)
            : base(repository, git_otype.GIT_OBJ_BLOB)
        {
            this.blob = (git_blob *)obj;
        }

        public void SetRawContentFromFile(string filename)
        {
            int ret = NativeMethods.git_blob_set_rawcontent_fromfile(blob, filename);
            GitError.Check(ret);
        }

        // TODO: implement a lot of overload methods for this!
        public void SetRawContent()
        {
            throw new NotImplementedException();
        }

        private void *GetRawContent()
        {
            // TODO: this has to be fixed first in the type definitions of libgit2
            // return NativeMethods.git_blob_rawcontent(blob);
            throw new NotImplementedException();
        }

        public int Size
        {
            get {
                return NativeMethods.git_blob_rawsize(blob);
            }
        }

        public static void WriteFile(ObjectId writtenId, Repository repository, string path)
        {
            NativeMethods.git_blob_writefile(&writtenId.oid, repository.repository, path);
        }
    }
}
