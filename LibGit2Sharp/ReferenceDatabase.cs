using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Provides methods to directly work against the Git reference database.
    /// </summary>
    public class ReferenceDatabase
    {
        private readonly Repository repo;
        private readonly ReferenceDatabaseSafeHandle handle;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected ReferenceDatabase()
        { }

        internal ReferenceDatabase(Repository repo)
        {
            this.repo = repo;
            handle = Proxy.git_repository_refdb(repo.Handle);

            repo.RegisterForCleanup(handle);
        }

        internal ReferenceDatabaseSafeHandle Handle
        {
            get { return handle; }
        }

        /// <summary>
        ///   Sets the provided backend to be the reference database provider.
        /// </summary>
        /// <param name="backend">The backend to add</param>
        public virtual void SetBackend(RefdbBackend backend)
        {
            Ensure.ArgumentNotNull(backend, "backend");

            Proxy.git_refdb_set_backend(this.handle, backend.GitRefdbBackendPointer);
        }
    }
}
