using System;
using System.IO;
using System.Runtime.InteropServices;
using libgit2sharp.Wrapper;

namespace libgit2sharp
{
    public class RepositoryLifecycleManager : ILifecycleManager
    {
        private IntPtr _repositoryPtr = IntPtr.Zero;
        private RepositoryDetails _details;

        public IntPtr RepositoryPtr
        {
            get { return _repositoryPtr; }
        }

        public RepositoryDetails Details
        {
            get { return _details; }
        }

        public RepositoryLifecycleManager(string repositoryDirectory)
        {
            #region Parameters Validation

            if (string.IsNullOrEmpty(repositoryDirectory))
            {
                throw new ArgumentNullException(repositoryDirectory);
            }

            #endregion Parameters Validation

            OpenRepository(() => LibGit2Api.wrapped_git_repository_open(out _repositoryPtr, Posixify(repositoryDirectory)));
        }

        public RepositoryLifecycleManager(string repositoryDirectory, string databaseDirectory, string index, string workingDirectory)
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

            OpenRepository(() => LibGit2Api.wrapped_git_repository_open2(out _repositoryPtr, Posixify(repositoryDirectory),
                                                        Posixify(databaseDirectory), Posixify(index), Posixify(workingDirectory)));
        }

        private static string Posixify(string path)
        {
            if (Path.DirectorySeparatorChar == '/')
            {
                return path;
            }

            return path.Replace(Path.DirectorySeparatorChar, '/');
        }


        private void OpenRepository(Func<OperationResult> opener)
        {
            OperationResult result = opener();

            if (result == OperationResult.GIT_SUCCESS)
            {
                _details = BuildRepositoryDetails(_repositoryPtr);
                return;
            }

            _repositoryPtr = IntPtr.Zero;

            switch (result)
            {
                case OperationResult.GIT_ENOTAREPO:
                    throw new NotAValidRepositoryException();

                default:
                    throw new Exception(Enum.GetName(typeof(OperationResult), result));
            }
        }
        
        private static RepositoryDetails BuildRepositoryDetails(IntPtr gitRepositoryPtr)
        {
            var gitRepo = (git_repository)Marshal.PtrToStructure(gitRepositoryPtr, typeof(git_repository));
            return gitRepo.Build();
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_repositoryPtr == (IntPtr)0)
            {
                return;
            }

            LibGit2Api.wrapped_git_repository_free(_repositoryPtr);
            _repositoryPtr = (IntPtr)0;
        }

        ~RepositoryLifecycleManager()
        {
            Dispose(false);
        }
    }
}