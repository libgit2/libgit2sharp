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

            _details = BuildRepositoryDetails(_repositoryPtr);
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

            _details = BuildRepositoryDetails(_repositoryPtr);
        }

        private static RepositoryDetails BuildRepositoryDetails(IntPtr repository)
        {
            var repo = (git_repository)Marshal.PtrToStructure(repository, typeof(git_repository));

            return repo.Build();
        }

        public GitObject Lookup(string objectId)
        {
            return new GitObject(objectId, ObjectType.Commit);
        }

        public Header ReadHeader(string objectId)
        {
            DatabaseReader reader = LibGit2Api.wrapped_git_odb_read_header;
            Func<git_rawobj, Header> builder = (rawObj) => rawObj.BuildHeader(objectId);

            return ReadInternal(objectId, reader, builder);
        }

        public RawObject Read(string objectId)
        {
            DatabaseReader reader = LibGit2Api.wrapped_git_odb_read;
            Func<git_rawobj, RawObject> builder = (rawObj) => rawObj.Build(objectId);

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