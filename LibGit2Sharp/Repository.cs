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
using System.IO;
using LibGit2Sharp.Properties;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Repository is the primary interface into a git repository
    /// </summary>
    public class Repository : IDisposable
    {
        private readonly RepositoryOptions options;
        private readonly IntPtr repo = IntPtr.Zero;
        private bool disposed;

#if NET35
        public Repository(string path) : this(path, null)
        {
        }

        public Repository(string path, RepositoryOptions options)
#else
    /// <summary>
    ///   Initializes a new instance of the <see cref = "Repository" /> class.
    /// 
    ///   Exceptions:
    ///     ArgumentException
    ///     ArgumentNullException
    ///     TODO: ApplicationException is thrown for all git errors right now
    /// </summary>
    /// <param name = "path">The path to the git repository to open.</param>
    /// <param name = "options">The options.</param>
        public Repository(string path, RepositoryOptions options = null)
#endif
        {
            Path = path;
            Ensure.ArgumentNotNull(path, "path");
            this.options = options ?? new RepositoryOptions();

            Path = path;
            PosixPath = path.Replace(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

            if (!this.options.CreateIfNeeded && !Directory.Exists(path))
                throw new ArgumentException(Resources.RepositoryDoesNotExist, "path");

<<<<<<< HEAD
        public Header ReadHeader(string objectId)
        {
            Func<Core.RawObject, Header> builder = rawObj => { 
                return new Header(objectId, (ObjectType)rawObj.Type, rawObj.Length);
            };
			
            return ReadHeaderInternal(objectId, builder);
        }

        public RawObject Read(string objectId)
        {
            //TODO: RawObject should be freed when the Repository is disposed (cf. https://github.com/libgit2/libgit2/blob/6fd195d76c7f52baae5540e287affe2259900d36/tests/t0205-readheader.c#L202)
            
            Func<Core.RawObject, RawObject> builder = rawObj => {
                Header header = new Header(objectId, (ObjectType)rawObj.Type, rawObj.Length);
                return new RawObject(header, rawObj.GetData());
            };

            return ReadInternal(objectId, builder);
        }

        public bool Exists(string objectId)
        {
            return _lifecycleManager.CoreRepository.Database.Exists(new Core.ObjectId(objectId));
=======
            if (this.options.CreateIfNeeded)
            {
                var res = NativeMethods.git_repository_init(out repo, PosixPath, this.options.IsBareRepository);
                Ensure.Success(res);
            }
            else
            {
                var res = NativeMethods.git_repository_open(out repo, PosixPath);
                Ensure.Success(res);
            }
>>>>>>> 777a6eb... can open and create a repository using new interop
        }
		
        private TType ReadHeaderInternal<TType>(string objectid, Func<Core.RawObject, TType> builder)
        {
            var rawObj = _lifecycleManager.CoreRepository.Database.ReadHeader(new Core.ObjectId(objectid));

            return builder(rawObj);
        }

        private TType ReadInternal<TType>(string objectid, Func<Core.RawObject, TType> builder)
        {
            var rawObj = _lifecycleManager.CoreRepository.Database.Read(new Core.ObjectId(objectid));
            
            return builder(rawObj);
        }

        /// <summary>
        ///   Gets the path to the git repository.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        ///   Gets the posix path to the git repository.
        /// </summary>
        public string PosixPath { get; private set; }

        /// <summary>
        /// Tells if the specified <see cref="GitOid"/> exists in the repository.
        /// 
        ///   Exceptions:
        ///     ArgumentNullException
        /// </summary>
        /// <param name="oid">The oid.</param>
        /// <returns></returns>
        public bool Exists(GitOid oid)
        {
            Ensure.ArgumentNotNull(oid, "oid");

            var odb = NativeMethods.git_repository_database(repo);
            return NativeMethods.git_odb_exists(odb, ref oid);
        }

        /// <summary>
        /// Tells if the specified sha exists in the repository.
        /// 
        ///   Exceptions:
        ///     ArgumentException
        ///     ArgumentNullException
        /// </summary>
        /// <param name="sha">The sha.</param>
        /// <returns></returns>
        public bool Exists(string sha)
        {
            Ensure.ArgumentNotNullOrEmptyString(sha, "sha");

            var oid = GitOid.FromSha(sha);
            return Exists(oid);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

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
                if (repo != IntPtr.Zero)
                    NativeMethods.git_repository_free(repo);

                // Note disposing has been done.
                disposed = true;
            }
        }

        ~Repository()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }
    }
}
