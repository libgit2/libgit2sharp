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

namespace LibGit2Sharp.Core
{
    unsafe public class Database
    {
        internal git_odb *database;
    
        internal Database(git_odb *database)
        {
            this.database = database;
        }
    
        public Database(string objectsDir)
        {
            int ret;
            fixed (git_odb **database = &this.database)
            {
                ret = NativeMethods.git_odb_open(database, objectsDir);
            }
            GitError.Check(ret);
        }

        public bool Exists(string oid)
        {
            return Exists(new ObjectId(oid));
        }

        public bool Exists(ObjectId id)
        {
            return (NativeMethods.git_odb_exists(database, &id.oid) > 0);
        }
    
        public void Close()
        {
            NativeMethods.git_odb_close(database);
        }

        public void ReadHeader(string oid, out uint len, out git_otype type)
        {
            ReadHeader(new ObjectId(oid), out len, out type);
        }

        public void ReadHeader(ObjectId id, out uint len, out git_otype type)
        {
            int ret = NativeMethods.git_odb_read_header(out len, out type, database, &id.oid);
            GitError.Check(ret);
        }

        public DatabaseObject Read(string oid)
        {
            return Read(new ObjectId(oid));
        }

        public DatabaseObject Read(ObjectId id)
        {
            git_odb_object *obj = null;
            int ret = NativeMethods.git_odb_read(&obj, database, &id.oid);
            GitError.Check(ret);
            return new DatabaseObject(obj);
        }

        public ObjectId Hash(byte[] data, git_otype type)
        {
            ObjectId oid = new ObjectId();
            int ret = NativeMethods.git_odb_hash(&oid.oid, data, (uint)data.Length, type);
            GitError.Check(ret);
            return oid;
        }
    }
}
