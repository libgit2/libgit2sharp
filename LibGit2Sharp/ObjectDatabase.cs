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
        private readonly ObjectDatabaseSafeHandle handle;

        internal ObjectDatabase(Repository repo)
        {
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
    }
}
