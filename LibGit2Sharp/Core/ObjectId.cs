/*
 * The MIT License
 *
 * Copyright (c) 2010 Andrius Bentkus
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
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    unsafe public struct ObjectId
    {
        public const int RawSize = 20;
        public const int HexSize = RawSize * 2;
    
        internal git_oid oid;
    
        internal ObjectId(git_oid *oid)
        {
            fixed (git_oid *poid = &this.oid)
            {
                Copy(poid, oid);
            }
        }

        public ObjectId(ObjectId oid)
            : this(&oid.oid)
        {
        }
    
        public ObjectId(string str)
        {
            fixed (git_oid *p = &oid)
            {
                int ret = NativeMethods.git_oid_mkstr(p, str);
                GitError.Check(ret);
            }
        }
    
        public void Copy(ObjectId src)
        {
            Copy(this, src);
        }

        public static void Copy(ObjectId dst, ObjectId src)
        {
            Copy(&dst.oid, &src.oid);
        }

        internal static void Copy(git_oid *dst, git_oid *src)
        {
            NativeMethods.git_oid_cpy(dst, src);
        }
    
        public static int Compare(ObjectId a, ObjectId b)
        {
            return NativeMethods.git_oid_cmp(&a.oid, &b.oid);
        }
        
        public static int Compare(ObjectId a, string b)
        {
            return Compare(a, new ObjectId(b));
        }

        public static int Compare(string a, ObjectId b)
        {
            return Compare(new ObjectId(a), b);
        }

        public static int Compare(string a, string b)
        {
            return Compare(new ObjectId(a), new ObjectId(b));
        }
        
        public int Compare(ObjectId other)
        {
            return Compare(this, other);
        }

        public int Compare(string other)
        {
            return Compare(this, other);
        }
        
        public override string ToString()
        {
            fixed (git_oid *poid = &oid)
            {
                // +1 for holding the string terminator
                IntPtr ptr = Marshal.AllocHGlobal(HexSize + 1);
                return NativeMethods.git_oid_to_string((sbyte *)ptr.ToPointer(), HexSize + 1, poid);
            }
        }
    }
}
