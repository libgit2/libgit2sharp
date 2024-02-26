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
        private readonly ObjectDatabaseHandle handle;

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
            ICollection<ObjectId> oids = Proxy.git_odb_foreach(handle);

            return oids
                .Select(gitOid => repo.Lookup<GitObject>(gitOid))
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
        /// Retrieves the header of a GitObject from the object database. The header contains the Size
        /// and Type of the object. Note that most backends do not support reading only the header
        /// of an object, so the whole object will be read and then size would be returned.
        /// </summary>
        /// <param name="objectId">Object Id of the queried object</param>
        /// <returns>GitObjectMetadata object instance containg object header information</returns>
        public virtual GitObjectMetadata RetrieveObjectMetadata(ObjectId objectId)
        {
            Ensure.ArgumentNotNull(objectId, "objectId");

            return Proxy.git_odb_read_header(handle, objectId);
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
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                    "Cannot create a blob in a bare repository from a relative path ('{0}').",
                                                    path));
            }

            ObjectId id = Path.IsPathRooted(path)
                ? Proxy.git_blob_create_from_disk(repo.Handle, path)
                : Proxy.git_blob_create_from_workdir(repo.Handle, path);

            return repo.Lookup<Blob>(id);
        }

        /// <summary>
        /// Adds the provided backend to the object database with the specified priority.
        /// <para>
        /// If the provided backend implements <see cref="IDisposable"/>, the <see cref="IDisposable.Dispose"/>
        /// method will be honored and invoked upon the disposal of the repository.
        /// </para>
        /// </summary>
        /// <param name="backend">The backend to add</param>
        /// <param name="priority">The priority at which libgit2 should consult this backend (higher values are consulted first)</param>
        public virtual void AddBackend(OdbBackend backend, int priority)
        {
            Ensure.ArgumentNotNull(backend, "backend");
            Ensure.ArgumentConformsTo(priority, s => s > 0, "priority");

            Proxy.git_odb_add_backend(handle, backend.GitOdbBackendPointer, priority);
        }

        private class Processor
        {
            private readonly Stream stream;
            private readonly long? numberOfBytesToConsume;
            private int totalNumberOfReadBytes;

            public Processor(Stream stream, long? numberOfBytesToConsume)
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
                    long totalRemainingBytesToRead = numberOfBytesToConsume.Value - totalNumberOfReadBytes;

                    if (totalRemainingBytesToRead < max_length)
                    {
                        bytesToRead = totalRemainingBytesToRead > int.MaxValue
                            ? int.MaxValue
                            : (int)totalRemainingBytesToRead;
                    }
                }

                if (bytesToRead == 0)
                {
                    return 0;
                }

                int numberOfReadBytes = stream.Read(local, 0, bytesToRead);

                if (numberOfBytesToConsume.HasValue && numberOfReadBytes == 0)
                {
                    return (int)GitErrorCode.User;
                }

                totalNumberOfReadBytes += numberOfReadBytes;

                Marshal.Copy(local, 0, content, numberOfReadBytes);

                return numberOfReadBytes;
            }
        }

        /// <summary>
        /// Writes an object to the object database.
        /// </summary>
        /// <param name="data">The contents of the object</param>
        /// <typeparam name="T">The type of object to write</typeparam>
        public virtual ObjectId Write<T>(byte[] data) where T : GitObject
        {
            return Proxy.git_odb_write(handle, data, GitObject.TypeToKindMap[typeof(T)]);
        }

        /// <summary>
        /// Writes an object to the object database.
        /// </summary>
        /// <param name="stream">The contents of the object</param>
        /// <param name="numberOfBytesToConsume">The number of bytes to consume from the stream</param>
        /// <typeparam name="T">The type of object to write</typeparam>
        public virtual ObjectId Write<T>(Stream stream, long numberOfBytesToConsume) where T : GitObject
        {
            Ensure.ArgumentNotNull(stream, "stream");

            if (!stream.CanRead)
            {
                throw new ArgumentException("The stream cannot be read from.", "stream");
            }

            using (var odbStream = Proxy.git_odb_open_wstream(handle, numberOfBytesToConsume, GitObjectType.Blob))
            {
                var buffer = new byte[4 * 1024];
                long totalRead = 0;

                while (totalRead < numberOfBytesToConsume)
                {
                    long left = numberOfBytesToConsume - totalRead;
                    int toRead = left < buffer.Length ? (int)left : buffer.Length;
                    var read = stream.Read(buffer, 0, toRead);

                    if (read == 0)
                    {
                        throw new EndOfStreamException("The stream ended unexpectedly");
                    }

                    Proxy.git_odb_stream_write(odbStream, buffer, read);
                    totalRead += read;
                }

                return Proxy.git_odb_stream_finalize_write(odbStream);
            }
        }

        /// <summary>
        /// Inserts a <see cref="Blob"/> into the object database, created from the content of a stream.
        /// <para>Optionally, git filters will be applied to the content before storing it.</para>
        /// </summary>
        /// <param name="stream">The stream from which will be read the content of the blob to be created.</param>
        /// <returns>The created <see cref="Blob"/>.</returns>
        public virtual Blob CreateBlob(Stream stream)
        {
            return CreateBlob(stream, null, null);
        }

        /// <summary>
        /// Inserts a <see cref="Blob"/> into the object database, created from the content of a stream.
        /// <para>Optionally, git filters will be applied to the content before storing it.</para>
        /// </summary>
        /// <param name="stream">The stream from which will be read the content of the blob to be created.</param>
        /// <param name="hintpath">The hintpath is used to determine what git filters should be applied to the object before it can be placed to the object database.</param>
        /// <returns>The created <see cref="Blob"/>.</returns>
        public virtual Blob CreateBlob(Stream stream, string hintpath)
        {
            return CreateBlob(stream, hintpath, null);
        }

        /// <summary>
        /// Inserts a <see cref="Blob"/> into the object database, created from the content of a stream.
        /// <para>Optionally, git filters will be applied to the content before storing it.</para>
        /// </summary>
        /// <param name="stream">The stream from which will be read the content of the blob to be created.</param>
        /// <param name="hintpath">The hintpath is used to determine what git filters should be applied to the object before it can be placed to the object database.</param>
        /// <param name="numberOfBytesToConsume">The number of bytes to consume from the stream.</param>
        /// <returns>The created <see cref="Blob"/>.</returns>
        public virtual Blob CreateBlob(Stream stream, string hintpath, long numberOfBytesToConsume)
        {
            return CreateBlob(stream, hintpath, (long?)numberOfBytesToConsume);
        }

        private unsafe Blob CreateBlob(Stream stream, string hintpath, long? numberOfBytesToConsume)
        {
            Ensure.ArgumentNotNull(stream, "stream");

            // there's no need to buffer the file for filtering, so simply use a stream
            if (hintpath == null && numberOfBytesToConsume.HasValue)
            {
                return CreateBlob(stream, numberOfBytesToConsume.Value);
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException("The stream cannot be read from.", "stream");
            }

            IntPtr writestream_ptr = Proxy.git_blob_create_from_stream(repo.Handle, hintpath);
            GitWriteStream writestream = Marshal.PtrToStructure<GitWriteStream>(writestream_ptr);

            try
            {
                var buffer = new byte[4 * 1024];
                long totalRead = 0;
                int read = 0;

                while (true)
                {
                    int toRead = numberOfBytesToConsume.HasValue ?
                        (int)Math.Min(numberOfBytesToConsume.Value - totalRead, (long)buffer.Length) :
                        buffer.Length;

                    if (toRead > 0)
                    {
                        read = (toRead > 0) ? stream.Read(buffer, 0, toRead) : 0;
                    }

                    if (read == 0)
                    {
                        break;
                    }

                    fixed (byte* buffer_ptr = buffer)
                    {
                        writestream.write(writestream_ptr, (IntPtr)buffer_ptr, (UIntPtr)read);
                    }

                    totalRead += read;
                }

                if (numberOfBytesToConsume.HasValue && totalRead < numberOfBytesToConsume.Value)
                {
                    throw new EndOfStreamException("The stream ended unexpectedly");
                }
            }
            catch(Exception)
            {
                writestream.free(writestream_ptr);
                throw;
            }

            ObjectId id = Proxy.git_blob_create_fromstream_commit(writestream_ptr);
            return repo.Lookup<Blob>(id);
        }

        /// <summary>
        /// Inserts a <see cref="Blob"/> into the object database created from the content of the stream.
        /// </summary>
        /// <param name="stream">The stream from which will be read the content of the blob to be created.</param>
        /// <param name="numberOfBytesToConsume">Number of bytes to consume from the stream.</param>
        /// <returns>The created <see cref="Blob"/>.</returns>
        public virtual Blob CreateBlob(Stream stream, long numberOfBytesToConsume)
        {
            var id = Write<Blob>(stream, numberOfBytesToConsume);
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
        /// Inserts a <see cref="Tree"/> into the object database, created from the <see cref="Index"/>.
        /// <para>
        ///   It recursively creates tree objects for each of the subtrees stored in the index, but only returns the root tree.
        /// </para>
        /// <para>
        ///   The index must be fully merged.
        /// </para>
        /// </summary>
        /// <param name="index">The <see cref="Index"/>.</param>
        /// <returns>The created <see cref="Tree"/>. This can be used e.g. to create a <see cref="Commit"/>.</returns>
        public virtual Tree CreateTree(Index index)
        {
            Ensure.ArgumentNotNull(index, "index");

            var treeId = Proxy.git_index_write_tree(index.Handle);
            return this.repo.Lookup<Tree>(treeId);
        }

        /// <summary>
        /// Inserts a <see cref="Commit"/> into the object database, referencing an existing <see cref="Tree"/>.
        /// <para>
        /// Prettifing the message includes:
        /// * Removing empty lines from the beginning and end.
        /// * Removing trailing spaces from every line.
        /// * Turning multiple consecutive empty lines between paragraphs into just one empty line.
        /// * Ensuring the commit message ends with a newline.
        /// * Removing every line starting with "#".
        /// </para>
        /// </summary>
        /// <param name="author">The <see cref="Signature"/> of who made the change.</param>
        /// <param name="committer">The <see cref="Signature"/> of who added the change to the repository.</param>
        /// <param name="message">The description of why a change was made to the repository.</param>
        /// <param name="tree">The <see cref="Tree"/> of the <see cref="Commit"/> to be created.</param>
        /// <param name="parents">The parents of the <see cref="Commit"/> to be created.</param>
        /// <param name="prettifyMessage">True to prettify the message, or false to leave it as is.</param>
        /// <returns>The created <see cref="Commit"/>.</returns>
        public virtual Commit CreateCommit(Signature author, Signature committer, string message, Tree tree, IEnumerable<Commit> parents, bool prettifyMessage)
        {
            return CreateCommit(author, committer, message, tree, parents, prettifyMessage, null);
        }

        /// <summary>
        /// Inserts a <see cref="Commit"/> into the object database, referencing an existing <see cref="Tree"/>.
        /// <para>
        /// Prettifing the message includes:
        /// * Removing empty lines from the beginning and end.
        /// * Removing trailing spaces from every line.
        /// * Turning multiple consecutive empty lines between paragraphs into just one empty line.
        /// * Ensuring the commit message ends with a newline.
        /// * Removing every line starting with the <paramref name="commentChar"/>.
        /// </para>
        /// </summary>
        /// <param name="author">The <see cref="Signature"/> of who made the change.</param>
        /// <param name="committer">The <see cref="Signature"/> of who added the change to the repository.</param>
        /// <param name="message">The description of why a change was made to the repository.</param>
        /// <param name="tree">The <see cref="Tree"/> of the <see cref="Commit"/> to be created.</param>
        /// <param name="parents">The parents of the <see cref="Commit"/> to be created.</param>
        /// <param name="prettifyMessage">True to prettify the message, or false to leave it as is.</param>
        /// <param name="commentChar">When non null, lines starting with this character will be stripped if prettifyMessage is true.</param>
        /// <returns>The created <see cref="Commit"/>.</returns>
        public virtual Commit CreateCommit(Signature author, Signature committer, string message, Tree tree, IEnumerable<Commit> parents, bool prettifyMessage, char? commentChar)
        {
            Ensure.ArgumentNotNull(message, "message");
            Ensure.ArgumentDoesNotContainZeroByte(message, "message");
            Ensure.ArgumentNotNull(author, "author");
            Ensure.ArgumentNotNull(committer, "committer");
            Ensure.ArgumentNotNull(tree, "tree");
            Ensure.ArgumentNotNull(parents, "parents");

            if (prettifyMessage)
            {
                message = Proxy.git_message_prettify(message, commentChar);
            }
            GitOid[] parentIds = parents.Select(p => p.Id.Oid).ToArray();

            ObjectId commitId = Proxy.git_commit_create(repo.Handle, null, author, committer, message, tree, parentIds);

            Commit commit = repo.Lookup<Commit>(commitId);
            Ensure.GitObjectIsNotNull(commit, commitId.Sha);
            return commit;
        }

        /// <summary>
        /// Inserts a <see cref="Commit"/> into the object database after attaching the given signature.
        /// </summary>
        /// <param name="commitContent">The raw unsigned commit</param>
        /// <param name="signature">The signature data </param>
        /// <param name="field">The header field in the commit in which to store the signature</param>
        /// <returns>The created <see cref="Commit"/>.</returns>
        public virtual ObjectId CreateCommitWithSignature(string commitContent, string signature, string field)
        {
            return Proxy.git_commit_create_with_signature(repo.Handle, commitContent, signature, field);
        }

        /// <summary>
        /// Inserts a <see cref="Commit"/> into the object database after attaching the given signature.
        /// <para>
        /// This overload uses the default header field of "gpgsig"
        /// </para>
        /// </summary>
        /// <param name="commitContent">The raw unsigned commit</param>
        /// <param name="signature">The signature data </param>
        /// <returns>The created <see cref="Commit"/>.</returns>
        public virtual ObjectId CreateCommitWithSignature(string commitContent, string signature)
        {
            return Proxy.git_commit_create_with_signature(repo.Handle, commitContent, signature, null);
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
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(message, "message");
            Ensure.ArgumentNotNull(target, "target");
            Ensure.ArgumentNotNull(tagger, "tagger");
            Ensure.ArgumentDoesNotContainZeroByte(name, "name");
            Ensure.ArgumentDoesNotContainZeroByte(message, "message");

            string prettifiedMessage = Proxy.git_message_prettify(message, null);

            ObjectId tagId = Proxy.git_tag_annotation_create(repo.Handle, name, target, tagger, prettifiedMessage);

            return repo.Lookup<TagAnnotation>(tagId);
        }

        /// <summary>
        /// Create a TAR archive of the given tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <param name="archivePath">The archive path.</param>
        public virtual void Archive(Tree tree, string archivePath)
        {
            using (var output = new FileStream(archivePath, FileMode.Create))
            using (var archiver = new TarArchiver(output))
            {
                Archive(tree, archiver);
            }
        }

        /// <summary>
        /// Create a TAR archive of the given commit.
        /// </summary>
        /// <param name="commit">commit.</param>
        /// <param name="archivePath">The archive path.</param>
        public virtual void Archive(Commit commit, string archivePath)
        {
            using (var output = new FileStream(archivePath, FileMode.Create))
            using (var archiver = new TarArchiver(output))
            {
                Archive(commit, archiver);
            }
        }

        /// <summary>
        /// Archive the given commit.
        /// </summary>
        /// <param name="commit">The commit.</param>
        /// <param name="archiver">The archiver to use.</param>
        public virtual void Archive(Commit commit, ArchiverBase archiver)
        {
            Ensure.ArgumentNotNull(commit, "commit");
            Ensure.ArgumentNotNull(archiver, "archiver");

            archiver.OrchestrateArchiving(commit.Tree, commit.Id, commit.Committer.When);
        }

        /// <summary>
        /// Archive the given tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <param name="archiver">The archiver to use.</param>
        public virtual void Archive(Tree tree, ArchiverBase archiver)
        {
            Ensure.ArgumentNotNull(tree, "tree");
            Ensure.ArgumentNotNull(archiver, "archiver");

            archiver.OrchestrateArchiving(tree, null, DateTimeOffset.UtcNow);
        }

        /// <summary>
        /// Returns the merge base (best common ancestor) of the given commits
        /// and the distance between each of these commits and this base.
        /// </summary>
        /// <param name="one">The <see cref="Commit"/> being used as a reference.</param>
        /// <param name="another">The <see cref="Commit"/> being compared against <paramref name="one"/>.</param>
        /// <returns>A instance of <see cref="HistoryDivergence"/>.</returns>
        public virtual HistoryDivergence CalculateHistoryDivergence(Commit one, Commit another)
        {
            Ensure.ArgumentNotNull(one, "one");
            Ensure.ArgumentNotNull(another, "another");

            return new HistoryDivergence(repo, one, another);
        }

        /// <summary>
        /// Performs a cherry-pick of <paramref name="cherryPickCommit"/> onto <paramref name="cherryPickOnto"/> commit.
        /// </summary>
        /// <param name="cherryPickCommit">The commit to cherry-pick.</param>
        /// <param name="cherryPickOnto">The commit to cherry-pick onto.</param>
        /// <param name="mainline">Which commit to consider the parent for the diff when cherry-picking a merge commit.</param>
        /// <param name="options">The options for the merging in the cherry-pick operation.</param>
        /// <returns>A result containing a <see cref="Tree"/> if the cherry-pick was successful and a list of <see cref="Conflict"/>s if it is not.</returns>
        public virtual MergeTreeResult CherryPickCommit(Commit cherryPickCommit, Commit cherryPickOnto, int mainline, MergeTreeOptions options)
        {
            Ensure.ArgumentNotNull(cherryPickCommit, "cherryPickCommit");
            Ensure.ArgumentNotNull(cherryPickOnto, "cherryPickOnto");

            var modifiedOptions = new MergeTreeOptions();

            // We throw away the index after looking at the conflicts, so we'll never need the REUC
            // entries to be there
            modifiedOptions.SkipReuc = true;

            if (options != null)
            {
                modifiedOptions.FailOnConflict = options.FailOnConflict;
                modifiedOptions.FindRenames = options.FindRenames;
                modifiedOptions.MergeFileFavor = options.MergeFileFavor;
                modifiedOptions.RenameThreshold = options.RenameThreshold;
                modifiedOptions.TargetLimit = options.TargetLimit;
            }

            bool earlyStop;

            using (var indexHandle = CherryPickCommit(cherryPickCommit, cherryPickOnto, mainline, modifiedOptions, out earlyStop))
            {
                MergeTreeResult cherryPickResult;

                // Stopped due to FailOnConflict so there's no index or conflict list
                if (earlyStop)
                {
                    return new MergeTreeResult(new Conflict[] { });
                }

                if (Proxy.git_index_has_conflicts(indexHandle))
                {
                    List<Conflict> conflicts = new List<Conflict>();
                    Conflict conflict;
                    using (ConflictIteratorHandle iterator = Proxy.git_index_conflict_iterator_new(indexHandle))
                    {
                        while ((conflict = Proxy.git_index_conflict_next(iterator)) != null)
                        {
                            conflicts.Add(conflict);
                        }
                    }
                    cherryPickResult = new MergeTreeResult(conflicts);
                }
                else
                {
                    var treeId = Proxy.git_index_write_tree_to(indexHandle, repo.Handle);
                    cherryPickResult = new MergeTreeResult(this.repo.Lookup<Tree>(treeId));
                }

                return cherryPickResult;
            }
        }

        /// <summary>
        /// Calculates the current shortest abbreviated <see cref="ObjectId"/>
        /// string representation for a <see cref="GitObject"/>.
        /// </summary>
        /// <param name="gitObject">The <see cref="GitObject"/> which identifier should be shortened.</param>
        /// <returns>A short string representation of the <see cref="ObjectId"/>.</returns>
        public virtual string ShortenObjectId(GitObject gitObject)
        {
            var shortSha = Proxy.git_object_short_id(repo.Handle, gitObject.Id);
            return shortSha;
        }

        /// <summary>
        /// Calculates the current shortest abbreviated <see cref="ObjectId"/>
        /// string representation for a <see cref="GitObject"/>.
        /// </summary>
        /// <param name="gitObject">The <see cref="GitObject"/> which identifier should be shortened.</param>
        /// <param name="minLength">Minimum length of the shortened representation.</param>
        /// <returns>A short string representation of the <see cref="ObjectId"/>.</returns>
        public virtual string ShortenObjectId(GitObject gitObject, int minLength)
        {
            Ensure.ArgumentNotNull(gitObject, "gitObject");

            if (minLength <= 0 || minLength > ObjectId.HexSize)
            {
                throw new ArgumentOutOfRangeException("minLength",
                                                      minLength,
                                                      string.Format("Expected value should be greater than zero and less than or equal to {0}.",
                                                                    ObjectId.HexSize));
            }

            string shortSha = Proxy.git_object_short_id(repo.Handle, gitObject.Id);

            if (shortSha == null)
            {
                throw new LibGit2SharpException("Unable to abbreviate SHA-1 value for GitObject " + gitObject.Id);
            }

            if (minLength <= shortSha.Length)
            {
                return shortSha;
            }

            return gitObject.Sha.Substring(0, minLength);
        }

        /// <summary>
        /// Returns whether merging <paramref name="one"/> into <paramref name="another"/>
        /// would result in merge conflicts.
        /// </summary>
        /// <param name="one">The commit wrapping the base tree to merge into.</param>
        /// <param name="another">The commit wrapping the tree to merge into <paramref name="one"/>.</param>
        /// <returns>True if the merge does not result in a conflict, false otherwise.</returns>
        public virtual bool CanMergeWithoutConflict(Commit one, Commit another)
        {
            Ensure.ArgumentNotNull(one, "one");
            Ensure.ArgumentNotNull(another, "another");

            var opts = new MergeTreeOptions()
            {
                SkipReuc = true,
                FailOnConflict = true,
            };

            var result = repo.ObjectDatabase.MergeCommits(one, another, opts);
            return (result.Status == MergeTreeStatus.Succeeded);
        }

        /// <summary>
        /// Find the best possible merge base given two <see cref="Commit"/>s.
        /// </summary>
        /// <param name="first">The first <see cref="Commit"/>.</param>
        /// <param name="second">The second <see cref="Commit"/>.</param>
        /// <returns>The merge base or null if none found.</returns>
        public virtual Commit FindMergeBase(Commit first, Commit second)
        {
            Ensure.ArgumentNotNull(first, "first");
            Ensure.ArgumentNotNull(second, "second");

            return FindMergeBase(new[] { first, second }, MergeBaseFindingStrategy.Standard);
        }

        /// <summary>
        /// Find the best possible merge base given two or more <see cref="Commit"/> according to the <see cref="MergeBaseFindingStrategy"/>.
        /// </summary>
        /// <param name="commits">The <see cref="Commit"/>s for which to find the merge base.</param>
        /// <param name="strategy">The strategy to leverage in order to find the merge base.</param>
        /// <returns>The merge base or null if none found.</returns>
        public virtual Commit FindMergeBase(IEnumerable<Commit> commits, MergeBaseFindingStrategy strategy)
        {
            Ensure.ArgumentNotNull(commits, "commits");

            ObjectId id;
            List<GitOid> ids = new List<GitOid>(8);
            int count = 0;

            foreach (var commit in commits)
            {
                if (commit == null)
                {
                    throw new ArgumentException("Enumerable contains null at position: " + count.ToString(CultureInfo.InvariantCulture), "commits");
                }
                ids.Add(commit.Id.Oid);
                count++;
            }

            if (count < 2)
            {
                throw new ArgumentException("The enumerable must contains at least two commits.", "commits");
            }

            switch (strategy)
            {
                case MergeBaseFindingStrategy.Standard:
                    id = Proxy.git_merge_base_many(repo.Handle, ids.ToArray());
                    break;

                case MergeBaseFindingStrategy.Octopus:
                    id = Proxy.git_merge_base_octopus(repo.Handle, ids.ToArray());
                    break;

                default:
                    throw new ArgumentException("", "strategy");
            }

            return id == null ? null : repo.Lookup<Commit>(id);
        }

        /// <summary>
        /// Perform a three-way merge of two commits, looking up their
        /// commit ancestor. The returned <see cref="MergeTreeResult"/> will contain the results
        /// of the merge and can be examined for conflicts.
        /// </summary>
        /// <param name="ours">The first commit</param>
        /// <param name="theirs">The second commit</param>
        /// <param name="options">The <see cref="MergeTreeOptions"/> controlling the merge</param>
        /// <returns>The <see cref="MergeTreeResult"/> containing the merged trees and any conflicts</returns>
        public virtual MergeTreeResult MergeCommits(Commit ours, Commit theirs, MergeTreeOptions options)
        {
            Ensure.ArgumentNotNull(ours, "ours");
            Ensure.ArgumentNotNull(theirs, "theirs");

            var modifiedOptions = new MergeTreeOptions();

            // We throw away the index after looking at the conflicts, so we'll never need the REUC
            // entries to be there
            modifiedOptions.SkipReuc = true;

            if (options != null)
            {
                modifiedOptions.FailOnConflict = options.FailOnConflict;
                modifiedOptions.FindRenames = options.FindRenames;
                modifiedOptions.IgnoreWhitespaceChange = options.IgnoreWhitespaceChange;
                modifiedOptions.MergeFileFavor = options.MergeFileFavor;
                modifiedOptions.RenameThreshold = options.RenameThreshold;
                modifiedOptions.TargetLimit = options.TargetLimit;
            }

            bool earlyStop;
            using (var indexHandle = MergeCommits(ours, theirs, modifiedOptions, out earlyStop))
            {
                MergeTreeResult mergeResult;

                // Stopped due to FailOnConflict so there's no index or conflict list
                if (earlyStop)
                {
                    return new MergeTreeResult(new Conflict[] { });
                }

                if (Proxy.git_index_has_conflicts(indexHandle))
                {
                    List<Conflict> conflicts = new List<Conflict>();
                    Conflict conflict;

                    using (ConflictIteratorHandle iterator = Proxy.git_index_conflict_iterator_new(indexHandle))
                    {
                        while ((conflict = Proxy.git_index_conflict_next(iterator)) != null)
                        {
                            conflicts.Add(conflict);
                        }
                    }

                    mergeResult = new MergeTreeResult(conflicts);
                }
                else
                {
                    var treeId = Proxy.git_index_write_tree_to(indexHandle, repo.Handle);
                    mergeResult = new MergeTreeResult(this.repo.Lookup<Tree>(treeId));
                }

                return mergeResult;
            }
        }

        /// <summary>
        /// Packs all the objects in the <see cref="ObjectDatabase"/> and write a pack (.pack) and index (.idx) files for them.
        /// </summary>
        /// <param name="options">Packing options</param>
        /// This method will invoke the default action of packing all objects in an arbitrary order.
        /// <returns>Packing results</returns>
        public virtual PackBuilderResults Pack(PackBuilderOptions options)
        {
            return InternalPack(options, builder =>
            {
                foreach (GitObject obj in repo.ObjectDatabase)
                {
                    builder.Add(obj.Id);
                }
            });
        }

        /// <summary>
        /// Packs objects in the <see cref="ObjectDatabase"/> chosen by the packDelegate action
        /// and write a pack (.pack) and index (.idx) files for them
        /// </summary>
        /// <param name="options">Packing options</param>
        /// <param name="packDelegate">Packing action</param>
        /// <returns>Packing results</returns>
        public virtual PackBuilderResults Pack(PackBuilderOptions options, Action<PackBuilder> packDelegate)
        {
            return InternalPack(options, packDelegate);
        }

        /// <summary>
        /// Perform a three-way merge of two commits, looking up their
        /// commit ancestor. The returned index will contain the results
        /// of the merge and can be examined for conflicts.
        /// </summary>
        /// <param name="ours">The first tree</param>
        /// <param name="theirs">The second tree</param>
        /// <param name="options">The <see cref="MergeTreeOptions"/> controlling the merge</param>
        /// <returns>The <see cref="TransientIndex"/> containing the merged trees and any conflicts, or null if the merge stopped early due to conflicts.
        /// The index must be disposed by the caller.</returns>
        public virtual TransientIndex MergeCommitsIntoIndex(Commit ours, Commit theirs, MergeTreeOptions options)
        {
            Ensure.ArgumentNotNull(ours, "ours");
            Ensure.ArgumentNotNull(theirs, "theirs");

            options = options ?? new MergeTreeOptions();

            bool earlyStop;
            var indexHandle = MergeCommits(ours, theirs, options, out earlyStop);
            if (earlyStop)
            {
                if (indexHandle != null)
                {
                    indexHandle.Dispose();
                }
                return null;
            }
            var result = new TransientIndex(indexHandle, repo);
            return result;
        }

        /// <summary>
        /// Performs a cherry-pick of <paramref name="cherryPickCommit"/> onto <paramref name="cherryPickOnto"/> commit.
        /// </summary>
        /// <param name="cherryPickCommit">The commit to cherry-pick.</param>
        /// <param name="cherryPickOnto">The commit to cherry-pick onto.</param>
        /// <param name="mainline">Which commit to consider the parent for the diff when cherry-picking a merge commit.</param>
        /// <param name="options">The options for the merging in the cherry-pick operation.</param>
        /// <returns>The <see cref="TransientIndex"/> containing the cherry-pick result tree and any conflicts, or null if the merge stopped early due to conflicts.
        /// The index must be disposed by the caller. </returns>
        public virtual TransientIndex CherryPickCommitIntoIndex(Commit cherryPickCommit, Commit cherryPickOnto, int mainline, MergeTreeOptions options)
        {
            Ensure.ArgumentNotNull(cherryPickCommit, "cherryPickCommit");
            Ensure.ArgumentNotNull(cherryPickOnto, "cherryPickOnto");

            options = options ?? new MergeTreeOptions();

            bool earlyStop;
            var indexHandle = CherryPickCommit(cherryPickCommit, cherryPickOnto, mainline, options, out earlyStop);
            if (earlyStop)
            {
                if (indexHandle != null)
                {
                    indexHandle.Dispose();
                }
                return null;
            }
            var result = new TransientIndex(indexHandle, repo);
            return result;
        }

        /// <summary>
        /// Perform a three-way merge of two commits, looking up their
        /// commit ancestor. The returned index will contain the results
        /// of the merge and can be examined for conflicts.
        /// </summary>
        /// <param name="ours">The first tree</param>
        /// <param name="theirs">The second tree</param>
        /// <param name="options">The <see cref="MergeTreeOptions"/> controlling the merge</param>
        /// <param name="earlyStop">True if the merge stopped early due to conflicts</param>
        /// <returns>The <see cref="IndexHandle"/> containing the merged trees and any conflicts</returns>
        private IndexHandle MergeCommits(Commit ours, Commit theirs, MergeTreeOptions options, out bool earlyStop)
        {
            GitMergeFlag mergeFlags = GitMergeFlag.GIT_MERGE_NORMAL;
            if (options.SkipReuc)
            {
                mergeFlags |= GitMergeFlag.GIT_MERGE_SKIP_REUC;
            }
            if (options.FindRenames)
            {
                mergeFlags |= GitMergeFlag.GIT_MERGE_FIND_RENAMES;
            }
            if (options.FailOnConflict)
            {
                mergeFlags |= GitMergeFlag.GIT_MERGE_FAIL_ON_CONFLICT;
            }

            var mergeOptions = new GitMergeOpts
            {
                Version = 1,
                MergeFileFavorFlags = options.MergeFileFavor,
                MergeTreeFlags = mergeFlags,
                RenameThreshold = (uint)options.RenameThreshold,
                TargetLimit = (uint)options.TargetLimit,
            };
            using (var oneHandle = Proxy.git_object_lookup(repo.Handle, ours.Id, GitObjectType.Commit))
            using (var twoHandle = Proxy.git_object_lookup(repo.Handle, theirs.Id, GitObjectType.Commit))
            {
                var indexHandle = Proxy.git_merge_commits(repo.Handle, oneHandle, twoHandle, mergeOptions, out earlyStop);
                return indexHandle;
            }
        }

        /// <summary>
        /// Performs a cherry-pick of <paramref name="cherryPickCommit"/> onto <paramref name="cherryPickOnto"/> commit.
        /// </summary>
        /// <param name="cherryPickCommit">The commit to cherry-pick.</param>
        /// <param name="cherryPickOnto">The commit to cherry-pick onto.</param>
        /// <param name="mainline">Which commit to consider the parent for the diff when cherry-picking a merge commit.</param>
        /// <param name="options">The options for the merging in the cherry-pick operation.</param>
        /// <param name="earlyStop">True if the cherry-pick stopped early due to conflicts</param>
        /// <returns>The <see cref="IndexHandle"/> containing the cherry-pick result tree and any conflicts</returns>
        private IndexHandle CherryPickCommit(Commit cherryPickCommit, Commit cherryPickOnto, int mainline, MergeTreeOptions options, out bool earlyStop)
        {
            GitMergeFlag mergeFlags = GitMergeFlag.GIT_MERGE_NORMAL;
            if (options.SkipReuc)
            {
                mergeFlags |= GitMergeFlag.GIT_MERGE_SKIP_REUC;
            }
            if (options.FindRenames)
            {
                mergeFlags |= GitMergeFlag.GIT_MERGE_FIND_RENAMES;
            }
            if (options.FailOnConflict)
            {
                mergeFlags |= GitMergeFlag.GIT_MERGE_FAIL_ON_CONFLICT;
            }

            var mergeOptions = new GitMergeOpts
            {
                Version = 1,
                MergeFileFavorFlags = options.MergeFileFavor,
                MergeTreeFlags = mergeFlags,
                RenameThreshold = (uint)options.RenameThreshold,
                TargetLimit = (uint)options.TargetLimit,
            };

            using (var cherryPickOntoHandle = Proxy.git_object_lookup(repo.Handle, cherryPickOnto.Id, GitObjectType.Commit))
            using (var cherryPickCommitHandle = Proxy.git_object_lookup(repo.Handle, cherryPickCommit.Id, GitObjectType.Commit))
            {
                var indexHandle = Proxy.git_cherrypick_commit(repo.Handle, cherryPickCommitHandle, cherryPickOntoHandle, (uint)mainline, mergeOptions, out earlyStop);
                return indexHandle;
            }
        }


        /// <summary>
        /// Packs objects in the <see cref="ObjectDatabase"/> and write a pack (.pack) and index (.idx) files for them.
        /// For internal use only.
        /// </summary>
        /// <param name="options">Packing options</param>
        /// <param name="packDelegate">Packing action</param>
        /// <returns>Packing results</returns>
        private PackBuilderResults InternalPack(PackBuilderOptions options, Action<PackBuilder> packDelegate)
        {
            Ensure.ArgumentNotNull(options, "options");
            Ensure.ArgumentNotNull(packDelegate, "packDelegate");

            PackBuilderResults results = new PackBuilderResults();

            using (PackBuilder builder = new PackBuilder(repo))
            {
                // set pre-build options
                builder.SetMaximumNumberOfThreads(options.MaximumNumberOfThreads);

                // call the provided action
                packDelegate(builder);

                // writing the pack and index files
                builder.Write(options.PackDirectoryPath);

                // adding the results to the PackBuilderResults object
                results.WrittenObjectsCount = builder.WrittenObjectsCount;
            }

            return results;
        }

        /// <summary>
        /// Performs a revert of <paramref name="revertCommit"/> onto <paramref name="revertOnto"/> commit.
        /// </summary>
        /// <param name="revertCommit">The commit to revert.</param>
        /// <param name="revertOnto">The commit to revert onto.</param>
        /// <param name="mainline">Which commit to consider the parent for the diff when reverting a merge commit.</param>
        /// <param name="options">The options for the merging in the revert operation.</param>
        /// <returns>A result containing a <see cref="Tree"/> if the revert was successful and a list of <see cref="Conflict"/>s if it is not.</returns>
        public virtual MergeTreeResult RevertCommit(Commit revertCommit, Commit revertOnto, int mainline, MergeTreeOptions options)
        {
            Ensure.ArgumentNotNull(revertCommit, "revertCommit");
            Ensure.ArgumentNotNull(revertOnto, "revertOnto");

            options = options ?? new MergeTreeOptions();

            // We throw away the index after looking at the conflicts, so we'll never need the REUC
            // entries to be there
            GitMergeFlag mergeFlags = GitMergeFlag.GIT_MERGE_NORMAL | GitMergeFlag.GIT_MERGE_SKIP_REUC;
            if (options.FindRenames)
            {
                mergeFlags |= GitMergeFlag.GIT_MERGE_FIND_RENAMES;
            }
            if (options.FailOnConflict)
            {
                mergeFlags |= GitMergeFlag.GIT_MERGE_FAIL_ON_CONFLICT;
            }


            var opts = new GitMergeOpts
            {
                Version = 1,
                MergeFileFavorFlags = options.MergeFileFavor,
                MergeTreeFlags = mergeFlags,
                RenameThreshold = (uint)options.RenameThreshold,
                TargetLimit = (uint)options.TargetLimit
            };

            bool earlyStop;

            using (var revertOntoHandle = Proxy.git_object_lookup(repo.Handle, revertOnto.Id, GitObjectType.Commit))
            using (var revertCommitHandle = Proxy.git_object_lookup(repo.Handle, revertCommit.Id, GitObjectType.Commit))
            using (var indexHandle = Proxy.git_revert_commit(repo.Handle, revertCommitHandle, revertOntoHandle, (uint)mainline, opts, out earlyStop))
            {
                MergeTreeResult revertTreeResult;

                // Stopped due to FailOnConflict so there's no index or conflict list
                if (earlyStop)
                {
                    return new MergeTreeResult(new Conflict[] { });
                }

                if (Proxy.git_index_has_conflicts(indexHandle))
                {
                    List<Conflict> conflicts = new List<Conflict>();
                    Conflict conflict;
                    using (ConflictIteratorHandle iterator = Proxy.git_index_conflict_iterator_new(indexHandle))
                    {
                        while ((conflict = Proxy.git_index_conflict_next(iterator)) != null)
                        {
                            conflicts.Add(conflict);
                        }
                    }
                    revertTreeResult = new MergeTreeResult(conflicts);
                }
                else
                {
                    var treeId = Proxy.git_index_write_tree_to(indexHandle, repo.Handle);
                    revertTreeResult = new MergeTreeResult(this.repo.Lookup<Tree>(treeId));
                }

                return revertTreeResult;
            }
        }
    }
}
