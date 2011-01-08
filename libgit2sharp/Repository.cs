using System;
using System.Collections.Generic;
using libgit2sharp.Wrapper;

namespace libgit2sharp
{
    public sealed class Repository : IObjectResolver, IDisposable
    {
        private readonly IObjectResolver _objectResolver;
        private readonly ILifecycleManager _lifecycleManager;
        private readonly IBuilder _builder;
        private readonly IRefsResolver _refsResolver;

        public RepositoryDetails Details
        {
            get { return _lifecycleManager.Details; }
        }

        public Repository(string repositoryDirectory, string databaseDirectory, string index, string workingDirectory)
            : this(new RepositoryLifecycleManager(repositoryDirectory, databaseDirectory, index, workingDirectory))
        {

        }

        public Repository(string repositoryDirectory)
            : this(new RepositoryLifecycleManager(repositoryDirectory))
        {

        }

        private Repository(ILifecycleManager lifecycleManager)
        {
            _lifecycleManager = lifecycleManager;
            _builder = new ObjectBuilder();
            _objectResolver = new ObjectResolver(_lifecycleManager.RepositoryPtr, _builder);
            _refsResolver = new RefsResolver();
        }

        public IList<Ref> RetrieveRefs()
        {
            throw new NotImplementedException();
        } 

        public Header ReadHeader(string objectId)
        {
            DatabaseReader reader = NativeMethods.wrapped_git_odb_read_header;
            Func<git_rawobj, Header> builder = rawObj => rawObj.BuildHeader(objectId);

            return ReadInternal(objectId, reader, builder);
        }

        public RawObject Read(string objectId)
        {
            DatabaseReader reader = NativeMethods.wrapped_git_odb_read;
            Func<git_rawobj, RawObject> builder = rawObj => rawObj.Build(objectId);

            //TODO: RawObject should be freed when the Repository is disposed (cf. https://github.com/libgit2/libgit2/blob/6fd195d76c7f52baae5540e287affe2259900d36/tests/t0205-readheader.c#L202)
            return ReadInternal(objectId, reader, builder);
        }

        public bool Exists(string objectId)
        {
            return NativeMethods.wrapped_git_odb_exists(_lifecycleManager.RepositoryPtr, objectId);
        }

        private delegate OperationResult DatabaseReader(out git_rawobj rawobj, IntPtr repository, string objectId);

        private TType ReadInternal<TType>(string objectId, DatabaseReader reader, Func<git_rawobj, TType> builder)
        {
            git_rawobj rawObj;
            OperationResult result = reader(out rawObj, _lifecycleManager.RepositoryPtr, objectId);

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

        public static string Init(string path, bool isBare)
        {
            string repositoryDirectory;

            using (var lifecycleManager = new RepositoryLifecycleManager(path, isBare))
            {
                repositoryDirectory = lifecycleManager.Details.RepositoryDirectory;
            }

            return repositoryDirectory;
        }

        public void Dispose()
        {
            _lifecycleManager.Dispose();
        }

        public object Resolve(string identifier, Type expectedType)
        {
            if (ObjectId.IsValid(identifier))
            {
                return _objectResolver.Resolve(identifier, expectedType);
            }

            Ref reference = _refsResolver.Resolve(identifier);
            if (reference == null)
            {
                return null;
            }

            return _objectResolver.Resolve(reference.Target, expectedType);
        }

        public Tag ApplyTag(string targetId, string tagName, string tagMessage, Signature signature)
        {
            // TODO: To be refactored.
            IntPtr tag;
            var when = signature.When.ToGitDate();
            OperationResult result = NativeMethods.wrapped_git_apply_tag(out tag, _lifecycleManager.RepositoryPtr, targetId, tagName, tagMessage, signature.Name, signature.Email, (ulong)when.UnixTimeStamp, when.TimeZoneOffset);

            switch (result)
            {
                case OperationResult.GIT_SUCCESS:
                    return (Tag)_builder.BuildFrom(tag, ObjectType.Tag);

                case OperationResult.GIT_ENOTFOUND:
                    throw new ObjectNotFoundException();

                default:
                    throw new Exception(Enum.GetName(typeof(OperationResult), result));
            }
        }
    }
}