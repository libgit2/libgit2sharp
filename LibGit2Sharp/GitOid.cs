#region  Copyright (c) 2011 LibGit2Sharp committers

//  The MIT License
//  
//  Copyright (c) 2011 LibGit2Sharp committers
//  
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.

#endregion

using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Represents a unique id in git which is the sha1 hash of this id's content.
    /// </summary>
    public struct GitOid
    {
        /// <summary>
        ///   The raw binary 20 byte Id.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)] 
        public byte[] Id;

        public bool Equals(GitOid other)
        {
            return NativeMethods.git_oid_cmp(ref this, ref other) == 0;
        }

        /// <summary>
        ///   Create a new <see cref = "GitOid" /> from a sha1.
        /// </summary>
        /// <param name = "sha">The sha.</param>
        /// <returns></returns>
        public static GitOid FromSha(string sha)
        {
            GitOid oid;
            var result = NativeMethods.git_oid_mkstr(out oid, sha);
            Ensure.Success(result);
            return oid;
        }

        public override int GetHashCode()
        {
            return (Id != null ? Id.Sum(i => i) : 0);
        }

        /// <summary>
        ///   Convert to 40 character sha1 representation
        /// </summary>
        /// <returns></returns>
        public string ToSha()
        {
            var hex = new byte[40];
            NativeMethods.git_oid_fmt(hex, ref this);
            return Encoding.UTF8.GetString(hex);
        }

        public override string ToString()
        {
            return ToSha();
        }
    }
}