using System;
using LibGit2Sharp.Wrapper;

namespace LibGit2Sharp
{
    public sealed class Repository : IObjectResolver, IDisposable
    {
        private readonly IObjectResolver _objectResolver;
        private readonly ILifecycleManager _lifecycleManager;
        private readonly IBuilder _builder;
        private readonly IReferenceManager _referenceManager;

        public RepositoryDetails Details
        {
            get { return _lifecycleManager.Details; }
        }

        public IReferenceManager Refs
        {
            get { return _referenceManager; }
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
            _objectResolver = new ObjectResolver(_lifecycleManager.CoreRepository, _builder);
            _referenceManager = new ReferenceManager(_lifecycleManager.CoreRepository);
        }

        public Header ReadHeader(string objectId)
        {
            Func<Core.RawObject, Header> builder = rawObj => { 
                return new Header(objectId, (ObjectType)rawObj.Type, rawObj.Length);
            };
			
            return ReadInternal(objectId, builder);
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
        }
		
        private TType ReadInternal<TType>(string objectid, Func<Core.RawObject, TType> builder) 
        {
            var rawObj = _lifecycleManager.CoreRepository.Database.ReadHeader(new Core.ObjectId(objectid));
            
            return builder(rawObj);
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

            Ref reference = Refs.Lookup(identifier, true);
            if (reference == null)
            {
                return null;
            }

            return _objectResolver.Resolve(reference.Target, expectedType);
        }

        public Tag ApplyTag(string targetId, string tagName, string tagMessage, Signature signature)
        {
            Core.Repository coreRepository = _lifecycleManager.CoreRepository;
            
            Core.Tag tag = new Core.Tag(coreRepository);
            tag.Name = tagName;
            tag.Message = tagMessage;
            tag.Target = coreRepository.Lookup(new Core.ObjectId(targetId));
            // TODO: add time
            tag.Tagger = new Core.Signature(signature.Name, signature.Email);

            // TODO: catch and rethrow exceptions
            tag.Write();

            return (Tag)_builder.BuildFrom(tag);
        }
    }
}