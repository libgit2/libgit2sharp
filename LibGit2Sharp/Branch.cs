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

namespace LibGit2Sharp
{
    public class Branch
    {
        public string RemoteName { get; set; }
        public string Name { get; set; }
        public DirectReference Reference { get; set; }
        public BranchType Type { get; set; }

        internal static Branch CreateBranchFromReference(Reference reference)
        {
            var tokens = reference.Name.Split('/');
            if (tokens.Length < 2)
            {
                throw new ArgumentException(string.Format("Unexpected ref name: {0}", reference.Name));
            }

            if (tokens[tokens.Length - 2] == "heads")
            {
                return new Branch
                           {
                               Name = tokens[tokens.Length - 1],
                               Reference = (DirectReference) reference,
                               Type = BranchType.Local
                           };
            }
            return new Branch
                       {
                           Name = string.Join("/", tokens, tokens.Length - 2, 2),
                           RemoteName = tokens[tokens.Length - 2],
                           Reference = (DirectReference) reference,
                           Type = BranchType.Remote
                       };
        }
    }
}