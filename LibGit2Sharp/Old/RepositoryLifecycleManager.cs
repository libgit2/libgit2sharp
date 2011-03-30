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
using System.IO;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    public sealed class RepositoryLifecycleManager : IDisposable
    {
        internal Core.Repository CoreRepository { get; private set; }
		
        public RepositoryDetails Details { get; private set; }

        public RepositoryLifecycleManager(string initializationDirectory, bool isBare)
        {
            #region Parameters Validation

            if (string.IsNullOrEmpty("initializationDirectory"))
            {
                throw new ArgumentNullException("initializationDirectory");
            }

            #endregion Parameters Validation

            OpenRepository(Core.Repository.Init(Posixify(initializationDirectory), isBare));
        }

        public RepositoryLifecycleManager(string repositoryDirectory)
        {
            #region Parameters Validation

            if (string.IsNullOrEmpty(repositoryDirectory))
            {
                throw new ArgumentNullException("repositoryDirectory");
            }

            #endregion Parameters Validation

            OpenRepository(new Core.Repository(Posixify(repositoryDirectory)));
        }

        public RepositoryLifecycleManager(string repositoryDirectory, string databaseDirectory, string index, string workingDirectory)
        {
            #region Parameters Validation

            if (string.IsNullOrEmpty(repositoryDirectory))
            {
                throw new ArgumentNullException("repositoryDirectory");
            }

            if (string.IsNullOrEmpty(databaseDirectory))
            {
                throw new ArgumentNullException("databaseDirectory");
            }

            if (string.IsNullOrEmpty(index))
            {
                throw new ArgumentNullException("index");
            }

            if (string.IsNullOrEmpty(workingDirectory))
            {
                throw new ArgumentNullException("workingDirectory");
            }

            #endregion Parameters Validation

            OpenRepository(new Core.Repository(Posixify(repositoryDirectory),
                                               Posixify(databaseDirectory),
                                               Posixify(index),
                                               Posixify(workingDirectory)));
        }

        private static string Posixify(string path)
        {
            if (Path.DirectorySeparatorChar == Constants.DirectorySeparatorChar)
            {
                return path;
            }

            return path.Replace(Path.DirectorySeparatorChar, Constants.DirectorySeparatorChar);
        }

        private void OpenRepository(Core.Repository repository)
        {
            CoreRepository = repository;
            Details = BuildRepositoryDetails(repository);
        }

        private static RepositoryDetails BuildRepositoryDetails(Core.Repository coreRepository)
        {
            return new RepositoryDetails(coreRepository.RepositoryDirectory,
                                         coreRepository.IndexFile,
                                         coreRepository.DatabaseDirectory,
                                         coreRepository.WorkingDirectory,
                                         coreRepository.IsBare);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (CoreRepository == null)
            {
                return;
            }

            CoreRepository.Dispose();
            CoreRepository = null;
        }

        ~RepositoryLifecycleManager()
        {
            Dispose(false);
        }
    }
}
