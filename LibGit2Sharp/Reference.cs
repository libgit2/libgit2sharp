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
    /// <summary>
    ///   A Reference to another git object
    /// </summary>
    public abstract class Reference
    {
        private readonly Repository repo;
        private IntPtr referencePtr;

        protected Reference(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Gets the name of this reference.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///   Gets the type of this reference.
        /// </summary>
        public GitReferenceType Type { get; private set; }

        internal static Reference CreateFromPtr(IntPtr ptr, Repository repo)
        {
            var name = NativeMethods.git_reference_name(ptr);
            var type = NativeMethods.git_reference_type(ptr);
            if (type == GitReferenceType.Symbolic)
            {
                IntPtr resolveRef;
                NativeMethods.git_reference_resolve(out resolveRef, ptr);
                var reference = CreateFromPtr(resolveRef, repo);
                return new SymbolicReference(repo) {Name = name, Type = type, Target = reference, referencePtr = ptr};
            }
            if (type == GitReferenceType.Oid)
            {
                var oidPtr = NativeMethods.git_reference_oid(ptr);
                var oid = (GitOid) Marshal.PtrToStructure(oidPtr, typeof (GitOid));
                var target = repo.Lookup(oid);
                return new DirectReference(repo) {Name = name, Type = type, Target = target, referencePtr = ptr};
            }
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Deletes this reference.
        /// </summary>
        public void Delete()
        {
            var res = NativeMethods.git_reference_delete(referencePtr);
            Ensure.Success(res);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(Reference other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Name, Name);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        /// <summary>
        ///   Resolves to direct reference.
        /// </summary>
        /// <returns></returns>
        public DirectReference ResolveToDirectReference()
        {
            return ResolveToDirectReference(this);
        }

        private static DirectReference ResolveToDirectReference(Reference reference)
        {
            if (reference is DirectReference) return (DirectReference) reference;
            return ResolveToDirectReference(((SymbolicReference) reference).Target);
        }
    }
}