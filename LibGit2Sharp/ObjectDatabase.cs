using System;
using System.Collections;
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
    /// Provides methods to directly work against the Git object database
    /// without involving the index nor the working directory.
    /// </summary>
    public class ObjectDatabase : IEnumerable<GitObject>
    {
        private readonly Repository repo;
        private readonly ObjectDatabaseSafeHandle handle;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected ObjectDatabase()
        { }

        internal ObjectDatabase(Repository repo)
        {
            this.repo = repo;
            handle = Proxy.git_repository_odb(repo.Handle);

            repo.RegisterForCleanup(handle);
        }

        #region Implementation of IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<GitObject> GetEnumerator()
        {
            ICollection<GitOid> oids = Proxy.git_odb_foreach(handle,
                ptr => (GitOid) Marshal.PtrToStructure(ptr, typeof (GitOid)));

            return oids
                .Select(gitOid => repo.Lookup<GitObject>(new ObjectId(gitOid)))
                .GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Determines if the given object can be found in the object database.
        /// </summary>
        /// <param name="objectId">Identifier of the object being searched for.</param>
        /// <returns>True if the object has been found; false otherwise.</returns>
        public virtual bool Contains(ObjectId objectId)
        {
            Ensure.ArgumentNotNull(objectId, "objectId");

            return Proxy.git_odb_exists(handle, objectId);
        }

        /// <summary>
        /// Inserts a <see cref="Blob"/> into the object database, created from the content of a file.
        /// </summary>
        /// <param name="path">Path to the file to create the blob from.  A relative path is allowed to
        /// be passed if the <see cref="Repository"/> is a standard, non-bare, repository. The path
        /// will then be considered as a path relative to the root of the working directory.</param>
        /// <returns>The created <see cref="Blob"/>.</returns>
        public virtual Blob CreateBlob(string path)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            if (repo.Info.IsBare && !Path.IsPathRooted(path))
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture,
                        "Cannot create a blob in a bare repository from a relative path ('{0}').", path));
            }

            ObjectId id = Path.IsPathRooted(path)
                               ? Proxy.git_blob_create_fromdisk(repo.Handle, path)
                               : Proxy.git_blob_create_fromfile(repo.Handle, path);

            return repo.Lookup<Blob>(id);
        }

        /// <summary>
        /// Adds the provided backend to the object database with the specified priority.
        /// </summary>
        /// <param name="backend">The backend to add</param>
        /// <param name="priority">The priority at which libgit2 should consult this backend (higher values are consulted first)</param>
        public virtual void AddBackend(OdbBackend backend, int priority)
        {
            Ensure.ArgumentNotNull(backend, "backend");
            Ensure.ArgumentConformsTo(priority, s => s > 0, "priority");

            Proxy.git_odb_add_backend(this.handle, backend.GitOdbBackendPointer, priority);
        }

        private class Processor
        {
            private readonly Stream stream;
            private readonly int? numberOfBytesToConsume;
            private int totalNumberOfReadBytes;

            public Processor(Stream stream, int? numberOfBytesToConsume)
            {
                this.stream = stream;
                this.numberOfBytesToConsume = numberOfBytesToConsume;
            }

            public int Provider(IntPtr content, int max_length, IntPtr data)
            {
                var local = new byte[max_length];

                int bytesToRead = max_length;

                if (numberOfBytesToConsume.HasValue)
                {
                    int totalRemainingBytesToRead = numberOfBytesToConsume.Value - totalNumberOfReadBytes;

                    if (totalRemainingBytesToRead < max_length)
                    {
                        bytesToRead = totalRemainingBytesToRead;
                    }
                }

                int numberOfReadBytes = stream.Read(local, 0, bytesToRead);
                totalNumberOfReadBytes += numberOfReadBytes;

                Marshal.Copy(local, 0, content, numberOfReadBytes);

                return numberOfReadBytes;
            }
        }

        /// <summary>
        /// Inserts a <see cref="Blob"/> into the object database, created from the content of a data provider.
        /// </summary>
        /// <param name="stream">The stream from which will be read the content of the blob to be created.</param>
        /// <param name="hintpath">The hintpath is used to determine what git filters should be applied to the object before it can be placed to the object database.</param>
        /// <param name="numberOfBytesToConsume">The number of bytes to consume from the stream.</param>
        /// <returns>The created <see cref="Blob"/>.</returns>
        public virtual Blob CreateBlob(Stream stream, string hintpath = null, int? numberOfBytesToConsume = null)
        {
            Ensure.ArgumentNotNull(stream, "stream");

            if (!stream.CanRead)
            {
                throw new ArgumentException("The stream cannot be read from.", "stream");
            }

            var proc = new Processor(stream, numberOfBytesToConsume);
            ObjectId id = Proxy.git_blob_create_fromchunks(repo.Handle, hintpath, proc.Provider);

            return repo.Lookup<Blob>(id);
        }

        /// <summary>
        /// Inserts a <see cref="Tree"/> into the object database, created from a <see cref="TreeDefinition"/>.
        /// </summary>
        /// <param name="treeDefinition">The <see cref="TreeDefinition"/>.</param>
        /// <returns>The created <see cref="Tree"/>.</returns>
        public virtual Tree CreateTree(TreeDefinition treeDefinition)
        {
            Ensure.ArgumentNotNull(treeDefinition, "treeDefinition");

            return treeDefinition.Build(repo);
        }

        /// <summary>
        /// Inserts a <see cref="Commit"/> into the object database, referencing an existing <see cref="Tree"/>.
        /// </summary>
        /// <param name="message">The description of why a change was made to the repository.</param>
        /// <param name="author">The <see cref="Signature"/> of who made the change.</param>
        /// <param name="committer">The <see cref="Signature"/> of who added the change to the repository.</param>
        /// <param name="tree">The <see cref="Tree"/> of the <see cref="Commit"/> to be created.</param>
        /// <param name="parents">The parents of the <see cref="Commit"/> to be created.</param>
        /// <returns>The created <see cref="Commit"/>.</returns>
        public virtual Commit CreateCommit(string message, Signature author, Signature committer, Tree tree, IEnumerable<Commit> parents)
        {
            return CreateCommit(message, author, committer, tree, parents, null);
        }

        internal Commit CreateCommit(string message, Signature author, Signature committer, Tree tree, IEnumerable<Commit> parents, string referenceName)
        {
            Ensure.ArgumentNotNull(message, "message");
            Ensure.ArgumentNotNull(author, "author");
            Ensure.ArgumentNotNull(committer, "committer");
            Ensure.ArgumentNotNull(tree, "tree");
            Ensure.ArgumentNotNull(parents, "parents");

            string prettifiedMessage = Proxy.git_message_prettify(message);
            GitOid[] parentIds = parents.Select(p => p.Id.Oid).ToArray();

            ObjectId commitId = Proxy.git_commit_create(repo.Handle, referenceName, author, committer, prettifiedMessage, tree, parentIds);

            return repo.Lookup<Commit>(commitId);
        }

        /// <summary>
        /// Inserts a <see cref="TagAnnotation"/> into the object database, pointing to a specific <see cref="GitObject"/>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="target">The <see cref="GitObject"/> being pointed at.</param>
        /// <param name="tagger">The tagger.</param>
        /// <param name="message">The message.</param>
        /// <returns>The created <see cref="TagAnnotation"/>.</returns>
        public virtual TagAnnotation CreateTagAnnotation(string name, GitObject target, Signature tagger, string message)
        {
            string prettifiedMessage = Proxy.git_message_prettify(message);

            ObjectId tagId = Proxy.git_tag_annotation_create(repo.Handle, name, target, tagger, prettifiedMessage);

            return repo.Lookup<TagAnnotation>(tagId);
        }
    }
}
