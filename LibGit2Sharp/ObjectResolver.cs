/*
 * The MIT License
 *
 * Copyright (c) 2011 Emeric Fermas
 * Copyright (c) 2011 Andrius Bentkus
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
