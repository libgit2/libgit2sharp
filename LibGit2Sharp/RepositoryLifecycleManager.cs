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
using System.IO;
using System.Runtime.InteropServices;
using LibGit2Sharp.Wrapper;

namespace LibGit2Sharp
{
    public sealed class RepositoryLifecycleManager : ILifecycleManager
    {
        public Core.Repository CoreRepository { get; private set; }
		
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
            if (CoreRepository != null)
            {
            	CoreRepository.Dispose();
            	CoreRepository = null;
            }
        }

        ~RepositoryLifecycleManager()
        {
            Dispose(false);
        }
    }
}
