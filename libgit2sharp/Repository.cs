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
            var repo = (git_repository)Marshal.PtrToStructure(repository, typeof(git_repository));

            return new RepositoryDetails(repo.path_repository, repo.path_index, repo.path_odb, repo.path_workdir, repo.is_bare);
        }

        public GitObject Lookup(string objectId)
        {
            return new GitObject(objectId, ObjectType.Commit);
        }

        public Header ReadHeader(string objectId)
        {
            DatabaseReader reader = LibGit2Api.wrapped_git_odb_read_header;
            Func<git_rawobj, Header> builder = (rawObj) => (BuildHeaderFrom(objectId, rawObj));

            return ReadInternal(objectId, reader, builder);
        }

        public RawObject Read(string objectId)
        {
            DatabaseReader reader = LibGit2Api.wrapped_git_odb_read;
            Func<git_rawobj, RawObject> builder = (rawObj) => (BuildRawObjectFrom(objectId, rawObj));

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

        private static RawObject BuildRawObjectFrom(string objectId, git_rawobj rawObj)
        {
            Header header = BuildHeaderFrom(objectId, rawObj);
            var data = new byte[header.Length];

            //TODO: Casting the length to an int may lead to not copy the whole data. This should be converted to a loop.
            Marshal.Copy(rawObj.data, data, 0, (int)header.Length);
            return new RawObject(header, data);
        }

        private static Header BuildHeaderFrom(string objectId, git_rawobj rawObj)
        {
            return new Header(objectId, (ObjectType)rawObj.type, rawObj.len.ToUInt64());
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