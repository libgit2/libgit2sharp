using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// A Repository is the primary interface into a git repository
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class Repository : IRepository
    {
        private readonly bool isBare;
        private readonly BranchCollection branches;
        private readonly CommitLog commits;
        private readonly Lazy<Configuration> config;
        private readonly RepositoryHandle handle;
        private readonly Lazy<Index> index;
        private readonly ReferenceCollection refs;
        private readonly TagCollection tags;
        private readonly StashCollection stashes;
        private readonly Lazy<RepositoryInformation> info;
        private readonly Diff diff;
        private readonly NoteCollection notes;
        private readonly Lazy<ObjectDatabase> odb;
        private readonly Lazy<Network> network;
        private readonly Lazy<Rebase> rebaseOperation;
        private readonly Stack<IDisposable> toCleanup = new Stack<IDisposable>();
        private readonly Ignore ignore;
        private readonly SubmoduleCollection submodules;
        private readonly WorktreeCollection worktrees;
        private readonly Lazy<PathCase> pathCase;

        [Flags]
        private enum RepositoryRequiredParameter
        {
            None = 0,
            Path = 1,
            Options = 2,
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository"/> class
        /// that does not point to an on-disk Git repository.  This is
        /// suitable only for custom, in-memory Git repositories that are
        /// configured with custom object database, reference database and/or
        /// configuration backends.
        /// </summary>
        public Repository()
            : this(null, null, RepositoryRequiredParameter.None)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository"/> class.
        /// <para>For a standard repository, <paramref name="path"/> should either point to the ".git" folder or to the working directory. For a bare repository, <paramref name="path"/> should directly point to the repository folder.</para>
        /// </summary>
        /// <param name="path">
        /// The path to the git repository to open, can be either the path to the git directory (for non-bare repositories this
        /// would be the ".git" folder inside the working directory) or the path to the working directory.
        /// </param>
        public Repository(string path)
            : this(path, null, RepositoryRequiredParameter.Path)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository"/> class,
        /// providing optional behavioral overrides through the
        /// <paramref name="options"/> parameter.
        /// <para>For a standard repository, <paramref name="path"/> may
        /// either point to the ".git" folder or to the working directory.
        /// For a bare repository, <paramref name="path"/> should directly
        /// point to the repository folder.</para>
        /// </summary>
        /// <param name="path">
        /// The path to the git repository to open, can be either the
        /// path to the git directory (for non-bare repositories this
        /// would be the ".git" folder inside the working directory)
        /// or the path to the working directory.
        /// </param>
        /// <param name="options">
        /// Overrides to the way a repository is opened.
        /// </param>
        public Repository(string path, RepositoryOptions options) :
            this(path, options, RepositoryRequiredParameter.Path | RepositoryRequiredParameter.Options)
        {
        }

        internal Repository(WorktreeHandle worktreeHandle)
        {
            try
            {
                handle = Proxy.git_repository_open_from_worktree(worktreeHandle);
                RegisterForCleanup(handle);
                RegisterForCleanup(worktreeHandle);

                isBare = Proxy.git_repository_is_bare(handle);

                Func<Index> indexBuilder = () => new Index(this);

                string configurationGlobalFilePath = null;
                string configurationXDGFilePath = null;
                string configurationSystemFilePath = null;

                if (!isBare)
                {
                    index = new Lazy<Index>(() => indexBuilder());
                }

                commits = new CommitLog(this);
                refs = new ReferenceCollection(this);
                branches = new BranchCollection(this);
                tags = new TagCollection(this);
                stashes = new StashCollection(this);
                info = new Lazy<RepositoryInformation>(() => new RepositoryInformation(this, isBare));
                config = new Lazy<Configuration>(() => RegisterForCleanup(new Configuration(this,
                                                                                            null,
                                                                                            configurationGlobalFilePath,
                                                                                            configurationXDGFilePath,
                                                                                            configurationSystemFilePath)));
                odb = new Lazy<ObjectDatabase>(() => new ObjectDatabase(this));
                diff = new Diff(this);
                notes = new NoteCollection(this);
                ignore = new Ignore(this);
                network = new Lazy<Network>(() => new Network(this));
                rebaseOperation = new Lazy<Rebase>(() => new Rebase(this));
                pathCase = new Lazy<PathCase>(() => new PathCase(this));
                submodules = new SubmoduleCollection(this);
                worktrees = new WorktreeCollection(this);
            }
            catch
            {
                CleanupDisposableDependencies();
                throw;
            }
        }

        private Repository(string path, RepositoryOptions options, RepositoryRequiredParameter requiredParameter)
        {
            if ((requiredParameter & RepositoryRequiredParameter.Path) == RepositoryRequiredParameter.Path)
            {
                Ensure.ArgumentNotNullOrEmptyString(path, "path");
            }

            if ((requiredParameter & RepositoryRequiredParameter.Options) == RepositoryRequiredParameter.Options)
            {
                Ensure.ArgumentNotNull(options, "options");
            }

            try
            {
                handle = (path != null) ? Proxy.git_repository_open(path) : Proxy.git_repository_new();
                RegisterForCleanup(handle);

                isBare = Proxy.git_repository_is_bare(handle);

                /* TODO: bug in libgit2, update when fixed by
                 * https://github.com/libgit2/libgit2/pull/2970
                 */
                if (path == null)
                {
                    isBare = true;
                }

                Func<Index> indexBuilder = () => new Index(this);

                string configurationGlobalFilePath = null;
                string configurationXDGFilePath = null;
                string configurationSystemFilePath = null;

                if (options != null)
                {
                    bool isWorkDirNull = string.IsNullOrEmpty(options.WorkingDirectoryPath);
                    bool isIndexNull = string.IsNullOrEmpty(options.IndexPath);

                    if (isBare && (isWorkDirNull ^ isIndexNull))
                    {
                        throw new ArgumentException("When overriding the opening of a bare repository, both RepositoryOptions.WorkingDirectoryPath an RepositoryOptions.IndexPath have to be provided.");
                    }

                    if (!isWorkDirNull)
                    {
                        isBare = false;
                    }

                    if (!isIndexNull)
                    {
                        indexBuilder = () => new Index(this, options.IndexPath);
                    }

                    if (!isWorkDirNull)
                    {
                        Proxy.git_repository_set_workdir(handle, options.WorkingDirectoryPath);
                    }

                    if (options.Identity != null)
                    {
                        Proxy.git_repository_set_ident(handle, options.Identity.Name, options.Identity.Email);
                    }
                }

                if (!isBare)
                {
                    index = new Lazy<Index>(() => indexBuilder());
                }

                commits = new CommitLog(this);
                refs = new ReferenceCollection(this);
                branches = new BranchCollection(this);
                tags = new TagCollection(this);
                stashes = new StashCollection(this);
                info = new Lazy<RepositoryInformation>(() => new RepositoryInformation(this, isBare));
                config = new Lazy<Configuration>(() => RegisterForCleanup(new Configuration(this,
                                                                                            null,
                                                                                            configurationGlobalFilePath,
                                                                                            configurationXDGFilePath,
                                                                                            configurationSystemFilePath)));
                odb = new Lazy<ObjectDatabase>(() => new ObjectDatabase(this));
                diff = new Diff(this);
                notes = new NoteCollection(this);
                ignore = new Ignore(this);
                network = new Lazy<Network>(() => new Network(this));
                rebaseOperation = new Lazy<Rebase>(() => new Rebase(this));
                pathCase = new Lazy<PathCase>(() => new PathCase(this));
                submodules = new SubmoduleCollection(this);
                worktrees = new WorktreeCollection(this);

                EagerlyLoadComponentsWithSpecifiedPaths(options);
            }
            catch
            {
                CleanupDisposableDependencies();
                throw;
            }
        }

        /// <summary>
        /// Check if parameter <paramref name="path"/> leads to a valid git repository.
        /// </summary>
        /// <param name="path">
        /// The path to the git repository to check, can be either the path to the git directory (for non-bare repositories this
        /// would be the ".git" folder inside the working directory) or the path to the working directory.
        /// </param>
        /// <returns>True if a repository can be resolved through this path; false otherwise</returns>
        static public bool IsValid(string path)
        {
            Ensure.ArgumentNotNull(path, "path");

            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                Proxy.git_repository_open_ext(path, RepositoryOpenFlags.NoSearch, null);
            }
            catch (RepositoryNotFoundException)
            {
                return false;
            }

            return true;
        }

        private void EagerlyLoadComponentsWithSpecifiedPaths(RepositoryOptions options)
        {
            if (options == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(options.IndexPath))
            {
                // Another dirty hack to avoid warnings
                if (Index.Count < 0)
                {
                    throw new InvalidOperationException("Unexpected state.");
                }
            }
        }

        internal RepositoryHandle Handle
        {
            get { return handle; }
        }

        /// <summary>
        /// Shortcut to return the branch pointed to by HEAD
        /// </summary>
        public Branch Head
        {
            get
            {
                Reference reference = Refs.Head;

                if (reference == null)
                {
                    throw new LibGit2SharpException("Corrupt repository. The 'HEAD' reference is missing.");
                }

                if (reference is SymbolicReference)
                {
                    return new Branch(this, reference);
                }

                return new DetachedHead(this, reference);
            }
        }

        /// <summary>
        /// Provides access to the configuration settings for this repository.
        /// </summary>
        public Configuration Config
        {
            get { return config.Value; }
        }

        /// <summary>
        /// Gets the index.
        /// </summary>
        public Index Index
        {
            get
            {
                if (isBare)
                {
                    throw new BareRepositoryException("Index is not available in a bare repository.");
                }

                return index != null ? index.Value : null;
            }
        }

        /// <summary>
        /// Manipulate the currently ignored files.
        /// </summary>
        public Ignore Ignore
        {
            get { return ignore; }
        }

        /// <summary>
        /// Provides access to network functionality for a repository.
        /// </summary>
        public Network Network
        {
            get { return network.Value; }
        }

        /// <summary>
        /// Provides access to rebase functionality for a repository.
        /// </summary>
        public Rebase Rebase
        {
            get
            {
                return rebaseOperation.Value;
            }
        }

        /// <summary>
        /// Gets the database.
        /// </summary>
        public ObjectDatabase ObjectDatabase
        {
            get { return odb.Value; }
        }

        /// <summary>
        /// Lookup and enumerate references in the repository.
        /// </summary>
        public ReferenceCollection Refs
        {
            get { return refs; }
        }

        /// <summary>
        /// Lookup and enumerate commits in the repository.
        /// Iterating this collection directly starts walking from the HEAD.
        /// </summary>
        public IQueryableCommitLog Commits
        {
            get { return commits; }
        }

        /// <summary>
        /// Lookup and enumerate branches in the repository.
        /// </summary>
        public BranchCollection Branches
        {
            get { return branches; }
        }

        /// <summary>
        /// Lookup and enumerate tags in the repository.
        /// </summary>
        public TagCollection Tags
        {
            get { return tags; }
        }

        ///<summary>
        /// Lookup and enumerate stashes in the repository.
        ///</summary>
        public StashCollection Stashes
        {
            get { return stashes; }
        }

        /// <summary>
        /// Provides high level information about this repository.
        /// </summary>
        public RepositoryInformation Info
        {
            get { return info.Value; }
        }

        /// <summary>
        /// Provides access to diffing functionalities to show changes between the working tree and the index or a tree, changes between the index and a tree, changes between two trees, or changes between two files on disk.
        /// </summary>
        public Diff Diff
        {
            get { return diff; }
        }

        /// <summary>
        /// Lookup notes in the repository.
        /// </summary>
        public NoteCollection Notes
        {
            get { return notes; }
        }

        /// <summary>
        /// Submodules in the repository.
        /// </summary>
        public SubmoduleCollection Submodules
        {
            get { return submodules; }
        }

        /// <summary>
        /// Worktrees in the repository.
        /// </summary>
        public WorktreeCollection Worktrees
        {
            get { return worktrees; }
        }

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        private void Dispose(bool disposing)
        {
            CleanupDisposableDependencies();
        }

        #endregion

        /// <summary>
        /// Initialize a repository at the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path to the working folder when initializing a standard ".git" repository. Otherwise, when initializing a bare repository, the path to the expected location of this later.</param>
        /// <returns>The path to the created repository.</returns>
        public static string Init(string path)
        {
            return Init(path, false);
        }

        /// <summary>
        /// Initialize a repository at the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path to the working folder when initializing a standard ".git" repository. Otherwise, when initializing a bare repository, the path to the expected location of this later.</param>
        /// <param name="isBare">true to initialize a bare repository. False otherwise, to initialize a standard ".git" repository.</param>
        /// <returns>The path to the created repository.</returns>
        public static string Init(string path, bool isBare)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            using (RepositoryHandle repo = Proxy.git_repository_init_ext(null, path, isBare))
            {
                FilePath repoPath = Proxy.git_repository_path(repo);
                return repoPath.Native;
            }
        }

        /// <summary>
        /// Initialize a repository by explictly setting the path to both the working directory and the git directory.
        /// </summary>
        /// <param name="workingDirectoryPath">The path to the working directory.</param>
        /// <param name="gitDirectoryPath">The path to the git repository to be created.</param>
        /// <returns>The path to the created repository.</returns>
        public static string Init(string workingDirectoryPath, string gitDirectoryPath)
        {
            Ensure.ArgumentNotNullOrEmptyString(workingDirectoryPath, "workingDirectoryPath");
            Ensure.ArgumentNotNullOrEmptyString(gitDirectoryPath, "gitDirectoryPath");

            // When being passed a relative workdir path, libgit2 will evaluate it from the
            // path to the repository. We pass a fully rooted path in order for the LibGit2Sharp caller
            // to pass a path relatively to his current directory.
            string wd = Path.GetFullPath(workingDirectoryPath);

            // TODO: Shouldn't we ensure that the working folder isn't under the gitDir?

            using (RepositoryHandle repo = Proxy.git_repository_init_ext(wd, gitDirectoryPath, false))
            {
                FilePath repoPath = Proxy.git_repository_path(repo);
                return repoPath.Native;
            }
        }

        /// <summary>
        /// Try to lookup an object by its <see cref="ObjectId"/>. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name="id">The id to lookup.</param>
        /// <returns>The <see cref="GitObject"/> or null if it was not found.</returns>
        public GitObject Lookup(ObjectId id)
        {
            return LookupInternal(id, GitObjectType.Any, null);
        }

        /// <summary>
        /// Try to lookup an object by its sha or a reference canonical name. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name="objectish">A revparse spec for the object to lookup.</param>
        /// <returns>The <see cref="GitObject"/> or null if it was not found.</returns>
        public GitObject Lookup(string objectish)
        {
            return Lookup(objectish, GitObjectType.Any, LookUpOptions.None);
        }

        /// <summary>
        /// Try to lookup an object by its <see cref="ObjectId"/> and <see cref="ObjectType"/>. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name="id">The id to lookup.</param>
        /// <param name="type">The kind of GitObject being looked up</param>
        /// <returns>The <see cref="GitObject"/> or null if it was not found.</returns>
        public GitObject Lookup(ObjectId id, ObjectType type)
        {
            return LookupInternal(id, type.ToGitObjectType(), null);
        }

        /// <summary>
        /// Try to lookup an object by its sha or a reference canonical name and <see cref="ObjectType"/>. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name="objectish">A revparse spec for the object to lookup.</param>
        /// <param name="type">The kind of <see cref="GitObject"/> being looked up</param>
        /// <returns>The <see cref="GitObject"/> or null if it was not found.</returns>
        public GitObject Lookup(string objectish, ObjectType type)
        {
            return Lookup(objectish, type.ToGitObjectType(), LookUpOptions.None);
        }

        internal GitObject LookupInternal(ObjectId id, GitObjectType type, string knownPath)
        {
            Ensure.ArgumentNotNull(id, "id");

            using (ObjectHandle obj = Proxy.git_object_lookup(handle, id, type))
            {
                if (obj == null || obj.IsNull)
                {
                    return null;
                }

                return GitObject.BuildFrom(this, id, Proxy.git_object_type(obj), knownPath);
            }
        }

        private static string PathFromRevparseSpec(string spec)
        {
            if (spec.StartsWith(":/", StringComparison.Ordinal))
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
            Ensure.ArgumentNotNullOrEmptyString(objectish, "objectish");

            GitObject obj;
            using (ObjectHandle sh = Proxy.git_revparse_single(handle, objectish))
            {
                if (sh == null)
                {
                    if (lookUpOptions.HasFlag(LookUpOptions.ThrowWhenNoGitObjectHasBeenFound))
                    {
                        Ensure.GitObjectIsNotNull(null, objectish);
                    }

                    return null;
                }

                GitObjectType objType = Proxy.git_object_type(sh);

                if (type != GitObjectType.Any && objType != type)
                {
                    return null;
                }

                obj = GitObject.BuildFrom(this, Proxy.git_object_id(sh), objType, PathFromRevparseSpec(objectish));
            }

            if (lookUpOptions.HasFlag(LookUpOptions.DereferenceResultToCommit))
            {
                return obj.Peel<Commit>(lookUpOptions.HasFlag(LookUpOptions.ThrowWhenCanNotBeDereferencedToACommit));
            }

            return obj;
        }

        internal Commit LookupCommit(string committish)
        {
            return (Commit)Lookup(committish,
                                  GitObjectType.Any,
                                  LookUpOptions.ThrowWhenNoGitObjectHasBeenFound |
                                  LookUpOptions.DereferenceResultToCommit |
                                  LookUpOptions.ThrowWhenCanNotBeDereferencedToACommit);
        }

        /// <summary>
        /// Lists the Remote Repository References.
        /// </summary>
        /// <para>
        /// Does not require a local Repository. The retrieved
        /// <see cref="IBelongToARepository.Repository"/>
        /// throws <see cref="InvalidOperationException"/> in this case.
        /// </para>
        /// <param name="url">The url to list from.</param>
        /// <returns>The references in the remote repository.</returns>
        public static IEnumerable<Reference> ListRemoteReferences(string url)
        {
            return ListRemoteReferences(url, null);
        }

        /// <summary>
        /// Lists the Remote Repository References.
        /// </summary>
        /// <para>
        /// Does not require a local Repository. The retrieved
        /// <see cref="IBelongToARepository.Repository"/>
        /// throws <see cref="InvalidOperationException"/> in this case.
        /// </para>
        /// <param name="url">The url to list from.</param>
        /// <param name="credentialsProvider">The <see cref="Func{Credentials}"/> used to connect to remote repository.</param>
        /// <returns>The references in the remote repository.</returns>
        public static IEnumerable<Reference> ListRemoteReferences(string url, CredentialsHandler credentialsProvider)
        {
            Ensure.ArgumentNotNull(url, "url");

            using (RepositoryHandle repositoryHandle = Proxy.git_repository_new())
            using (RemoteHandle remoteHandle = Proxy.git_remote_create_anonymous(repositoryHandle, url))
            {
                var gitCallbacks = new GitRemoteCallbacks { version = 1 };
                var proxyOptions = new GitProxyOptions { Version = 1 };

                if (credentialsProvider != null)
                {
                    var callbacks = new RemoteCallbacks(credentialsProvider);
                    gitCallbacks = callbacks.GenerateCallbacks();
                }

                Proxy.git_remote_connect(remoteHandle, GitDirection.Fetch, ref gitCallbacks, ref proxyOptions);
                return Proxy.git_remote_ls(null, remoteHandle);
            }
        }

        /// <summary>
        /// Probe for a git repository.
        /// <para>The lookup start from <paramref name="startingPath"/> and walk upward parent directories if nothing has been found.</para>
        /// </summary>
        /// <param name="startingPath">The base path where the lookup starts.</param>
        /// <returns>The path to the git repository, or null if no repository was found.</returns>
        public static string Discover(string startingPath)
        {
            FilePath discoveredPath = Proxy.git_repository_discover(startingPath);

            if (discoveredPath == null)
            {
                return null;
            }

            return discoveredPath.Native;
        }

        /// <summary>
        /// Clone using default options.
        /// </summary>
        /// <exception cref="RecurseSubmodulesException">This exception is thrown when there
        /// is an error is encountered while recursively cloning submodules. The inner exception
        /// will contain the original exception. The initially cloned repository would
        /// be reported through the <see cref="RecurseSubmodulesException.InitialRepositoryPath"/>
        /// property.</exception>"
        /// <exception cref="UserCancelledException">Exception thrown when the cancelling
        /// the clone of the initial repository.</exception>"
        /// <param name="sourceUrl">URI for the remote repository</param>
        /// <param name="workdirPath">Local path to clone into</param>
        /// <returns>The path to the created repository.</returns>
        public static string Clone(string sourceUrl, string workdirPath)
        {
            return Clone(sourceUrl, workdirPath, null);
        }

        /// <summary>
        /// Clone with specified options.
        /// </summary>
        /// <exception cref="RecurseSubmodulesException">This exception is thrown when there
        /// is an error is encountered while recursively cloning submodules. The inner exception
        /// will contain the original exception. The initially cloned repository would
        /// be reported through the <see cref="RecurseSubmodulesException.InitialRepositoryPath"/>
        /// property.</exception>"
        /// <exception cref="UserCancelledException">Exception thrown when the cancelling
        /// the clone of the initial repository.</exception>"
        /// <param name="sourceUrl">URI for the remote repository</param>
        /// <param name="workdirPath">Local path to clone into</param>
        /// <param name="options"><see cref="CloneOptions"/> controlling clone behavior</param>
        /// <returns>The path to the created repository.</returns>
        public static string Clone(string sourceUrl, string workdirPath,
            CloneOptions options)
        {
            Ensure.ArgumentNotNull(sourceUrl, "sourceUrl");
            Ensure.ArgumentNotNull(workdirPath, "workdirPath");

            options = options ?? new CloneOptions();

            // context variable that contains information on the repository that
            // we are cloning.
            var context = new RepositoryOperationContext(Path.GetFullPath(workdirPath), sourceUrl);

            // Notify caller that we are starting to work with the current repository.
            bool continueOperation = OnRepositoryOperationStarting(options.RepositoryOperationStarting,
                                                                   context);

            if (!continueOperation)
            {
                throw new UserCancelledException("Clone cancelled by the user.");
            }

            using (var checkoutOptionsWrapper = new GitCheckoutOptsWrapper(options))
            using (var fetchOptionsWrapper = new GitFetchOptionsWrapper())
            {
                var gitCheckoutOptions = checkoutOptionsWrapper.Options;

                var gitFetchOptions = fetchOptionsWrapper.Options;
                gitFetchOptions.ProxyOptions = new GitProxyOptions { Version = 1 };
                gitFetchOptions.RemoteCallbacks = new RemoteCallbacks(options).GenerateCallbacks();
                if (options.FetchOptions != null && options.FetchOptions.CustomHeaders != null)
                {
                    gitFetchOptions.CustomHeaders =
                        GitStrArrayManaged.BuildFrom(options.FetchOptions.CustomHeaders);
                }

                var cloneOpts = new GitCloneOptions
                {
                    Version = 1,
                    Bare = options.IsBare ? 1 : 0,
                    CheckoutOpts = gitCheckoutOptions,
                    FetchOpts = gitFetchOptions,
                };

                string clonedRepoPath;

                try
                {
                    cloneOpts.CheckoutBranch = StrictUtf8Marshaler.FromManaged(options.BranchName);

                    using (RepositoryHandle repo = Proxy.git_clone(sourceUrl, workdirPath, ref cloneOpts))
                    {
                        clonedRepoPath = Proxy.git_repository_path(repo).Native;
                    }
                }
                finally
                {
                    EncodingMarshaler.Cleanup(cloneOpts.CheckoutBranch);
                }

                // Notify caller that we are done with the current repository.
                OnRepositoryOperationCompleted(options.RepositoryOperationCompleted,
                                               context);

                // Recursively clone submodules if requested.
                try
                {
                    RecursivelyCloneSubmodules(options, clonedRepoPath, 1);
                }
                catch (Exception ex)
                {
                    throw new RecurseSubmodulesException("The top level repository was cloned, but there was an error cloning its submodules.",
                                                         ex,
                                                         clonedRepoPath);
                }

                return clonedRepoPath;
            }
        }

        /// <summary>
        /// Recursively clone submodules if directed to do so by the clone options.
        /// </summary>
        /// <param name="options">Options controlling clone behavior.</param>
        /// <param name="repoPath">Path of the parent repository.</param>
        /// <param name="recursionDepth">The current depth of the recursion.</param>
        private static void RecursivelyCloneSubmodules(CloneOptions options, string repoPath, int recursionDepth)
        {
            if (options.RecurseSubmodules)
            {
                List<string> submodules = new List<string>();

                using (Repository repo = new Repository(repoPath))
                {
                    SubmoduleUpdateOptions updateOptions = new SubmoduleUpdateOptions()
                    {
                        Init = true,
                        CredentialsProvider = options.CredentialsProvider,
                        OnCheckoutProgress = options.OnCheckoutProgress,
                        OnProgress = options.OnProgress,
                        OnTransferProgress = options.OnTransferProgress,
                        OnUpdateTips = options.OnUpdateTips,
                    };

                    string parentRepoWorkDir = repo.Info.WorkingDirectory;

                    // Iterate through the submodules (where the submodule is in the index),
                    // and clone them.
                    foreach (var sm in repo.Submodules.Where(sm => sm.RetrieveStatus().HasFlag(SubmoduleStatus.InIndex)))
                    {
                        string fullSubmodulePath = Path.Combine(parentRepoWorkDir, sm.Path);

                        // Resolve the URL in the .gitmodule file to the one actually used
                        // to clone
                        string resolvedUrl = Proxy.git_submodule_resolve_url(repo.Handle, sm.Url);

                        var context = new RepositoryOperationContext(fullSubmodulePath,
                                                                     resolvedUrl,
                                                                     parentRepoWorkDir,
                                                                     sm.Name,
                                                                     recursionDepth);

                        bool continueOperation = OnRepositoryOperationStarting(options.RepositoryOperationStarting,
                                                                               context);

                        if (!continueOperation)
                        {
                            throw new UserCancelledException("Recursive clone of submodules was cancelled.");
                        }

                        repo.Submodules.Update(sm.Name, updateOptions);

                        OnRepositoryOperationCompleted(options.RepositoryOperationCompleted,
                                                       context);

                        submodules.Add(Path.Combine(repo.Info.WorkingDirectory, sm.Path));
                    }
                }

                // If we are continuing the recursive operation, then
                // recurse into nested submodules.
                // Check submodules to see if they have their own submodules.
                foreach (string submodule in submodules)
                {
                    RecursivelyCloneSubmodules(options, submodule, recursionDepth + 1);
                }
            }
        }

        /// <summary>
        /// If a callback has been provided to notify callers that we are
        /// either starting to work on a repository.
        /// </summary>
        /// <param name="repositoryChangedCallback">The callback to notify change.</param>
        /// <param name="context">Context of the repository this operation affects.</param>
        /// <returns>true to continue the operation, false to cancel.</returns>
        private static bool OnRepositoryOperationStarting(
            RepositoryOperationStarting repositoryChangedCallback,
            RepositoryOperationContext context)
        {
            bool continueOperation = true;
            if (repositoryChangedCallback != null)
            {
                continueOperation = repositoryChangedCallback(context);
            }

            return continueOperation;
        }

        private static void OnRepositoryOperationCompleted(
            RepositoryOperationCompleted repositoryChangedCallback,
            RepositoryOperationContext context)
        {
            if (repositoryChangedCallback != null)
            {
                repositoryChangedCallback(context);
            }
        }

        /// <summary>
        /// Find where each line of a file originated.
        /// </summary>
        /// <param name="path">Path of the file to blame.</param>
        /// <param name="options">Specifies optional parameters; if null, the defaults are used.</param>
        /// <returns>The blame for the file.</returns>
        public BlameHunkCollection Blame(string path, BlameOptions options)
        {
            return new BlameHunkCollection(this, Handle, path, options ?? new BlameOptions());
        }

        /// <summary>
        /// Checkout the specified tree.
        /// </summary>
        /// <param name="tree">The <see cref="Tree"/> to checkout.</param>
        /// <param name="paths">The paths to checkout.</param>
        /// <param name="options">Collection of parameters controlling checkout behavior.</param>
        public void Checkout(Tree tree, IEnumerable<string> paths, CheckoutOptions options)
        {
            CheckoutTree(tree, paths != null ? paths.ToList() : null, options);
        }

        /// <summary>
        /// Checkout the specified tree.
        /// </summary>
        /// <param name="tree">The <see cref="Tree"/> to checkout.</param>
        /// <param name="paths">The paths to checkout.</param>
        /// <param name="opts">Collection of parameters controlling checkout behavior.</param>
        private void CheckoutTree(Tree tree, IList<string> paths, IConvertableToGitCheckoutOpts opts)
        {

            using (GitCheckoutOptsWrapper checkoutOptionsWrapper = new GitCheckoutOptsWrapper(opts, ToFilePaths(paths)))
            {
                var options = checkoutOptionsWrapper.Options;
                Proxy.git_checkout_tree(Handle, tree.Id, ref options);
            }
        }

        /// <summary>
        /// Sets the current <see cref="Head"/> to the specified commit and optionally resets the <see cref="Index"/> and
        /// the content of the working tree to match.
        /// </summary>
        /// <param name="resetMode">Flavor of reset operation to perform.</param>
        /// <param name="commit">The target commit object.</param>
        public void Reset(ResetMode resetMode, Commit commit)
        {
            Reset(resetMode, commit, new CheckoutOptions());
        }

        /// <summary>
        /// Sets <see cref="Head"/> to the specified commit and optionally resets the <see cref="Index"/> and
        /// the content of the working tree to match.
        /// </summary>
        /// <param name="resetMode">Flavor of reset operation to perform.</param>
        /// <param name="commit">The target commit object.</param>
        /// <param name="opts">Collection of parameters controlling checkout behavior.</param>
        public void Reset(ResetMode resetMode, Commit commit, CheckoutOptions opts)
        {
            Ensure.ArgumentNotNull(commit, "commit");
            Ensure.ArgumentNotNull(opts, "opts");

            using (GitCheckoutOptsWrapper checkoutOptionsWrapper = new GitCheckoutOptsWrapper(opts))
            {
                var options = checkoutOptionsWrapper.Options;
                Proxy.git_reset(handle, commit.Id, resetMode, ref options);
            }
        }

        /// <summary>
        /// Updates specifed paths in the index and working directory with the versions from the specified branch, reference, or SHA.
        /// <para>
        /// This method does not switch branches or update the current repository HEAD.
        /// </para>
        /// </summary>
        /// <param name = "committishOrBranchSpec">A revparse spec for the commit or branch to checkout paths from.</param>
        /// <param name="paths">The paths to checkout. Will throw if null is passed in. Passing an empty enumeration results in nothing being checked out.</param>
        /// <param name="checkoutOptions">Collection of parameters controlling checkout behavior.</param>
        public void CheckoutPaths(string committishOrBranchSpec, IEnumerable<string> paths, CheckoutOptions checkoutOptions)
        {
            Ensure.ArgumentNotNullOrEmptyString(committishOrBranchSpec, "committishOrBranchSpec");
            Ensure.ArgumentNotNull(paths, "paths");

            var listOfPaths = paths.ToList();

            // If there are no paths, then there is nothing to do.
            if (listOfPaths.Count == 0)
            {
                return;
            }

            Commit commit = LookupCommit(committishOrBranchSpec);

            CheckoutTree(commit.Tree, listOfPaths, checkoutOptions ?? new CheckoutOptions());
        }

        /// <summary>
        /// Stores the content of the <see cref="Repository.Index"/> as a new <see cref="LibGit2Sharp.Commit"/> into the repository.
        /// The tip of the <see cref="Repository.Head"/> will be used as the parent of this new Commit.
        /// Once the commit is created, the <see cref="Repository.Head"/> will move forward to point at it.
        /// </summary>
        /// <param name="message">The description of why a change was made to the repository.</param>
        /// <param name="author">The <see cref="Signature"/> of who made the change.</param>
        /// <param name="committer">The <see cref="Signature"/> of who added the change to the repository.</param>
        /// <param name="options">The <see cref="CommitOptions"/> that specify the commit behavior.</param>
        /// <returns>The generated <see cref="LibGit2Sharp.Commit"/>.</returns>
        public Commit Commit(string message, Signature author, Signature committer, CommitOptions options)
        {
            if (options == null)
            {
                options = new CommitOptions();
            }

            bool isHeadOrphaned = Info.IsHeadUnborn;

            if (options.AmendPreviousCommit && isHeadOrphaned)
            {
                throw new UnbornBranchException("Can not amend anything. The Head doesn't point at any commit.");
            }

            var treeId = Proxy.git_index_write_tree(Index.Handle);
            var tree = this.Lookup<Tree>(treeId);

            var parents = RetrieveParentsOfTheCommitBeingCreated(options.AmendPreviousCommit).ToList();

            if (parents.Count == 1 && !options.AllowEmptyCommit)
            {
                var treesame = parents[0].Tree.Id.Equals(treeId);
                var amendMergeCommit = options.AmendPreviousCommit && !isHeadOrphaned && Head.Tip.Parents.Count() > 1;

                if (treesame && !amendMergeCommit)
                {
                    throw (options.AmendPreviousCommit ? 
                        new EmptyCommitException("Amending this commit would produce a commit that is identical to its parent (id = {0})", parents[0].Id) :
                        new EmptyCommitException("No changes; nothing to commit."));
                }
            }

            Commit result = ObjectDatabase.CreateCommit(author, committer, message, tree, parents, options.PrettifyMessage, options.CommentaryChar);

            Proxy.git_repository_state_cleanup(handle);

            var logMessage = BuildCommitLogMessage(result, options.AmendPreviousCommit, isHeadOrphaned, parents.Count > 1);
            UpdateHeadAndTerminalReference(result, logMessage);

            return result;
        }

        private static string BuildCommitLogMessage(Commit commit, bool amendPreviousCommit, bool isHeadOrphaned, bool isMergeCommit)
        {
            string kind = string.Empty;
            if (isHeadOrphaned)
            {
                kind = " (initial)";
            }
            else if (amendPreviousCommit)
            {
                kind = " (amend)";
            }
            else if (isMergeCommit)
            {
                kind = " (merge)";
            }

            return string.Format(CultureInfo.InvariantCulture, "commit{0}: {1}", kind, commit.MessageShort);
        }

        private void UpdateHeadAndTerminalReference(Commit commit, string reflogMessage)
        {
            Reference reference = Refs.Head;

            while (true) //TODO: Implement max nesting level
            {
                if (reference is DirectReference)
                {
                    Refs.UpdateTarget(reference, commit.Id, reflogMessage);
                    return;
                }

                var symRef = (SymbolicReference)reference;

                reference = symRef.Target;

                if (reference == null)
                {
                    Refs.Add(symRef.TargetIdentifier, commit.Id, reflogMessage);
                    return;
                }
            }
        }

        private IEnumerable<Commit> RetrieveParentsOfTheCommitBeingCreated(bool amendPreviousCommit)
        {
            if (amendPreviousCommit)
            {
                return Head.Tip.Parents;
            }

            if (Info.IsHeadUnborn)
            {
                return Enumerable.Empty<Commit>();
            }

            var parents = new List<Commit> { Head.Tip };

            if (Info.CurrentOperation == CurrentOperation.Merge)
            {
                parents.AddRange(MergeHeads.Select(mh => mh.Tip));
            }

            return parents;
        }

        /// <summary>
        /// Clean the working tree by removing files that are not under version control.
        /// </summary>
        public unsafe void RemoveUntrackedFiles()
        {
            var options = new GitCheckoutOpts
            {
                version = 1,
                checkout_strategy = CheckoutStrategy.GIT_CHECKOUT_REMOVE_UNTRACKED
                                     | CheckoutStrategy.GIT_CHECKOUT_ALLOW_CONFLICTS,
            };

            Proxy.git_checkout_index(Handle, new ObjectHandle(null, false), ref options);
        }

        private void CleanupDisposableDependencies()
        {
            while (toCleanup.Count > 0)
            {
                toCleanup.Pop().SafeDispose();
            }
        }

        internal T RegisterForCleanup<T>(T disposable) where T : IDisposable
        {
            toCleanup.Push(disposable);
            return disposable;
        }

        /// <summary>
        /// Merges changes from commit into the branch pointed at by HEAD.
        /// </summary>
        /// <param name="commit">The commit to merge into the branch pointed at by HEAD.</param>
        /// <param name="merger">The <see cref="Signature"/> of who is performing the merge.</param>
        /// <param name="options">Specifies optional parameters controlling merge behavior; if null, the defaults are used.</param>
        /// <returns>The <see cref="MergeResult"/> of the merge.</returns>
        public MergeResult Merge(Commit commit, Signature merger, MergeOptions options)
        {
            Ensure.ArgumentNotNull(commit, "commit");
            Ensure.ArgumentNotNull(merger, "merger");

            options = options ?? new MergeOptions();

            using (AnnotatedCommitHandle annotatedCommitHandle = Proxy.git_annotated_commit_lookup(Handle, commit.Id.Oid))
            {
                return Merge(new[] { annotatedCommitHandle }, merger, options);
            }
        }

        /// <summary>
        /// Merges changes from branch into the branch pointed at by HEAD.
        /// </summary>
        /// <param name="branch">The branch to merge into the branch pointed at by HEAD.</param>
        /// <param name="merger">The <see cref="Signature"/> of who is performing the merge.</param>
        /// <param name="options">Specifies optional parameters controlling merge behavior; if null, the defaults are used.</param>
        /// <returns>The <see cref="MergeResult"/> of the merge.</returns>
        public MergeResult Merge(Branch branch, Signature merger, MergeOptions options)
        {
            Ensure.ArgumentNotNull(branch, "branch");
            Ensure.ArgumentNotNull(merger, "merger");

            options = options ?? new MergeOptions();

            using (ReferenceHandle referencePtr = Refs.RetrieveReferencePtr(branch.CanonicalName))
            using (AnnotatedCommitHandle annotatedCommitHandle = Proxy.git_annotated_commit_from_ref(Handle, referencePtr))
            {
                return Merge(new[] { annotatedCommitHandle }, merger, options);
            }
        }

        /// <summary>
        /// Merges changes from the commit into the branch pointed at by HEAD.
        /// </summary>
        /// <param name="committish">The commit to merge into the branch pointed at by HEAD.</param>
        /// <param name="merger">The <see cref="Signature"/> of who is performing the merge.</param>
        /// <param name="options">Specifies optional parameters controlling merge behavior; if null, the defaults are used.</param>
        /// <returns>The <see cref="MergeResult"/> of the merge.</returns>
        public MergeResult Merge(string committish, Signature merger, MergeOptions options)
        {
            Ensure.ArgumentNotNull(committish, "committish");
            Ensure.ArgumentNotNull(merger, "merger");

            options = options ?? new MergeOptions();

            Commit commit = LookupCommit(committish);
            return Merge(commit, merger, options);
        }

        /// <summary>
        /// Merge the reference that was recently fetched. This will merge
        /// the branch on the fetched remote that corresponded to the
        /// current local branch when we did the fetch.  This is the
        /// second step in performing a pull operation (after having
        /// performed said fetch).
        /// </summary>
        /// <param name="merger">The <see cref="Signature"/> of who is performing the merge.</param>
        /// <param name="options">Specifies optional parameters controlling merge behavior; if null, the defaults are used.</param>
        /// <returns>The <see cref="MergeResult"/> of the merge.</returns>
        public MergeResult MergeFetchedRefs(Signature merger, MergeOptions options)
        {
            Ensure.ArgumentNotNull(merger, "merger");

            options = options ?? new MergeOptions();

            // The current FetchHeads that are marked for merging.
            FetchHead[] fetchHeads = Network.FetchHeads.Where(fetchHead => fetchHead.ForMerge).ToArray();

            if (fetchHeads.Length == 0)
            {
                var expectedRef = this.Head.UpstreamBranchCanonicalName;
                throw new MergeFetchHeadNotFoundException("The current branch is configured to merge with the reference '{0}' from the remote, but this reference was not fetched.", 
                    expectedRef);
            }

            AnnotatedCommitHandle[] annotatedCommitHandles = fetchHeads.Select(fetchHead =>
                Proxy.git_annotated_commit_from_fetchhead(Handle, fetchHead.RemoteCanonicalName, fetchHead.Url, fetchHead.Target.Id.Oid)).ToArray();

            try
            {
                // Perform the merge.
                return Merge(annotatedCommitHandles, merger, options);
            }
            finally
            {
                // Cleanup.
                foreach (AnnotatedCommitHandle annotatedCommitHandle in annotatedCommitHandles)
                {
                    annotatedCommitHandle.Dispose();
                }
            }
        }

        /// <summary>
        /// Revert the specified commit.
        /// <para>
        ///  If the revert is successful but there are no changes to commit,
        ///  then the <see cref="RevertStatus"/> will be <see cref="RevertStatus.NothingToRevert"/>.
        ///  If the revert is successful and there are changes to revert, then
        ///  the <see cref="RevertStatus"/> will be <see cref="RevertStatus.Reverted"/>.
        ///  If the revert resulted in conflicts, then the <see cref="RevertStatus"/>
        ///  will be <see cref="RevertStatus.Conflicts"/>.
        /// </para>
        /// </summary>
        /// <param name="commit">The <see cref="Commit"/> to revert.</param>
        /// <param name="reverter">The <see cref="Signature"/> of who is performing the revert.</param>
        /// <param name="options"><see cref="RevertOptions"/> controlling revert behavior.</param>
        /// <returns>The result of the revert.</returns>
        public RevertResult Revert(Commit commit, Signature reverter, RevertOptions options)
        {
            Ensure.ArgumentNotNull(commit, "commit");
            Ensure.ArgumentNotNull(reverter, "reverter");

            if (Info.IsHeadUnborn)
            {
                throw new UnbornBranchException("Can not revert the commit. The Head doesn't point at a commit.");
            }

            options = options ?? new RevertOptions();

            RevertResult result = null;

            using (GitCheckoutOptsWrapper checkoutOptionsWrapper = new GitCheckoutOptsWrapper(options))
            {
                var mergeOptions = new GitMergeOpts
                {
                    Version = 1,
                    MergeFileFavorFlags = options.MergeFileFavor,
                    MergeTreeFlags = options.FindRenames ? GitMergeFlag.GIT_MERGE_FIND_RENAMES :
                                                           GitMergeFlag.GIT_MERGE_NORMAL,
                    RenameThreshold = (uint)options.RenameThreshold,
                    TargetLimit = (uint)options.TargetLimit,
                };

                GitRevertOpts gitRevertOpts = new GitRevertOpts()
                {
                    Mainline = (uint)options.Mainline,
                    MergeOpts = mergeOptions,

                    CheckoutOpts = checkoutOptionsWrapper.Options,
                };

                Proxy.git_revert(handle, commit.Id.Oid, gitRevertOpts);

                if (Index.IsFullyMerged)
                {
                    Commit revertCommit = null;

                    // Check if the revert generated any changes
                    // and set the revert status accordingly
                    bool anythingToRevert = RetrieveStatus(
                        new StatusOptions()
                        {
                            DetectRenamesInIndex = false,
                            Show = StatusShowOption.IndexOnly
                        }).Any();

                    RevertStatus revertStatus = anythingToRevert ?
                        RevertStatus.Reverted : RevertStatus.NothingToRevert;

                    if (options.CommitOnSuccess)
                    {
                        if (!anythingToRevert)
                        {
                            // If there were no changes to revert, and we are
                            // asked to commit the changes, then cleanup
                            // the repository state (following command line behavior).
                            Proxy.git_repository_state_cleanup(handle);
                        }
                        else
                        {
                            revertCommit = this.Commit(
                                Info.Message,
                                author: reverter,
                                committer: reverter,
                                options: null);
                        }
                    }

                    result = new RevertResult(revertStatus, revertCommit);
                }
                else
                {
                    result = new RevertResult(RevertStatus.Conflicts);
                }
            }

            return result;
        }

        /// <summary>
        /// Cherry-picks the specified commit.
        /// </summary>
        /// <param name="commit">The <see cref="Commit"/> to cherry-pick.</param>
        /// <param name="committer">The <see cref="Signature"/> of who is performing the cherry pick.</param>
        /// <param name="options"><see cref="CherryPickOptions"/> controlling cherry pick behavior.</param>
        /// <returns>The result of the cherry pick.</returns>
        public CherryPickResult CherryPick(Commit commit, Signature committer, CherryPickOptions options)
        {
            Ensure.ArgumentNotNull(commit, "commit");
            Ensure.ArgumentNotNull(committer, "committer");

            options = options ?? new CherryPickOptions();

            CherryPickResult result = null;

            using (var checkoutOptionsWrapper = new GitCheckoutOptsWrapper(options))
            {
                var mergeOptions = new GitMergeOpts
                {
                    Version = 1,
                    MergeFileFavorFlags = options.MergeFileFavor,
                    MergeTreeFlags = options.FindRenames ? GitMergeFlag.GIT_MERGE_FIND_RENAMES :
                                                           GitMergeFlag.GIT_MERGE_NORMAL,
                    RenameThreshold = (uint)options.RenameThreshold,
                    TargetLimit = (uint)options.TargetLimit,
                };

                var gitCherryPickOpts = new GitCherryPickOptions()
                {
                    Mainline = (uint)options.Mainline,
                    MergeOpts = mergeOptions,

                    CheckoutOpts = checkoutOptionsWrapper.Options,
                };

                Proxy.git_cherrypick(handle, commit.Id.Oid, gitCherryPickOpts);

                if (Index.IsFullyMerged)
                {
                    Commit cherryPickCommit = null;
                    if (options.CommitOnSuccess)
                    {
                        cherryPickCommit = this.Commit(Info.Message, commit.Author, committer, null);
                    }

                    result = new CherryPickResult(CherryPickStatus.CherryPicked, cherryPickCommit);
                }
                else
                {
                    result = new CherryPickResult(CherryPickStatus.Conflicts);
                }
            }

            return result;
        }

        private FastForwardStrategy FastForwardStrategyFromMergePreference(GitMergePreference preference)
        {
            switch (preference)
            {
                case GitMergePreference.GIT_MERGE_PREFERENCE_NONE:
                    return FastForwardStrategy.Default;
                case GitMergePreference.GIT_MERGE_PREFERENCE_FASTFORWARD_ONLY:
                    return FastForwardStrategy.FastForwardOnly;
                case GitMergePreference.GIT_MERGE_PREFERENCE_NO_FASTFORWARD:
                    return FastForwardStrategy.NoFastForward;
                default:
                    throw new InvalidOperationException(String.Format("Unknown merge preference: {0}", preference));
            }
        }

        /// <summary>
        /// Internal implementation of merge.
        /// </summary>
        /// <param name="annotatedCommits">Merge heads to operate on.</param>
        /// <param name="merger">The <see cref="Signature"/> of who is performing the merge.</param>
        /// <param name="options">Specifies optional parameters controlling merge behavior; if null, the defaults are used.</param>
        /// <returns>The <see cref="MergeResult"/> of the merge.</returns>
        private MergeResult Merge(AnnotatedCommitHandle[] annotatedCommits, Signature merger, MergeOptions options)
        {
            GitMergeAnalysis mergeAnalysis;
            GitMergePreference mergePreference;

            Proxy.git_merge_analysis(Handle, annotatedCommits, out mergeAnalysis, out mergePreference);

            MergeResult mergeResult = null;

            if ((mergeAnalysis & GitMergeAnalysis.GIT_MERGE_ANALYSIS_UP_TO_DATE) == GitMergeAnalysis.GIT_MERGE_ANALYSIS_UP_TO_DATE)
            {
                return new MergeResult(MergeStatus.UpToDate);
            }

            FastForwardStrategy fastForwardStrategy = (options.FastForwardStrategy != FastForwardStrategy.Default) ?
                options.FastForwardStrategy : FastForwardStrategyFromMergePreference(mergePreference);

            switch (fastForwardStrategy)
            {
                case FastForwardStrategy.Default:
                    if (mergeAnalysis.HasFlag(GitMergeAnalysis.GIT_MERGE_ANALYSIS_FASTFORWARD))
                    {
                        if (annotatedCommits.Length != 1)
                        {
                            // We should not reach this code unless there is a bug somewhere.
                            throw new LibGit2SharpException("Unable to perform Fast-Forward merge with mith multiple merge heads.");
                        }

                        mergeResult = FastForwardMerge(annotatedCommits[0], options);
                    }
                    else if (mergeAnalysis.HasFlag(GitMergeAnalysis.GIT_MERGE_ANALYSIS_NORMAL))
                    {
                        mergeResult = NormalMerge(annotatedCommits, merger, options);
                    }
                    break;
                case FastForwardStrategy.FastForwardOnly:
                    if (mergeAnalysis.HasFlag(GitMergeAnalysis.GIT_MERGE_ANALYSIS_FASTFORWARD))
                    {
                        if (annotatedCommits.Length != 1)
                        {
                            // We should not reach this code unless there is a bug somewhere.
                            throw new LibGit2SharpException("Unable to perform Fast-Forward merge with mith multiple merge heads.");
                        }

                        mergeResult = FastForwardMerge(annotatedCommits[0], options);
                    }
                    else
                    {
                        // TODO: Maybe this condition should rather be indicated through the merge result
                        //       instead of throwing an exception.
                        throw new NonFastForwardException("Cannot perform fast-forward merge.");
                    }
                    break;
                case FastForwardStrategy.NoFastForward:
                    if (mergeAnalysis.HasFlag(GitMergeAnalysis.GIT_MERGE_ANALYSIS_NORMAL))
                    {
                        mergeResult = NormalMerge(annotatedCommits, merger, options);
                    }
                    break;
                default:
                    throw new NotImplementedException(
                        string.Format(CultureInfo.InvariantCulture, "Unknown fast forward strategy: {0}", mergeAnalysis));
            }

            if (mergeResult == null)
            {
                throw new NotImplementedException(
                    string.Format(CultureInfo.InvariantCulture, "Unknown merge analysis: {0}", options.FastForwardStrategy));
            }

            return mergeResult;
        }

        /// <summary>
        /// Perform a normal merge (i.e. a non-fast-forward merge).
        /// </summary>
        /// <param name="annotatedCommits">The merge head handles to merge.</param>
        /// <param name="merger">The <see cref="Signature"/> of who is performing the merge.</param>
        /// <param name="options">Specifies optional parameters controlling merge behavior; if null, the defaults are used.</param>
        /// <returns>The <see cref="MergeResult"/> of the merge.</returns>
        private MergeResult NormalMerge(AnnotatedCommitHandle[] annotatedCommits, Signature merger, MergeOptions options)
        {
            MergeResult mergeResult;
            GitMergeFlag treeFlags = options.FindRenames ? GitMergeFlag.GIT_MERGE_FIND_RENAMES
                                                              : GitMergeFlag.GIT_MERGE_NORMAL;

            if (options.FailOnConflict)
            {
                treeFlags |= GitMergeFlag.GIT_MERGE_FAIL_ON_CONFLICT;
            }

            if (options.SkipReuc)
            {
                treeFlags |= GitMergeFlag.GIT_MERGE_SKIP_REUC;
            }

            var fileFlags = options.IgnoreWhitespaceChange
                ? GitMergeFileFlag.GIT_MERGE_FILE_IGNORE_WHITESPACE_CHANGE
                : GitMergeFileFlag.GIT_MERGE_FILE_DEFAULT;

            var mergeOptions = new GitMergeOpts
            {
                Version = 1,
                MergeFileFavorFlags = options.MergeFileFavor,
                MergeTreeFlags = treeFlags,
                RenameThreshold = (uint)options.RenameThreshold,
                TargetLimit = (uint)options.TargetLimit,
                FileFlags = fileFlags
            };

            bool earlyStop;
            using (GitCheckoutOptsWrapper checkoutOptionsWrapper = new GitCheckoutOptsWrapper(options))
            {
                var checkoutOpts = checkoutOptionsWrapper.Options;

                Proxy.git_merge(Handle, annotatedCommits, mergeOptions, checkoutOpts, out earlyStop);
            }

            if (earlyStop)
            {
                return new MergeResult(MergeStatus.Conflicts);
            }

            if (Index.IsFullyMerged)
            {
                Commit mergeCommit = null;
                if (options.CommitOnSuccess)
                {
                    // Commit the merge
                    mergeCommit = Commit(Info.Message, author: merger, committer: merger, options: null);
                }

                mergeResult = new MergeResult(MergeStatus.NonFastForward, mergeCommit);
            }
            else
            {
                mergeResult = new MergeResult(MergeStatus.Conflicts);
            }

            return mergeResult;
        }

        /// <summary>
        /// Perform a fast-forward merge.
        /// </summary>
        /// <param name="annotatedCommit">The merge head handle to fast-forward merge.</param>
        /// <param name="options">Options controlling merge behavior.</param>
        /// <returns>The <see cref="MergeResult"/> of the merge.</returns>
        private MergeResult FastForwardMerge(AnnotatedCommitHandle annotatedCommit, MergeOptions options)
        {
            ObjectId id = Proxy.git_annotated_commit_id(annotatedCommit);
            Commit fastForwardCommit = (Commit)Lookup(id, ObjectType.Commit);
            Ensure.GitObjectIsNotNull(fastForwardCommit, id.Sha);

            CheckoutTree(fastForwardCommit.Tree, null, new FastForwardCheckoutOptionsAdapter(options));

            var reference = Refs.Head.ResolveToDirectReference();

            // TODO: This reflog entry could be more specific
            string refLogEntry = string.Format(
                CultureInfo.InvariantCulture, "merge {0}: Fast-forward", fastForwardCommit.Sha);

            if (reference == null)
            {
                // Reference does not exist, create it.
                Refs.Add(Refs.Head.TargetIdentifier, fastForwardCommit.Id, refLogEntry);
            }
            else
            {
                // Update target reference.
                Refs.UpdateTarget(reference, fastForwardCommit.Id.Sha, refLogEntry);
            }

            return new MergeResult(MergeStatus.FastForward, fastForwardCommit);
        }

        /// <summary>
        /// Gets the references to the tips that are currently being merged.
        /// </summary>
        internal IEnumerable<MergeHead> MergeHeads
        {
            get
            {
                int i = 0;
                return Proxy.git_repository_mergehead_foreach(Handle,
                    commitId => new MergeHead(this, commitId, i++));
            }
        }

        internal StringComparer PathComparer
        {
            get { return pathCase.Value.Comparer; }
        }

        internal FilePath[] ToFilePaths(IEnumerable<string> paths)
        {
            if (paths == null)
            {
                return null;
            }

            var filePaths = new List<FilePath>();

            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path))
                {
                    throw new ArgumentException("At least one provided path is either null or empty.", "paths");
                }

                filePaths.Add(this.BuildRelativePathFrom(path));
            }

            if (filePaths.Count == 0)
            {
                throw new ArgumentException("No path has been provided.", "paths");
            }

            return filePaths.ToArray();
        }

        /// <summary>
        /// Retrieves the state of a file in the working directory, comparing it against the staging area and the latest commit.
        /// </summary>
        /// <param name="filePath">The relative path within the working directory to the file.</param>
        /// <returns>A <see cref="FileStatus"/> representing the state of the <paramref name="filePath"/> parameter.</returns>
        public FileStatus RetrieveStatus(string filePath)
        {
            Ensure.ArgumentNotNullOrEmptyString(filePath, "filePath");

            string relativePath = this.BuildRelativePathFrom(filePath);

            return Proxy.git_status_file(Handle, relativePath);
        }

        /// <summary>
        /// Retrieves the state of all files in the working directory, comparing them against the staging area and the latest commit.
        /// </summary>
        /// <param name="options">If set, the options that control the status investigation.</param>
        /// <returns>A <see cref="RepositoryStatus"/> holding the state of all the files.</returns>
        public RepositoryStatus RetrieveStatus(StatusOptions options)
        {
            ReloadFromDisk();

            return new RepositoryStatus(this, options);
        }

        internal void ReloadFromDisk()
        {
            Proxy.git_index_read(Index.Handle);
        }

        internal void AddToIndex(string relativePath)
        {
            if (!Submodules.TryStage(relativePath, true))
            {
                Proxy.git_index_add_bypath(Index.Handle, relativePath);
            }
        }

        internal string RemoveFromIndex(string relativePath)
        {
            Proxy.git_index_remove_bypath(Index.Handle, relativePath);

            return relativePath;
        }

        internal void UpdatePhysicalIndex()
        {
            Proxy.git_index_write(Index.Handle);
        }

        /// <summary>
        /// Finds the most recent annotated tag that is reachable from a commit.
        /// <para>
        ///   If the tag points to the commit, then only the tag is shown. Otherwise,
        ///   it suffixes the tag name with the number of additional commits on top
        ///   of the tagged object and the abbreviated object name of the most recent commit.
        /// </para>
        /// <para>
        ///   Optionally, the <paramref name="options"/> parameter allow to tweak the
        ///   search strategy (considering lightweith tags, or even branches as reference points)
        ///   and the formatting of the returned identifier.
        /// </para>
        /// </summary>
        /// <param name="commit">The commit to be described.</param>
        /// <param name="options">Determines how the commit will be described.</param>
        /// <returns>A descriptive identifier for the commit based on the nearest annotated tag.</returns>
        public string Describe(Commit commit, DescribeOptions options)
        {
            Ensure.ArgumentNotNull(commit, "commit");
            Ensure.ArgumentNotNull(options, "options");

            return Proxy.git_describe_commit(handle, commit.Id, options);
        }

        /// <summary>
        /// Parse an extended SHA-1 expression and retrieve the object and the reference
        /// mentioned in the revision (if any).
        /// </summary>
        /// <param name="revision">An extended SHA-1 expression for the object to look up</param>
        /// <param name="reference">The reference mentioned in the revision (if any)</param>
        /// <param name="obj">The object which the revision resolves to</param>
        public void RevParse(string revision, out Reference reference, out GitObject obj)
        {
            var handles = Proxy.git_revparse_ext(Handle, revision);
            if (handles == null)
            {
                Ensure.GitObjectIsNotNull(null, revision);
            }

            using (var objH = handles.Item1)
            using (var refH = handles.Item2)
            {
                reference = refH.IsNull ? null : Reference.BuildFromPtr<Reference>(refH, this);
                obj = GitObject.BuildFrom(this, Proxy.git_object_id(objH), Proxy.git_object_type(objH), PathFromRevparseSpec(revision));
            }
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                                     "{0} = \"{1}\"",
                                     Info.IsBare ? "Gitdir" : "Workdir",
                                     Info.IsBare ? Info.Path : Info.WorkingDirectory);
            }
        }
    }
}
