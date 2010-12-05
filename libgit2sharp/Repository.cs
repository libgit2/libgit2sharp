using System;
using System.Runtime.InteropServices;
using libgit2sharp.Wrapper;

namespace libgit2sharp
{
    public class Repository : IDisposable
    {
        private IntPtr _repositoryPtr = IntPtr.Zero;
        private readonly RepositoryDetails _details;

        public RepositoryDetails Details
        {
            get { return _details; }
        }

        public Repository(string repositoryDirectory, string databaseDirectory, string index, string workingDirectory)
        {
            #region Parameters Validation

            if (string.IsNullOrEmpty(repositoryDirectory))
            {
                throw new ArgumentNullException("repositoryDirectory");
            }

            if (string.IsNullOrEmpty(databaseDirectory))
            {
                throw new ArgumentNullException("databaseDirectory");
            }

            if (string.IsNullOrEmpty(index))
            {
                throw new ArgumentNullException("index");
            }

            if (string.IsNullOrEmpty(workingDirectory))
            {
                throw new ArgumentNullException("workingDirectory");
            }

            #endregion Parameters Validation

            OperationResult result = LibGit2Api.wrapped_git_repository_open2(out _repositoryPtr, repositoryDirectory, databaseDirectory, index, workingDirectory);

            if (result != OperationResult.GIT_SUCCESS)
            {
                throw new Exception(Enum.GetName(typeof(OperationResult), result));
            }

            _details = BuildFrom(_repositoryPtr);
        }

        public Repository(string repositoryDirectory)
        {
            #region Parameters Validation 

            if (string.IsNullOrEmpty(repositoryDirectory))
            {
                throw new ArgumentNullException(repositoryDirectory);
            }

            #endregion Parameters Validation

            OperationResult result = LibGit2Api.wrapped_git_repository_open(out _repositoryPtr, repositoryDirectory);

            if (result != OperationResult.GIT_SUCCESS)
            {
                throw new Exception(Enum.GetName(typeof(OperationResult), result));
            }

            _details = BuildFrom(_repositoryPtr);
        }

        private static RepositoryDetails BuildFrom(IntPtr repository)
        {
            var repo = (wrapped_git_repository)Marshal.PtrToStructure(repository, typeof(wrapped_git_repository));

            return new RepositoryDetails(repo.path_repository, repo.path_index, repo.path_odb, repo.path_workdir, repo.is_bare);
        }

        public GitObject Lookup(string objectId)
        {
            return new GitObject() { Id = objectId };
        }

        public Header ReadHeader(string objectId)
        {
            git_rawobj rawObj;

            OperationResult result = LibGit2Api.wrapped_git_odb_read_header(out rawObj, _repositoryPtr, objectId);

            switch (result)
            {
                case OperationResult.GIT_SUCCESS:
                    return new Header(objectId, (ObjectType)rawObj.type, rawObj.len.ToUInt64());

                case OperationResult.GIT_ENOTFOUND:
                    return null;

                default:
                    throw new Exception(Enum.GetName(typeof(OperationResult), result));
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_repositoryPtr == (IntPtr)0)
                    return;

            LibGit2Api.wrapped_git_repository_free(_repositoryPtr);
            _repositoryPtr = (IntPtr)0;
        }

        ~Repository()
        {
            Dispose(false);
        }
    }
}