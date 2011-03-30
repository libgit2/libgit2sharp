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
    unsafe public class SymbolicReference : Reference
    {
        internal SymbolicReference(git_reference *reference)
        {
            this.reference = reference;
        }

        public SymbolicReference(Repository repository, string name, string target)
        {
            int ret;
            fixed (git_reference **reference = &this.reference)
            {
                ret = NativeMethods.git_reference_create_symbolic(reference, repository.repository, name, target);
            }
            GitError.Check(ret);
            if (this.reference == null)
                throw new GitException();
        }

        public string Target
        {
            get {
                return new string(NativeMethods.git_reference_target(reference));
            }
            set {
                int ret = NativeMethods.git_reference_set_target(reference, value);
                GitError.Check(ret);
            }
        }
    }
}   
