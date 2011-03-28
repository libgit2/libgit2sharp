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
    public class GitObject : IDisposable
    {
        public static GitObjectTypeMap TypeToTypeMap =
            new GitObjectTypeMap
                {
                    {typeof (Commit), GitObjectType.Commit},
                    {typeof (Tree), GitObjectType.Tree},
                    {typeof (Blob), GitObjectType.Blob},
                    {typeof (Tag), GitObjectType.Tag},
                    {typeof (GitObject), GitObjectType.Any},
                };

        protected readonly Repository Repo;
        protected IntPtr Obj = IntPtr.Zero;
        private bool disposed;

        private string sha;

        protected GitObject(IntPtr obj, GitOid oid, Repository repo)
        {
            Oid = oid;
            Obj = obj;
            Repo = repo;
        }

        public GitOid Oid { get; private set; }

        public string Sha
        {
            get
            {
                if (sha != null) return sha;

                var ptr = NativeMethods.git_object_id(Obj);
                var oid = (GitOid) Marshal.PtrToStructure(ptr, typeof (GitOid));
                sha = oid.ToSha();
                return sha;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        internal static GitObject CreateFromPtr(IntPtr obj, GitOid oid, Repository repo)
        {
            var type = NativeMethods.git_object_type(obj);
            switch (type)
            {
                case GitObjectType.Commit:
                    return new Commit(obj, oid, repo);
                case GitObjectType.Tree:
                    return new Tree(obj, oid, repo);
                case GitObjectType.Tag:
                    return new Tag(obj, oid, repo);
                case GitObjectType.Blob:
                    return new Blob(obj, oid, repo);
                default:
                    return new GitObject(obj, oid, repo);
            }
        }

        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                if (Obj != IntPtr.Zero)
                    NativeMethods.git_object_close(Obj);

                // Note disposing has been done.
                disposed = true;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (GitObject)) return false;
            return Equals((GitObject) obj);
        }

        public bool Equals(GitObject other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.Oid.Equals(Oid);
        }

        ~GitObject()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        public override int GetHashCode()
        {
            return Oid.GetHashCode();
        }
    }
}