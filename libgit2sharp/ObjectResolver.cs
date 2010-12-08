using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using libgit2sharp.Wrapper;

namespace libgit2sharp
{
    public class ObjectResolver : IResolver
    {
        private readonly IntPtr _repositoryPtr = IntPtr.Zero;
        private readonly IObjectHeaderReader _objectHeaderReader;

        private static readonly IDictionary<ObjectType, Type> TypeMapper = new Dictionary<ObjectType, Type>
                                                                  {
                                                                      {ObjectType.Blob, typeof(Blob)},
                                                                      {ObjectType.Commit, typeof(Commit)},
                                                                      {ObjectType.Tag, typeof(Tag)},
                                                                      {ObjectType.Tree, typeof(Tree)},
                                                                  };

        private static readonly IDictionary<Type, ObjectType> ReverseTypeMapper =
            TypeMapper.ToDictionary(kv => kv.Value, kv => kv.Key);

        private static readonly IDictionary<Type, Func<string, IntPtr, object>> BuilderMapper = new Dictionary<Type, Func<string, IntPtr, object>>
                                                                  {
                                                                      {typeof(Blob), BuildBlob},
                                                                      {typeof(Commit), BuildCommit},
                                                                      {typeof(Tag), BuildTag},
                                                                      {typeof(Tree), BuildTree},
                                                                  };

        public ObjectResolver(IntPtr repositoryPtr, IObjectHeaderReader objectHeaderReader)
        {
            if (repositoryPtr == IntPtr.Zero)
            {
                throw new ArgumentNullException("repositoryPtr");
            }

            if (objectHeaderReader == null)
            {
                throw new ArgumentNullException("objectHeaderReader");
            }

            _repositoryPtr = repositoryPtr;
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
                    return BuilderMapper[outputType](objectId, gitObjectPtr);

                case OperationResult.GIT_ENOTFOUND:
                //TODO: Should we free gitObjectPtr if OperationResult.GIT_EINVALIDTYPE is returned ?
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

    }
}