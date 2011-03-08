/*
 * This file is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License, version 2,
 * as published by the Free Software Foundation.
 *
 * In addition to the permissions in the GNU General Public License,
 * the authors give you unlimited permission to link the compiled
 * version of this file into combinations with other programs,
 * and to distribute those combinations without any restriction
 * coming from the use of this file.  (The General Public License
 * restrictions do apply in other respects; for example, they cover
 * modification of the file, and distribution when not linked into
 * a combined executable.)
 *
 * This file is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 51 Franklin Street, Fifth Floor,
 * Boston, MA 02110-1301, USA.
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
