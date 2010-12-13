using System;
using System.Runtime.InteropServices;
using libgit2sharp.Wrapper;

namespace libgit2sharp
{
    public class Repository : IResolver, IDisposable, IObjectHeaderReader
    {
        private IntPtr _repositoryPtr = IntPtr.Zero;
        private RepositoryDetails _details;
        private IResolver _resolver;

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

            OpenRepository(() => LibGit2Api.wrapped_git_repository_open2(out _repositoryPtr, repositoryDirectory, databaseDirectory, index, workingDirectory));
        }

        public Repository(string repositoryDirectory)
        {
            #region Parameters Validation

            if (string.IsNullOrEmpty(repositoryDirectory))
            {
                throw new ArgumentNullException(repositoryDirectory);
            }

            #endregion Parameters Validation

            OpenRepository(() => LibGit2Api.wrapped_git_repository_open(out _repositoryPtr, repositoryDirectory));
        }

        private void OpenRepository(Func<OperationResult> opener)
        {
            OperationResult result = opener();

            if (result == OperationResult.GIT_SUCCESS)
            {
                _resolver = new ObjectResolver(_repositoryPtr, this);
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

        public Header ReadHeader(string objectId)
        {
            DatabaseReader reader = LibGit2Api.wrapped_git_odb_read_header;
            Func<git_rawobj, Header> builder = rawObj => rawObj.BuildHeader(objectId);

            return ReadInternal(objectId, reader, builder);
        }

        public RawObject Read(string objectId)
        {
            DatabaseReader reader = LibGit2Api.wrapped_git_odb_read;
            Func<git_rawobj, RawObject> builder = rawObj => rawObj.Build(objectId);

            //TODO: RawObject should be freed when the Repository is disposed (cf. https://github.com/libgit2/libgit2/blob/6fd195d76c7f52baae5540e287affe2259900d36/tests/t0205-readheader.c#L202)
            return ReadInternal(objectId, reader, builder);
        }

        public bool Exists(string objectId)
        {
            return LibGit2Api.wrapped_git_odb_exists(_repositoryPtr, objectId);
        }

        private delegate OperationResult DatabaseReader(out git_rawobj rawobj, IntPtr repository, string objectId);

        private TType ReadInternal<TType>(string objectId, DatabaseReader reader, Func<git_rawobj, TType> builder)
        {
            git_rawobj rawObj;
            OperationResult result = reader(out rawObj, _repositoryPtr, objectId);

            switch (result)
            {
                case OperationResult.GIT_SUCCESS:
                    return builder(rawObj);

                case OperationResult.GIT_ENOTFOUND:
                    return default(TType);

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
            {
                return;
            }

            LibGit2Api.wrapped_git_repository_free(_repositoryPtr);
            _repositoryPtr = (IntPtr)0;
        }

        ~Repository()
        {
            Dispose(false);
        }

        public GitObject Resolve(string objectId)
        {
            return _resolver.Resolve(objectId);
        }

        public TType Resolve<TType>(string objectId)
        {
            return _resolver.Resolve<TType>(objectId);
        }

        public object Resolve(string objectId, Type expectedType)
        {
            return _resolver.Resolve(objectId, expectedType);
        }

        private static RepositoryDetails BuildRepositoryDetails(IntPtr gitRepositoryPtr)
        {
            var gitRepo = (git_repository)Marshal.PtrToStructure(gitRepositoryPtr, typeof(git_repository));
            return gitRepo.Build();
        }

    }
}