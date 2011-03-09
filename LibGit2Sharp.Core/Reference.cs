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
    unsafe abstract public class Reference
    {
        internal git_reference *reference = null;
    
        internal Reference()
        {
        }
    
        public ObjectIdReference Resolve()
        {
            git_reference *resolved_ref = null;
    
            int ret = NativeMethods.git_reference_resolve(&resolved_ref, reference);
            GitError.Check(ret);
    
            return Reference.Create(resolved_ref) as ObjectIdReference;
        }

        public Repository Owner
        {
            get {
                return new Repository(NativeMethods.git_reference_owner(reference));
            }
        }
        
        public void Rename(string newName)
        {
            int ret = NativeMethods.git_reference_rename(reference, newName);
            GitError.Check(ret);
        }
        
        public void Delete()
        {
            int ret = NativeMethods.git_reference_delete(reference);
            GitError.Check(ret);
        }

        public git_rtype Type
        {
            get {
                return NativeMethods.git_reference_type(reference);
            }
        }

        internal static Reference Create(git_reference *reference)
        {
            switch (NativeMethods.git_reference_type(reference))
            {
                case git_rtype.GIT_REF_SYMBOLIC:
                    return new SymbolicReference(reference);
                case git_rtype.GIT_REF_OID:
                    return new ObjectIdReference(reference);
                case git_rtype.GIT_REF_INVALID:
                    throw new InvalidReferenceException();
                default:
                    throw new Exception("Reference type not yet implemented");
            }
        }
    }

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
    
    unsafe public class ObjectIdReference : Reference
    {
        internal ObjectIdReference(git_reference *reference)
        {
            this.reference = reference;
        }
    
        public ObjectIdReference(Repository repository, string name, ObjectId oid)
        {
            fixed (git_reference **reference = &this.reference)
            {
                int ret = NativeMethods.git_reference_create_oid(reference, repository.repository, name, &oid.oid);
                GitError.Check(ret);
            }
        }
    
        public ObjectId ObjectId
        {
            get {
                return new ObjectId(NativeMethods.git_reference_oid(reference));
            }
            set {
                NativeMethods.git_reference_set_oid(reference, &value.oid);
            }
        }
    }
}
