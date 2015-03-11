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
                ptr => ptr.MarshalAs<GitOid>());

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

                if (bytesToRead == 0)
                {
                    return 0;
                }

                int numberOfReadBytes = stream.Read(local, 0, bytesToRead);

                if (numberOfBytesToConsume.HasValue
                    && numberOfReadBytes == 0)
                {
                    return (int)GitErrorCode.User;
                }

                totalNumberOfReadBytes += numberOfReadBytes;

                Marshal.Copy(local, 0, content, numberOfReadBytes);

                return numberOfReadBytes;
            }
        }

        /// <summary>
        /// Inserts a <see cref="Blob"/> into the object database, created from the content of a stream.
        /// <para>Optionally, git filters will be applied to the content before storing it.</para>
        /// </summary>
        /// <param name="stream">The stream from which will be read the content of the blob to be created.</param>
        /// <param name="hintpath">The hintpath is used to determine what git filters should be applied to the object before it can be placed to the object database.</param>
        /// <param name="numberOfBytesToConsume">The number of bytes to consume from the stream.</param>
        /// <returns>The created <see cref="Blob"/>.</returns>
        public virtual Blob CreateBlob(Stream stream, string hintpath = null, int? numberOfBytesToConsume = null)
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

            var proc = new Processor(stream, numberOfBytesToConsume);
            ObjectId id = Proxy.git_blob_create_fromchunks(repo.Handle, hintpath, proc.Provider);

            return repo.Lookup<Blob>(id);
        }

        /// <summary>
        /// Inserts a <see cref="Blob"/> into the object database created from the content of the stream.
        /// </summary>
        /// <param name="stream">The stream from which will be read the content of the blob to be created.</param>
        /// <param name="numberOfBytesToConsume">Number of bytes to consume from the stream.</param>
        /// <returns>The created <see cref="Blob"/>.</returns>
        public virtual Blob CreateBlob(Stream stream, int numberOfBytesToConsume)
        {
            Ensure.ArgumentNotNull(stream, "stream");

            if (!stream.CanRead)
            {
                throw new ArgumentException("The stream cannot be read from.", "stream");
            }

            using (var odbStream = Proxy.git_odb_open_wstream(handle, (UIntPtr)numberOfBytesToConsume, GitObjectType.Blob))
            {
                var buffer = new byte[4*1024];
                int totalRead = 0;

                while (totalRead < numberOfBytesToConsume)
                {
                    var left = numberOfBytesToConsume - totalRead;
                    var toRead = left < buffer.Length ? left : buffer.Length;
                    var read = stream.Read(buffer, 0, toRead);

                    if (read == 0)
                    {
                        throw new EndOfStreamException("The stream ended unexpectedly");
                    }

                    Proxy.git_odb_stream_write(odbStream, buffer, read);
                    totalRead += read;
                }

                var id = Proxy.git_odb_stream_finalize_write(odbStream);

                return repo.Lookup<Blob>(id);
            }
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
        /// <param name="commentChar">Character that lines start with to be stripped if prettifyMessage is true.</param>
        /// <returns>The created <see cref="Commit"/>.</returns>
        public virtual Commit CreateCommit(Signature author, Signature committer, string message, Tree tree, IEnumerable<Commit> parents, bool prettifyMessage, char? commentChar = null)
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
        /// Calculates the current shortest abbreviated <see cref="ObjectId"/>
        /// string representation for a <see cref="GitObject"/>.
        /// </summary>
        /// <param name="gitObject">The <see cref="GitObject"/> which identifier should be shortened.</param>
        /// <param name="minLength">Minimum length of the shortened representation.</param>
        /// <returns>A short string representation of the <see cref="ObjectId"/>.</returns>
        public virtual string ShortenObjectId(GitObject gitObject, int? minLength = null)
        {
            if (minLength.HasValue && (minLength <= 0 || minLength > ObjectId.HexSize))
            {
                throw new ArgumentOutOfRangeException("minLength", minLength,
                    string.Format("Expected value should be greater than zero and less than or equal to {0}.", ObjectId.HexSize));
            }

            string shortSha = Proxy.git_object_short_id(repo.Handle, gitObject.Id);

            if (minLength == null || (minLength <= shortSha.Length))
            {
                return shortSha;
            }

            return gitObject.Sha.Substring(0, minLength.Value);
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

            var result = repo.ObjectDatabase.MergeCommits(one, another, null);
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
        /// commit ancestor. The returned index will contain the results
        /// of the merge and can be examined for conflicts. The returned
        /// index must be disposed.
        /// </summary>
        /// <param name="ours">The first tree</param>
        /// <param name="theirs">The second tree</param>
        /// <param name="options">The <see cref="MergeTreeOptions"/> controlling the merge</param>
        /// <returns>The <see cref="Index"/> containing the merged trees and any conflicts</returns>
        public virtual MergeTreeResult MergeCommits(Commit ours, Commit theirs, MergeTreeOptions options)
        {
            Ensure.ArgumentNotNull(ours, "ours");
            Ensure.ArgumentNotNull(theirs, "theirs");

            options = options ?? new MergeTreeOptions();

            var mergeOptions = new GitMergeOpts
            {
                Version = 1,
                MergeFileFavorFlags = options.MergeFileFavor,
                MergeTreeFlags = options.FindRenames ? GitMergeTreeFlags.GIT_MERGE_TREE_FIND_RENAMES :
                                                       GitMergeTreeFlags.GIT_MERGE_TREE_NORMAL,
                RenameThreshold = (uint)options.RenameThreshold,
                TargetLimit = (uint)options.TargetLimit,
            };

            using (var oneHandle = Proxy.git_object_lookup(repo.Handle, ours.Id, GitObjectType.Commit))
            using (var twoHandle = Proxy.git_object_lookup(repo.Handle, theirs.Id, GitObjectType.Commit))
            using (var indexHandle = Proxy.git_merge_commits(repo.Handle, oneHandle, twoHandle, mergeOptions))
            {
                MergeTreeResult mergeResult;

                if (Proxy.git_index_has_conflicts(indexHandle))
                {
                    List<Conflict> conflicts = new List<Conflict>();
                    Conflict conflict;

                    using (ConflictIteratorSafeHandle iterator = Proxy.git_index_conflict_iterator_new(indexHandle))
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
    }
}
