using System;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides methods to directly work against the Git object database
    ///   without involving the index nor the working directory.
    /// </summary>
    public class ObjectDatabase
    {
        private readonly Repository repo;
        private readonly ObjectDatabaseSafeHandle handle;

        internal ObjectDatabase(Repository repo)
        {
            this.repo = repo;
            Ensure.Success(NativeMethods.git_repository_odb(out handle, repo.Handle));

            repo.RegisterForCleanup(handle);
        }

        /// <summary>
        ///   Determines if the given object can be found in the object database.
        /// </summary>
        /// <param name="objectId">Identifier of the object being searched for.</param>
        /// <returns>True if the object has been found; false otherwise.</returns>
        public bool Contains(ObjectId objectId)
        {
            var oid = objectId.Oid;

            return NativeMethods.git_odb_exists(handle, ref oid) != (int)GitErrorCode.GIT_SUCCESS;
        }

        /// <summary>
        ///   Inserts a <see cref="Blob"/> into the object database, created from the content of a file.
        /// </summary>
        /// <param name="path">Relative path to the file in the working directory.</param>
        /// <returns>The created <see cref="Blob"/>.</returns>
        public Blob CreateBlob(string path)
        {
            if (repo.Info.IsBare)
            {
                throw new NotImplementedException();
            }

            var oid = new GitOid();
            Ensure.Success(NativeMethods.git_blob_create_fromfile(ref oid, repo.Handle, path));
            return repo.Lookup<Blob>(new ObjectId(oid));
        }
    }
}
