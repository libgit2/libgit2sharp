using System;
using System.Collections.Generic;
using System.Linq;
using libgit2sharp.Wrapper;

namespace libgit2sharp
{
    public class ObjectResolver : IResolver
    {
        private readonly IntPtr _repositoryPtr = IntPtr.Zero;
        private readonly IObjectHeaderReader _objectHeaderReader;
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

        public ObjectResolver(IntPtr repositoryPtr, IObjectHeaderReader objectHeaderReader, IBuilder builder)
        {
            #region Parameters Validation
            
            if (repositoryPtr == IntPtr.Zero)
            {
                throw new ArgumentNullException("repositoryPtr");
            }

            if (objectHeaderReader == null)
            {
                throw new ArgumentNullException("objectHeaderReader");
            }

            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }

            #endregion
            
            _repositoryPtr = repositoryPtr;
            _builder = builder;
            _objectHeaderReader = objectHeaderReader;
        }

        public GitObject Resolve(string objectId)
        {
            return Resolve<GitObject>(objectId);
        }

        public TType Resolve<TType>(string objectId)
        {
            return (TType)Resolve(objectId, typeof(TType));
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

            ObjectType expectedObjectType = ReverseTypeMapper[outputType];

            IntPtr gitObjectPtr;
            OperationResult result = LibGit2Api.wrapped_git_repository_lookup(out gitObjectPtr, _repositoryPtr,
                                                                              objectId,
                                                                              (git_otype)expectedObjectType);

            switch (result)
            {
                case OperationResult.GIT_SUCCESS:
                    return _builder.BuildFrom(gitObjectPtr, expectedObjectType);

                case OperationResult.GIT_ENOTFOUND:
                case OperationResult.GIT_EINVALIDTYPE:
                    return null;

                default:
                    throw new Exception(Enum.GetName(typeof(OperationResult), result));
            }
        }

        private Type GuessTypeToResolve(string objectId)
        {
            Header header = _objectHeaderReader.ReadHeader(objectId);

            if (header == null)
            {
                return null;
            }

            return TypeMapper[header.Type];
        }
    }
}