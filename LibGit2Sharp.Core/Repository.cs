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
	unsafe public class Repository : IDisposable
	{
        internal git_repository *repository = null;

        internal Repository(git_repository *repository)
        {
            this.repository = repository;
        }

        public Repository(string path)
        {
            int ret;
            fixed (git_repository **repo = &repository)
            {
                ret = NativeMethods.git_repository_open(repo, path);
            }
            GitError.Check(ret);
        }

        public Repository(string gitDir, string objcetDirectory, string indexFile, string workTree)
        {
            int ret;
            fixed (git_repository **repo = &repository)
            {
                ret = NativeMethods.git_repository_open2(repo, gitDir, objcetDirectory, indexFile, workTree);
            }
            GitError.Check(ret);
        }

        public static Repository Init(string path, bool bare)
        {
            git_repository *repo = null;
    
            int ret = NativeMethods.git_repository_init(&repo, path, (uint)(bare ? 1 : 0));
            GitError.Check(ret);
    
            return new Repository(repo);
        }

        public static Repository Init(string path)
        {
            return Init(path, false);
        }

        public Index Index
        {
            get {
                git_index *index = null;
                int ret = NativeMethods.git_repository_index(&index, repository);
                GitError.Check(ret);
                NativeMethods.git_index_read(index);
                return new Index(index);
            }
        }

        public GitObject Lookup(ObjectId oid)
        {
            return Lookup(oid, git_otype.GIT_OBJ_ANY);
        }

        public GitObject Lookup(ObjectId oid, git_otype type)
        {
            git_object *obj = null;
            int ret = NativeMethods.git_object_lookup(&obj, repository, &oid.oid, type);
    
            GitError.Check(ret);
    
            if (obj == null)
                return null;

            return GitObject.Create(obj);
        }

        public T Lookup<T>(ObjectId oid) where T : GitObject
        {
            git_object *obj = null;
            int ret = NativeMethods.git_object_lookup(&obj, repository, &oid.oid, GitObject.GetType(typeof(T)));
            GitError.Check(ret);
    
            if (obj == null)
                return default(T);

            return GitObject.Create<T>(obj);
        }
        
        public Reference ReferenceLookup(string name)
        {
            git_reference *reference = null;
    
            int ret = NativeMethods.git_reference_lookup(&reference, repository, name);
            GitError.Check(ret);

            if (reference == null)
             return null;

            return Reference.Create(reference);
        }

        public Index OpenIndex()
        {
            git_index *index = null;
            int ret = NativeMethods.git_index_open_inrepo(&index, repository);
            GitError.Check(ret);
    
            if (index == null)
              return null;

            return new Index(index);
        }

        public void WriteFile(ObjectId writtenId, string path)
        {
            Blob.WriteFile(writtenId, this, path);
        }

        public string RepositoryDirectory
        {
            get {
                if (repository->path_repository == null)
                return string.Empty;
                return new string(repository->path_repository);
            }
        }

        public string IndexFile
        {
            get {
                if (repository->path_index == null)
                    return string.Empty;
                return new string(repository->path_index);
            }
        }

        public string DatabaseDirectory
        {
            get {
                if (repository->path_odb == null)
                    return string.Empty;
                return new string(repository->path_odb);
            }
        }
        
        public string WorkingDirectory
        {
            get {
                if (repository->path_workdir == null)
                    return string.Empty;
                return new string(repository->path_workdir);
            }
        }
        
        public bool IsBare
        {
            get {
                return (repository->is_bare > 0);
            }
        }
        
        public Database Database
        {
            get {
                return new Database(NativeMethods.git_repository_database(repository));
            }
        }

        public ObjectId HeadObjectId
        {
            get {
                return ReferenceLookup("HEAD").Resolve().ObjectId;
            }
        }

        public GitObject GetHead()
        {
            return Lookup(HeadObjectId);
        }

        public T GetHead<T>() where T : GitObject
        {
            return Lookup<T>(HeadObjectId);
        }

        public Commit Head
        {
            get {
                return GetHead<Commit>();
            }
        }
        
        #region IDisposable implementation
        public void Dispose()
        {
            NativeMethods.git_repository_free(repository);
        }
        #endregion
    }
}
