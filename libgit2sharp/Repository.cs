using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            return Resolve<GitObject>(objectId);
        }

        public TType Resolve<TType>(string objectId)
        {
            return (TType)Resolve(objectId, typeof(TType));
        }

        private static readonly IDictionary<ObjectType, Type> resolveMapper = new Dictionary<ObjectType, Type>
                                                                  {
                                                                      {ObjectType.Blob, typeof(Blob)},
                                                                      {ObjectType.Commit, typeof(Commit)},
                                                                      {ObjectType.Tag, typeof(Tag)},
                                                                      {ObjectType.Tree, typeof(Tree)},
                                                                  };

        private static readonly IDictionary<Type, ObjectType> reverseResolveMapper =
            resolveMapper.ToDictionary(kv => kv.Value, kv => kv.Key);

        private static readonly IDictionary<Type, Func<string, IntPtr, object>> builderMapper = new Dictionary<Type, Func<string, IntPtr, object>>
                                                                  {
                                                                      {typeof(Blob), BuildBlob},
                                                                      {typeof(Commit), BuildCommit},
                                                                      {typeof(Tag), BuildTag},
                                                                      {typeof(Tree), BuildTree},
                                                                  };

        private Type GuessTypeToResolve(string objectId)
        {
            Header header = ReadHeader(objectId);

            if (header == null)
            {
                return null;
            }

            return resolveMapper[header.Type];
        }

        public object Resolve(string objectId, Type expectedType)
        {
            if (!typeof(GitObject).IsAssignableFrom(expectedType))
            {
                throw new ArgumentException("Only types deriving from GitObject are allowed.", "expectedType");
            }

            Type outputType = expectedType;
            if (outputType == typeof(GitObject))
            {
                Type guessedType = GuessTypeToResolve(objectId);

                if (guessedType == null)
                {
                    return null;
                }

                outputType = guessedType;
            }

            ObjectType expectedObjectType = reverseResolveMapper[outputType];

            IntPtr gitObjectPtr;
            OperationResult result = LibGit2Api.wrapped_git_repository_lookup(out gitObjectPtr, _repositoryPtr, objectId, (git_otype)expectedObjectType);

            switch (result)
            {
                case OperationResult.GIT_SUCCESS:
                    return builderMapper[outputType](objectId, gitObjectPtr);

                case OperationResult.GIT_ENOTFOUND:
                //TODO: Should we free gitObjectPtr if OperationResult.GIT_EINVALIDTYPE is returned ?
                case OperationResult.GIT_EINVALIDTYPE:
                    return null;

                default:
                    throw new Exception(Enum.GetName(typeof(OperationResult), result));
            }
        }

        private static object BuildTree(string objectId, IntPtr gitObjectPtr)
        {
            var gitTree = (git_tree)Marshal.PtrToStructure(gitObjectPtr, typeof(git_tree));
            return gitTree.Build();
        }

        private static object BuildCommit(string objectId, IntPtr gitObjectPtr)
        {
            var gitCommit = (git_commit)Marshal.PtrToStructure(gitObjectPtr, typeof(git_commit));
            return gitCommit.Build();
        }

        private static object BuildBlob(string objectId, IntPtr gitObjectPtr)
        {
            throw new NotImplementedException();
            //var gitBlob = (git_blob)Marshal.PtrToStructure(gitObjectPtr, typeof(git_blob));
            //return gitBlob.Build();
        }

        private static object BuildTag(string objectId, IntPtr gitObjectPtr)
        {
            var gitTag = (git_tag)Marshal.PtrToStructure(gitObjectPtr, typeof(git_tag));
            return gitTag.Build();
        }

        private static RepositoryDetails BuildRepositoryDetails(IntPtr gitRepositoryPtr)
        {
            var gitRepo = (git_repository)Marshal.PtrToStructure(gitRepositoryPtr, typeof(git_repository));
            return gitRepo.Build();
        }

    }
}