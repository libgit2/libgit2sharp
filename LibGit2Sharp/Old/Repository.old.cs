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
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public sealed class Repository : IObjectResolver, IDisposable
    {
        private readonly ObjectResolver _objectResolver;
        private readonly RepositoryLifecycleManager _lifecycleManager;
        private readonly ObjectBuilder _builder;
        private readonly ReferenceManager _referenceManager;

        public RepositoryDetails Details
        {
            get { return _lifecycleManager.Details; }
        }

        public ReferenceManager Refs
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

        private Repository(RepositoryLifecycleManager lifecycleManager)
        {
            _lifecycleManager = lifecycleManager;
            _builder = new ObjectBuilder();
            _objectResolver = new ObjectResolver(_lifecycleManager.CoreRepository, _builder);
            _referenceManager = new ReferenceManager(_lifecycleManager.CoreRepository);
        }

        public bool Exists(string objectId)
        {
            return _lifecycleManager.CoreRepository.Database.Exists(new Core.ObjectId(objectId));
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

            if (DoesReferenceExist(tagName)) //TODO: Remove when tag_create() implement checking of existing conflicting reference
            {
                throw new InvalidReferenceNameException();
            }

            Core.GitObject target = coreRepository.Lookup(new Core.ObjectId(targetId)); //TODO: Remove when tag_create() implement checking of target existence
            var tagger = new Core.Signature(signature.Name, signature.Email, signature.When);

            var tagOid = Core.Tag.Create(coreRepository,
                                      tagName,
                                      target.ObjectId,
                                      target.Type,
                                      tagger,
                                      tagMessage);

            tagger.Free();

            return (Tag)_builder.BuildFrom(coreRepository.Lookup(tagOid, git_otype.GIT_OBJ_TAG));
        }

        private bool DoesReferenceExist(string tagName)
        {
            try
            {
                Refs.Lookup("refs/tags/" + tagName, false);
            }
            catch (ObjectNotFoundException e)
            {
                return false;
            }

            return true;
        }
    }
}
