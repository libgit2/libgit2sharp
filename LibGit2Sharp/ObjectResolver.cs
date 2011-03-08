using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Wrapper;

namespace LibGit2Sharp
{
    public class ObjectResolver : IObjectResolver
    {
        private readonly Core.Repository _coreRepository = null;
        private readonly IBuilder _builder;

        private static readonly IDictionary<ObjectType, Type> TypeMapper = new Dictionary<ObjectType, Type>
                                                                  {
                                                                      {ObjectType.Blob, typeof(Blob)},
                                                                      {ObjectType.Commit, typeof(Commit)},
                                                                      {ObjectType.Tag, typeof(Tag)},
                                                                      {ObjectType.Tree, typeof(Tree)},
                                                                  };

        private static readonly IDictionary<Type, ObjectType> ReverseTypeMapper =
            TypeMapper.ToDictionary(kv => kv.Value, kv => kv.Key);

        public ObjectResolver(Core.Repository coreRepository, IBuilder builder)
        {
			
            #region Parameters Validation

            if (coreRepository == null)
            {
                throw new ArgumentNullException("repositoryPtr");
            }

            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }

            #endregion

			_coreRepository = coreRepository;
            _builder = builder;
        }

        public object Resolve(string objectId, Type expectedType)
        {
			
            if (!typeof(GitObject).IsAssignableFrom(expectedType))
            {
                throw new ArgumentException("Only types deriving from GitObject are allowed.", "expectedType");
            }
            
            var expected = Core.git_otype.GIT_OBJ_ANY;

            if (expectedType != typeof(GitObject))
            {
                expected = (Core.git_otype)ReverseTypeMapper[expectedType];
            }
            
            Core.GitObject obj = _coreRepository.Lookup(new Core.ObjectId(objectId));
            
            var expectedTypeHasBeenRetrieved = expected == Core.git_otype.GIT_OBJ_ANY || obj.Type == expected;
            
            if (!expectedTypeHasBeenRetrieved)
            {
                return null;
            }
            
            return _builder.BuildFrom(obj);
        }
    }
}
