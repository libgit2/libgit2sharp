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
    // TODO: implement the time fields
    unsafe public class Signature : IDisposable
    {
        internal git_signature *signature = null;
    
        internal Signature(git_signature *signature)
        {
            this.signature = signature;
        }
    
        public Signature(string name, string email)
            : this(NativeMethods.git_signature_new(name, email, 0, 0))
        {
        }

        // indicates if it is managed by a repository or not
        internal bool Managed { get; set; }

        public string Name
        {
            get {
                return new string(signature->name);
            }
        }
        
        public string Email
        {
            get {
                return new string(signature->email);
            }
        }
    
        public int Time
        {
            get {
                return signature->time;
            }
        }
    
        public int Offset
        {
            get {
                return signature->offset;
            }
        }
    
        public Signature Clone()
        {
            return new Signature(NativeMethods.git_signature_dup(signature));
        }

        public void Free()
        {
            NativeMethods.git_signature_free(signature);
            signature = null;
        }
    
        #region IDisposable implementation
        public void Dispose()
        {
            if (!Managed)
                Free();
        }
        #endregion
    }
}
