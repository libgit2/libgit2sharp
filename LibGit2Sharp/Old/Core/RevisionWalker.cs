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
    unsafe public class RevisionWalker : IDisposable
    {
        internal git_revwalk *revwalk;
        public RevisionWalker(Repository repository)
        {
            int ret;
            fixed (git_revwalk **revwalk = &this.revwalk)
            {
                ret = NativeMethods.git_revwalk_new(revwalk, repository.repository);
            }
            GitError.Check(ret);
        }
        
        public void Push(Commit commit)
        {
            Push(commit.ObjectId);
        }

        public void Push(string oid)
        {
            Push(new ObjectId(oid));
        }

        public void Push(ObjectId oid)
        {
            int ret = NativeMethods.git_revwalk_push(revwalk, &oid.oid);
            GitError.Check(ret);
        }
        
        public void Hide(Commit commit)
        {
            Hide(commit.ObjectId);
        }

        public void Hide(ObjectId oid)
        {
            int ret = NativeMethods.git_revwalk_hide(revwalk, &oid.oid);
            GitError.Check(ret);
        }
        
        public Commit Next()
        {
            ObjectId oid;
            if (Next(out oid))
                return Repository.Lookup<Commit>(oid);
            else
                return null;
        }

        public bool Next(out ObjectId oid)
        {
            ObjectId noid = new ObjectId();
            int ret = NativeMethods.git_revwalk_next(&noid.oid, revwalk);
            oid = noid;
            if (ret == (int)git_result.GIT_EREVWALKOVER)
            {
                return false;
            }
            GitError.Check(ret);
            return true;
        }
    
        public void Reset()
        {
            NativeMethods.git_revwalk_reset(revwalk);
        }

        public Repository Repository
        {
            get {
                return new Repository(NativeMethods.git_revwalk_repository(revwalk));
            }
        }

        public void Sorting(uint sortMode)
        {
            NativeMethods.git_revwalk_sorting(revwalk, sortMode);
        }
    
        #region IDisposable implementation
        public void Dispose ()
        {
            if (revwalk != null)
                NativeMethods.git_revwalk_free(revwalk);
        }
        #endregion
    }
}
