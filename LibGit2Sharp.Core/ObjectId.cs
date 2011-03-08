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
    
        public ObjectId(string str)
        {
            fixed (git_oid *p = &oid)
            {
                int ret = NativeMethods.git_oid_mkstr(p, str);
                GitError.Check(ret);
            }
        }
    
        internal static void Copy(git_oid *dst, git_oid *src)
        {
            NativeMethods.git_oid_cpy(dst, src);
        }
    
        public static int Compare(ObjectId a, ObjectId b)
        {
            return NativeMethods.git_oid_cmp(&a.oid, &b.oid);
        }
        
        public static int Compare(string a, string b)
        {
            return Compare(new ObjectId(a), new ObjectId(b));
        }
        
        public int Compare(ObjectId other)
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
