using System;
using System.Collections.Generic;
using System.Linq;
using libgit2sharp.Wrapper;

namespace libgit2sharp
{
    public class ObjectResolver : IObjectResolver
    {
        private readonly IntPtr _repositoryPtr = IntPtr.Zero;
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

        public ObjectResolver(IntPtr repositoryPtr, IBuilder builder)
        {
            #region Parameters Validation

            if (repositoryPtr == IntPtr.Zero)
            {
                throw new ArgumentNullException("repositoryPtr");
            }

            if (builder == null)
            {
                throw new ArgumentNullException("builder");
            }

            #endregion

            _repositoryPtr = repositoryPtr;
            _builder = builder;
        }

        public object Resolve(string objectId, Type expectedType)
        {
            if (!typeof(GitObject).IsAssignableFrom(expectedType))
            {
                throw new ArgumentException("Only types deriving from GitObject are allowed.", "expectedType");
            }

            var expected = git_otype.GIT_OBJ_ANY;

            if (expectedType != typeof(GitObject))
            {
                expected = (git_otype)ReverseTypeMapper[expectedType];
            }

            IntPtr gitObjectPtr;

            git_otype retrieved;
            OperationResult result = LibGit2Api.wrapped_git_repository_lookup(out gitObjectPtr, out retrieved, _repositoryPtr, objectId);

            var expectedTypeHasBeenRetrieved = expected == git_otype.GIT_OBJ_ANY || retrieved == expected;

            if (result == OperationResult.GIT_SUCCESS && !expectedTypeHasBeenRetrieved)
            {
                result = OperationResult.GIT_ENOTFOUND;
            }

            switch (result)
            {
                case OperationResult.GIT_SUCCESS:
                    return _builder.BuildFrom(gitObjectPtr, (ObjectType)retrieved);

                case OperationResult.GIT_ENOTFOUND:
                case OperationResult.GIT_EINVALIDTYPE:
                    return null;

                default:
                    throw new Exception(Enum.GetName(typeof(OperationResult), result));
            }
        }
    }
}