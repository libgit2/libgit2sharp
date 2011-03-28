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

using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp
{
    public class Signature
    {
        private readonly GitSignature sig = new GitSignature();
        private DateTimeOffset? when;

        internal Signature(IntPtr signaturePtr, bool ownedByRepo = true)
        {
            Marshal.PtrToStructure(signaturePtr, sig);
            if (!ownedByRepo)
            {
                NativeMethods.git_signature_free(signaturePtr);
            }
        }

        public Signature(string name, string email, DateTimeOffset when)
            : this(NativeMethods.git_signature_new(name, email, when.ToSecondsSinceEpoch(), (int) when.Offset.TotalMinutes), false)
        {
        }

        public string Name
        {
            get { return sig.Name; }
        }

        public string Email
        {
            get { return sig.Email; }
        }

        public DateTimeOffset When
        {
            get
            {
                if (when == null)
                {
                    when = Epoch.ToDateTimeOffset(sig.When.Time, sig.When.Offset);
                }
                return when.Value;
            }
        }
    }
}