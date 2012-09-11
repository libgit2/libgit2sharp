using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Repository is the primary interface into a git repository
    /// </summary>
    public class Repository : IRepository
    {
        private readonly BranchCollection branches;
        private readonly CommitLog commits;
        private readonly Lazy<Configuration> config;
        private readonly RepositorySafeHandle handle;
        private readonly Index index;
        private readonly ReferenceCollection refs;
        private readonly Lazy<RemoteCollection> remotes;
        private readonly TagCollection tags;
        private readonly Lazy<RepositoryInformation> info;
        private readonly Diff diff;
        private readonly NoteCollection notes;
        private readonly Lazy<ObjectDatabase> odb;
        private readonly Stack<IDisposable> toCleanup = new Stack<IDisposable>();
        private static readonly Lazy<string> versionRetriever = new Lazy<string>(RetrieveVersion);

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Repository" /> class, providing ooptional behavioral overrides through <paramref name="options"/> parameter.
        ///   <para>For a standard repository, <paramref name = "path" /> should either point to the ".git" folder or to the working directory. For a bare repository, <paramref name = "path" /> should directly point to the repository folder.</para>
        /// </summary>
        /// <param name = "path">
        ///   The path to the git repository to open, can be either the path to the git directory (for non-bare repositories this
        ///   would be the ".git" folder inside the working directory) or the path to the working directory.
        /// </param>
        /// <param name="options">
        ///   Overrides to the way a repository is opened.
        /// </param>
        public Repository(string path, RepositoryOptions options = null)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            int result = NativeMethods.git_repository_open(out handle, path);

            if (result == (int)GitErrorCode.NotFound)
            {
                throw new RepositoryNotFoundException(string.Format(CultureInfo.InvariantCulture, "Path '{0}' doesn't point at a valid Git repository or workdir.", path));
            }

            Ensure.Success(result);

            RegisterForCleanup(handle);

            bool isBare = NativeMethods.RepositoryStateChecker(handle, NativeMethods.git_repository_is_bare);

            Func<Index> indexBuilder = () => new Index(this);

            string configurationGlobalFilePath = null;
            string configurationSystemFilePath = null;

            if (options != null)
            {
                bool isWorkDirNull = string.IsNullOrEmpty(options.WorkingDirectoryPath);
                bool isIndexNull = string.IsNullOrEmpty(options.IndexPath);

                if (isBare && (isWorkDirNull ^ isIndexNull))
                {
                    throw new ArgumentException("When overriding the opening of a bare repository, both RepositoryOptions.WorkingDirectoryPath an RepositoryOptions.IndexPath have to be provided.");
                }

                isBare = false;

                if (!isIndexNull)
                {
                    indexBuilder = () => new Index(this, options.IndexPath);
                }

                if (!isWorkDirNull)
                {
                    Ensure.Success(NativeMethods.git_repository_set_workdir(handle, options.WorkingDirectoryPath, false));
                }

                configurationGlobalFilePath = options.GlobalConfigurationLocation;
                configurationSystemFilePath = options.SystemConfigurationLocation;
            }

            if (!isBare)
            {
                index = indexBuilder();
            }

            commits = new CommitLog(this);
            refs = new ReferenceCollection(this);
            branches = new BranchCollection(this);
            tags = new TagCollection(this);
            info = new Lazy<RepositoryInformation>(() => new RepositoryInformation(this, isBare));
            config = new Lazy<Configuration>(() => RegisterForCleanup(new Configuration(this, configurationGlobalFilePath, configurationSystemFilePath)));
            remotes = new Lazy<RemoteCollection>(() => new RemoteCollection(this));
            odb = new Lazy<ObjectDatabase>(() => new ObjectDatabase(this));
            diff = new Diff(this);
            notes = new NoteCollection(this);
        }

        /// <summary>
        ///   Takes care of releasing all non-managed remaining resources.
        /// </summary>
        ~Repository()
        {
            Dispose(false);
        }

        internal RepositorySafeHandle Handle
        {
            get { return handle; }
        }

        /// <summary>
        ///   Shortcut to return the branch pointed to by HEAD
        /// </summary>
        /// <returns></returns>
        public Branch Head
        {
            get
            {
                Reference reference = Refs["HEAD"];

                if (reference is SymbolicReference)
                {
                    return new Branch(this, reference);
                }

                return new DetachedHead(this, reference);
            }
        }

        /// <summary>
        ///   Provides access to the configuration settings for this repository.
        /// </summary>
        public Configuration Config
        {
            get { return config.Value; }
        }

        /// <summary>
        ///   Gets the index.
        /// </summary>
        public Index Index
        {
            get
            {
                if (index == null)
                {
                    throw new LibGit2SharpException("Index is not available in a bare repository.");
                }

                return index;
            }
        }

        /// <summary>
        ///   Gets the database.
        /// </summary>
        public ObjectDatabase ObjectDatabase
        {
            get
            {
                return odb.Value;
            }
        }

        /// <summary>
        ///   Lookup and enumerate references in the repository.
        /// </summary>
        public ReferenceCollection Refs
        {
            get { return refs; }
        }

        /// <summary>
        ///   Lookup and manage remotes in the repository.
        /// </summary>
        public RemoteCollection Remotes
        {
            get { return remotes.Value; }
        }

        /// <summary>
        ///   Lookup and enumerate commits in the repository.
        ///   Iterating this collection directly starts walking from the HEAD.
        /// </summary>
        public IQueryableCommitLog Commits
        {
            get { return commits; }
        }

        /// <summary>
        ///   Lookup and enumerate branches in the repository.
        /// </summary>
        public BranchCollection Branches
        {
            get { return branches; }
        }

        /// <summary>
        ///   Lookup and enumerate tags in the repository.
        /// </summary>
        public TagCollection Tags
        {
            get { return tags; }
        }

        /// <summary>
        ///   Provides high level information about this repository.
        /// </summary>
        public RepositoryInformation Info
        {
            get { return info.Value; }
        }

        /// <summary>
        ///   Provides access to diffing functionalities to show changes between the working tree and the index or a tree, changes between the index and a tree, changes between two trees, or changes between two files on disk.
        /// </summary>
        public Diff Diff
        {
            get { return diff; }
        }

        /// <summary>
        ///   Lookup notes in the repository.
        /// </summary>
        public NoteCollection Notes
        {
            get { return notes; }
        }

        #region IDisposable Members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            while (toCleanup.Count > 0)
            {
                toCleanup.Pop().SafeDispose();
            }
        }

        #endregion

        /// <summary>
        ///   Initialize a repository at the specified <paramref name = "path" />.
        /// </summary>
        /// <param name = "path">The path to the working folder when initializing a standard ".git" repository. Otherwise, when initializing a bare repository, the path to the expected location of this later.</param>
        /// <param name = "isBare">true to initialize a bare repository. False otherwise, to initialize a standard ".git" repository.</param>
        /// <returns> a new instance of the <see cref = "Repository" /> class. The client code is responsible for calling <see cref = "Dispose()" /> on this instance.</returns>
        public static Repository Init(string path, bool isBare = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            RepositorySafeHandle repo;
            int res = NativeMethods.git_repository_init(out repo, path, isBare);
            Ensure.Success(res);

            FilePath repoPath = NativeMethods.git_repository_path(repo);
            repo.SafeDispose();

            return new Repository(repoPath.Native);
        }

        /// <summary>
        ///   Try to lookup an object by its <see cref = "ObjectId" /> and <see cref = "GitObjectType" />. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name = "id">The id to lookup.</param>
        /// <param name = "type">The kind of GitObject being looked up</param>
        /// <returns>The <see cref = "GitObject" /> or null if it was not found.</returns>
        public GitObject Lookup(ObjectId id, GitObjectType type = GitObjectType.Any)
        {
            return LookupInternal(id, type, null);
        }

        internal GitObject LookupTreeEntryTarget(ObjectId id, FilePath path)
        {
            return LookupInternal(id, GitObjectType.Any, path);
        }

        internal GitObject LookupInternal(ObjectId id, GitObjectType type, FilePath knownPath)
        {
            Ensure.ArgumentNotNull(id, "id");

            GitOid oid = id.Oid;
            GitObjectSafeHandle obj = null;

            try
            {
                int res;
                if (id is AbbreviatedObjectId)
                {
                    res = NativeMethods.git_object_lookup_prefix(out obj, handle, ref oid, (uint)((AbbreviatedObjectId)id).Length, type);
                }
                else
                {
                    res = NativeMethods.git_object_lookup(out obj, handle, ref oid, type);
                }

                switch (res)
                {
                    case (int)GitErrorCode.NotFound:
                        return null;

                    case (int)GitErrorCode.Ambiguous:
                        throw new AmbiguousException(string.Format(CultureInfo.InvariantCulture, "Provided abbreviated ObjectId '{0}' is too short.", id));

                    default:
                        Ensure.Success(res);

                        if (id is AbbreviatedObjectId)
                        {
                            id = GitObject.ObjectIdOf(obj);
                        }

                        return GitObject.CreateFromPtr(obj, id, this, knownPath);
                }

            }
            finally
            {
                obj.SafeDispose();
            }
        }

        /// <summary>
        ///   Try to lookup an object by its sha or a reference canonical name and <see cref = "GitObjectType" />. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name = "objectish">A revparse spec for the object to lookup.</param>
        /// <param name = "type">The kind of <see cref = "GitObject" /> being looked up</param>
        /// <returns>The <see cref = "GitObject" /> or null if it was not found.</returns>
        public GitObject Lookup(string objectish, GitObjectType type = GitObjectType.Any)
        {
            return Lookup(objectish, type, LookUpOptions.None);
        }

        private string PathFromRevparseSpec(string spec)
        {
            if (spec.StartsWith(":/"))
            {
                return null;
            }

            if (Regex.IsMatch(spec, @"^:.*:"))
            {
                return null;
            }

            var m = Regex.Match(spec, @"[^@^ ]*:(.*)");
            return (m.Groups.Count > 1) ? m.Groups[1].Value : null;
        }

        internal GitObject Lookup(string objectish, GitObjectType type, LookUpOptions lookUpOptions)
        {
            Ensure.ArgumentNotNullOrEmptyString(objectish, "commitOrBranchSpec");

            GitObjectSafeHandle sh;
            int result = NativeMethods.git_revparse_single(out sh, Handle, objectish);

            if ((GitErrorCode)result != GitErrorCode.Ok || sh.IsInvalid)
            {
                if (lookUpOptions.Has(LookUpOptions.ThrowWhenNoGitObjectHasBeenFound) &&
                    result == (int)GitErrorCode.NotFound)
                {
                    Ensure.GitObjectIsNotNull(null, objectish);
                }

                if (result == (int)GitErrorCode.Ambiguous)
                {
                    throw new AmbiguousException(string.Format(CultureInfo.InvariantCulture, "Provided abbreviated ObjectId '{0}' is too short.", objectish));
                }

                return null;
            }

            if (type != GitObjectType.Any && NativeMethods.git_object_type(sh) != type)
            {
                sh.SafeDispose();
                return null;
            }

            var obj = GitObject.CreateFromPtr(sh, GitObject.ObjectIdOf(sh), this, PathFromRevparseSpec(objectish));
            sh.SafeDispose();

            if (lookUpOptions.Has(LookUpOptions.DereferenceResultToCommit))
            {
                return obj.DereferenceToCommit(objectish,
                                               lookUpOptions.Has(LookUpOptions.ThrowWhenCanNotBeDereferencedToACommit));
            }
            return obj;
        }

        /// <summary>
        ///   Lookup a commit by its SHA or name, or throw if a commit is not found.
        /// </summary>
        /// <param name="commitish">A revparse spec for the commit.</param>
        /// <returns>The commit.</returns>
        internal Commit LookupCommit(string commitish)
        {
            return (Commit)Lookup(commitish, GitObjectType.Any, LookUpOptions.ThrowWhenNoGitObjectHasBeenFound | LookUpOptions.DereferenceResultToCommit | LookUpOptions.ThrowWhenCanNotBeDereferencedToACommit);
        }

        /// <summary>
        ///   Probe for a git repository.
        ///   <para>The lookup start from <paramref name = "startingPath" /> and walk upward parent directories if nothing has been found.</para>
        /// </summary>
        /// <param name = "startingPath">The base path where the lookup starts.</param>
        /// <returns>The path to the git repository.</returns>
        public static string Discover(string startingPath)
        {
            var buffer = new byte[NativeMethods.GIT_PATH_MAX];

            int result = NativeMethods.git_repository_discover(buffer, buffer.Length, startingPath, false, null);

            if ((GitErrorCode)result == GitErrorCode.NotFound)
            {
                return null;
            }

            Ensure.Success(result);

            FilePath discoveredPath = Utf8Marshaler.Utf8FromBuffer(buffer);

            return discoveredPath.Native;
        }

        public static Repository Clone(
            string url,
            string destination,
            IndexerStats fetchStats = null,
            IndexerStats checkoutStats = null,
            GitCheckoutOptions checkout_options = null,
            bool isBare = false)
        {
            GitIndexerStats fetch_stats = (fetchStats != null) ? fetchStats.indexerStats : null;
            GitIndexerStats checkout_stats = (checkoutStats != null) ? checkoutStats.indexerStats : null;

            RepositorySafeHandle repo;
            int res;
            if (isBare)
                res = NativeMethods.git_clone_bare(out repo, url, destination, fetch_stats);
            else
                res = NativeMethods.git_clone(out repo, url, destination, fetch_stats, checkout_stats, checkout_options);
            Ensure.Success(res);

            repo.SafeDispose();
            return new Repository(destination);
        }

        /// <summary>
        ///   Checkout the specified branch, reference or SHA.
        /// </summary>
        /// <param name = "commitOrBranchSpec">A revparse spec for the commit or branch to checkout.</param>
        /// <returns>The new HEAD.</returns>
        public Branch Checkout(string commitOrBranchSpec)
        {
            // TODO: This does not yet checkout (write) the working directory

            var branch = Branches[commitOrBranchSpec];

            if (branch != null)
            {
                return Checkout(branch);
            }

            var commitId = LookupCommit(commitOrBranchSpec).Id;
            Refs.UpdateTarget("HEAD", commitId.Sha);
            return Head;
        }

        /// <summary>
        ///   Checkout the specified branch.
        /// </summary>
        /// <param name="branch">The branch to checkout.</param>
        /// <returns>The branch.</returns>
        public Branch Checkout(Branch branch)
        {
            Ensure.ArgumentNotNull(branch, "branch");

            Refs.UpdateTarget("HEAD", branch.CanonicalName);
            return branch;
        }

        /// <summary>
        ///   Fetch from the given <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote"><see cref="Remote"/> to fetch from.</param>
        /// <param name="progress">Class to report fetch progress.</param>
        public void Fetch(Remote remote, FetchProgress progress)
        {
            RemoteSafeHandle remoteHandle = this.Remotes.LoadRemote(remote.Name, true);
            using (remoteHandle)
            {
                FetchInternal(remoteHandle, progress);
            }
        }

        /// <summary>
        ///   Sets the current <see cref = "Head" /> to the specified commit and optionally resets the <see cref = "Index" /> and
        ///   the content of the working tree to match.
        /// </summary>
        /// <param name = "resetOptions">Flavor of reset operation to perform.</param>
        /// <param name = "commitish">A revparse spec for the target commit object.</param>
        public void Reset(ResetOptions resetOptions, string commitish = "HEAD")
        {
            Ensure.ArgumentNotNullOrEmptyString(commitish, "commitOrBranchSpec");

            if (resetOptions.Has(ResetOptions.Mixed) && Info.IsBare)
            {
                throw new LibGit2SharpException("Mixed reset is not allowed in a bare repository");
            }

            Commit commit = LookupCommit(commitish);

            //TODO: Check for unmerged entries

            string refToUpdate = Info.IsHeadDetached ? "HEAD" : Head.CanonicalName;
            Refs.UpdateTarget(refToUpdate, commit.Sha);

            if (resetOptions == ResetOptions.Soft)
            {
                return;
            }

            Index.ReplaceContentWithTree(commit.Tree);

            if (resetOptions == ResetOptions.Mixed)
            {
                return;
            }

            throw new NotImplementedException();
        }

        /// <summary>
        ///   Replaces entries in the <see cref="Index"/> with entries from the specified commit.
        /// </summary>
        /// <param name = "commitish">A revparse spec for the target commit object.</param>
        /// <param name = "paths">The list of paths (either files or directories) that should be considered.</param>
        public void Reset(string commitish = "HEAD", IEnumerable<string> paths = null)
        {
            if (Info.IsBare)
            {
                throw new LibGit2SharpException("Reset is not allowed in a bare repository");
            }

            Commit commit = LookupCommit(commitish);
            TreeChanges changes = Diff.Compare(commit.Tree, DiffTarget.Index, paths);

            Index.Reset(changes);
        }

        /// <summary>
        ///   Stores the content of the <see cref = "Repository.Index" /> as a new <see cref = "Commit" /> into the repository.
        ///   The tip of the <see cref = "Repository.Head"/> will be used as the parent of this new Commit.
        ///   Once the commit is created, the <see cref = "Repository.Head"/> will move forward to point at it.
        /// </summary>
        /// <param name = "message">The description of why a change was made to the repository.</param>
        /// <param name = "author">The <see cref = "Signature" /> of who made the change.</param>
        /// <param name = "committer">The <see cref = "Signature" /> of who added the change to the repository.</param>
        /// <param name = "amendPreviousCommit">True to amend the current <see cref = "Commit"/> pointed at by <see cref = "Repository.Head"/>, false otherwise.</param>
        /// <returns>The generated <see cref = "Commit" />.</returns>
        public Commit Commit(string message, Signature author, Signature committer, bool amendPreviousCommit = false)
        {
            if (amendPreviousCommit && Info.IsEmpty)
            {
                throw new LibGit2SharpException("Can not amend anything. The Head doesn't point at any commit.");
            }

            GitOid treeOid;
            Ensure.Success(NativeMethods.git_tree_create_fromindex(out treeOid, Index.Handle));
            var tree = this.Lookup<Tree>(new ObjectId(treeOid));

            var parents = RetrieveParentsOfTheCommitBeingCreated(amendPreviousCommit);

            return ObjectDatabase.CreateCommit(message, author, committer, tree, parents, "HEAD");
        }

        private IEnumerable<Commit> RetrieveParentsOfTheCommitBeingCreated(bool amendPreviousCommit)
        {
            if (amendPreviousCommit)
            {
                return Head.Tip.Parents;
            }

            if (Info.IsEmpty)
            {
                return Enumerable.Empty<Commit>();
            }

            return new[] { Head.Tip };
        }


        internal T RegisterForCleanup<T>(T disposable) where T : IDisposable
        {
            toCleanup.Push(disposable);
            return disposable;
        }

        /// <summary>
        ///   Gets the current LibGit2Sharp version.
        ///   <para>
        ///     The format of the version number is as follows:
        ///     <para>Major.Minor.Patch-LibGit2Sharp_abbrev_hash-libgit2_abbrev_hash (x86|amd64)</para>
        ///   </para>
        /// </summary>
        public static string Version
        {
            get { return versionRetriever.Value; }
        }

        private static string RetrieveVersion()
        {
            Assembly assembly = typeof(Repository).Assembly;

            Version version = assembly.GetName().Version;

            string libgit2Hash = ReadContentFromResource(assembly, "libgit2_hash.txt");
            string libgit2sharpHash = ReadContentFromResource(assembly, "libgit2sharp_hash.txt");

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1}-{2} ({3})",
                version.ToString(3),
                libgit2sharpHash.Substring(0, 7),
                libgit2Hash.Substring(0, 7),
                NativeMethods.ProcessorArchitecture
                );
        }

        private static string ReadContentFromResource(Assembly assembly, string partialResourceName)
        {
            string name = string.Format(CultureInfo.InvariantCulture, "LibGit2Sharp.{0}", partialResourceName);
            using (var sr = new StreamReader(assembly.GetManifestResourceStream(name)))
            {
                return sr.ReadLine();
            }
        }

        /// <summary>
        ///   Internal method that actually performs fetch given a handle to the remote to perform the fetch from.
        ///   Caller is responsible for dispoising the remote handle.
        ///   
        ///   This allows fetching by a named remote or by url.
        /// </summary>
        /// <param name="remoteHandle"></param>
        /// <param name="fetchProgress"></param>
        private void FetchInternal(RemoteSafeHandle remoteHandle, FetchProgress fetchProgress)
        {
            // reset the current progress object
            fetchProgress.Reset();

            try
            {
                NativeMethods.git_remote_set_callbacks(remoteHandle, ref fetchProgress.RemoteCallbacks.GitCallbacks);

                int res = NativeMethods.git_remote_connect(remoteHandle, GitDirection.Fetch);
                Ensure.Success(res);

                int downloadResult = NativeMethods.git_remote_download(remoteHandle, ref fetchProgress.bytes, fetchProgress.indexerStats);
                Ensure.Success(downloadResult);
            }
            finally
            {
                if (remoteHandle != null)
                {
                    NativeMethods.git_remote_disconnect(remoteHandle);
                }
            }

            // update references
            int updateTipsResult = NativeMethods.git_remote_update_tips(remoteHandle);
            Ensure.Success(updateTipsResult);
        }
    }
}
