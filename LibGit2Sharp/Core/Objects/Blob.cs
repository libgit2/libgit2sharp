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

        public static ObjectId Create(Repository repository, string path)
        {
            ObjectId oid = new ObjectId();
            int ret = NativeMethods.git_blob_create_fromfile(&oid.oid, repository.repository, path);
            GitError.Check(ret);
            return oid;
        }

        public static ObjectId Create(Repository repository, byte[] buffer)
        {
            ObjectId oid = new ObjectId();
            int ret = NativeMethods.git_blob_create_frombuffer(&oid.oid, repository.repository, buffer, (uint)buffer.Length);
            GitError.Check(ret);
            return oid;

        }

        public byte[] GetContent()
        {
            byte[] content = new byte[Size];
            byte *b = (byte *)GetRawContent();
            for (int i = 0; i < Size; i++) {
                content[i] = b[i];
            }
            return content;
        }

        internal void *GetRawContent()
        {
            return NativeMethods.git_blob_rawcontent(blob);
        }

        public NativeMemoryStream GetNativeMemoryStreamContent()
        {
            return new NativeMemoryStream(GetRawContent(), Size);
        }

        public int Size
        {
            get {
                return NativeMethods.git_blob_rawsize(blob);
            }
        }
    }
}
