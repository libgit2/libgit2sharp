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