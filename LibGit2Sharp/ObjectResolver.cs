/*
 * The MIT License
 *
 * Copyright (c) 2011 LibGit2Sharp committers
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public class ObjectResolver : IObjectResolver
    {
        private readonly Core.Repository _coreRepository = null;
        private readonly ObjectBuilder _builder;

        private static readonly IDictionary<ObjectType, Type> _typeMapper = new Dictionary<ObjectType, Type>
                                                                  {
                                                                      {ObjectType.Blob, typeof(Blob)},
                                                                      {ObjectType.Commit, typeof(Commit)},
                                                                      {ObjectType.Tag, typeof(Tag)},
                                                                      {ObjectType.Tree, typeof(Tree)},
                                                                  };

        private static readonly IDictionary<Type, ObjectType> _reverseTypeMapper =
            _typeMapper.ToDictionary(kv => kv.Value, kv => kv.Key);

        public ObjectResolver(Core.Repository coreRepository, ObjectBuilder builder)
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
            
            var expected = git_otype.GIT_OBJ_ANY;

            if (expectedType != typeof(GitObject))
            {
                expected = (git_otype)_reverseTypeMapper[expectedType];
            }

            Core.GitObject obj;
            try
            {
                obj = _coreRepository.Lookup(new Core.ObjectId(objectId), expected);
            }
            catch (GitException e)
            {
                if (e is ObjectNotFoundException || e is InvalidTypeException)
                {
                    return null;
                }

                throw;
            }
            
            var expectedTypeHasBeenRetrieved = expected == git_otype.GIT_OBJ_ANY || obj.Type == expected;
            
            if (!expectedTypeHasBeenRetrieved)
            {
                return null;
            }
            
            return _builder.BuildFrom(obj);
        }
    }
}
