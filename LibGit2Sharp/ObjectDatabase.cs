using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            return NativeMethods.git_odb_exists(handle, ref oid) != (int)GitErrorCode.Ok;
        }

        /// <summary>
        ///   Inserts a <see cref="Blob"/> into the object database, created from the content of a file.
        /// </summary>
        /// <param name="path">Path to the file to create the blob from.</param>
        /// <returns>The created <see cref="Blob"/>.</returns>
        public Blob CreateBlob(string path)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            var oid = new GitOid();

            if (!repo.Info.IsBare && !Path.IsPathRooted(path))
            {
                Ensure.Success(NativeMethods.git_blob_create_fromfile(ref oid, repo.Handle, path));
            }
            else
            {
                Ensure.Success(NativeMethods.git_blob_create_fromdisk(ref oid, repo.Handle, path));
            }

            return repo.Lookup<Blob>(new ObjectId(oid));
        }

        /// <summary>
        ///   Inserts a <see cref = "Tree"/> into the object database, created from a <see cref = "TreeDefinition"/>.
        /// </summary>
        /// <param name = "treeDefinition">The <see cref = "TreeDefinition"/>.</param>
        /// <returns>The created <see cref = "Tree"/>.</returns>
        public Tree CreateTree(TreeDefinition treeDefinition)
        {
            return treeDefinition.Build(repo);
        }

        /// <summary>
        ///   Inserts a <see cref = "Commit"/> into the object database, referencing an existing <see cref = "Tree"/>.
        /// </summary>
        /// <param name = "message">The description of why a change was made to the repository.</param>
        /// <param name = "author">The <see cref = "Signature" /> of who made the change.</param>
        /// <param name = "committer">The <see cref = "Signature" /> of who added the change to the repository.</param>
        /// <param name = "tree">The <see cref = "Tree"/> of the <see cref = "Commit"/> to be created.</param>
        /// <param name = "parents">The parents of the <see cref = "Commit"/> to be created.</param>
        /// <returns>The created <see cref = "Commit"/>.</returns>
        public ICommit CreateCommit(string message, Signature author, Signature committer, Tree tree, IEnumerable<ICommit> parents)
        {
            return CreateCommit(message, author, committer, tree, parents, null);
        }

        internal ICommit CreateCommit(string message, Signature author, Signature committer, Tree tree, IEnumerable<ICommit> parents, string referenceName)
        {
            Ensure.ArgumentNotNull(message, "message");
            Ensure.ArgumentNotNull(author, "author");
            Ensure.ArgumentNotNull(committer, "committer");
            Ensure.ArgumentNotNull(tree, "tree");
            Ensure.ArgumentNotNull(parents, "parents");

            IEnumerable<ObjectId> parentIds = parents.Select(p => p.Id);

            GitOid commitOid;
            using (var treePtr = new ObjectSafeWrapper(tree.Id, repo))
            using (var parentObjectPtrs = new DisposableEnumerable<ObjectSafeWrapper>(parentIds.Select(id => new ObjectSafeWrapper(id, repo))))
            using (SignatureSafeHandle authorHandle = author.BuildHandle())
            using (SignatureSafeHandle committerHandle = committer.BuildHandle())
            {
                string encoding = null; //TODO: Handle the encoding of the commit to be created

                IntPtr[] parentsPtrs = parentObjectPtrs.Select(o => o.ObjectPtr.DangerousGetHandle()).ToArray();
                int res = NativeMethods.git_commit_create(out commitOid, repo.Handle, referenceName, authorHandle,
                                                      committerHandle, encoding, message, treePtr.ObjectPtr, parentObjectPtrs.Count(), parentsPtrs);
                Ensure.Success(res);
            }

            return repo.Lookup<Commit>(new ObjectId(commitOid));
        }
    }
}
