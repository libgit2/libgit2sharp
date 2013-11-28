using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;
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
        private readonly RepositorySafeHandle handle;
        private readonly Index index;
        private readonly ReferenceCollection refs;
        private readonly TagCollection tags;
        private readonly StashCollection stashes;
        private readonly Lazy<RepositoryInformation> info;
        private readonly Diff diff;
        private readonly NoteCollection notes;
        private readonly Lazy<ObjectDatabase> odb;
        private readonly Lazy<Network> network;
        private readonly Stack<IDisposable> toCleanup = new Stack<IDisposable>();
        private readonly Ignore ignore;
        private readonly SubmoduleCollection submodules;
        private static readonly Lazy<string> versionRetriever = new Lazy<string>(RetrieveVersion);
        private readonly Lazy<PathCase> pathCase;

        /// <summary>
        /// Initializes a new instance of the <see cref="Repository"/> class, providing ooptional behavioral overrides through <paramref name="options"/> parameter.
        /// <para>For a standard repository, <paramref name="path"/> should either point to the ".git" folder or to the working directory. For a bare repository, <paramref name="path"/> should directly point to the repository folder.</para>
        /// </summary>
        /// <param name="path">
        /// The path to the git repository to open, can be either the path to the git directory (for non-bare repositories this
        /// would be the ".git" folder inside the working directory) or the path to the working directory.
        /// </param>
        /// <param name="options">
        /// Overrides to the way a repository is opened.
        /// </param>
        public Repository(string path, RepositoryOptions options = null)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            try
            {
                handle = Proxy.git_repository_open(path);
                RegisterForCleanup(handle);

                isBare = Proxy.git_repository_is_bare(handle);

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
                        throw new ArgumentException(
                            "When overriding the opening of a bare repository, both RepositoryOptions.WorkingDirectoryPath an RepositoryOptions.IndexPath have to be provided.");
                    }

                    isBare = false;

                    if (!isIndexNull)
                    {
                        indexBuilder = () => new Index(this, options.IndexPath);
                    }

                    if (!isWorkDirNull)
                    {
                        Proxy.git_repository_set_workdir(handle, options.WorkingDirectoryPath);
                    }

                    configurationGlobalFilePath = options.GlobalConfigurationLocation;
                    configurationXDGFilePath = options.XdgConfigurationLocation;
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
                stashes = new StashCollection(this);
                info = new Lazy<RepositoryInformation>(() => new RepositoryInformation(this, isBare));
                config =
                    new Lazy<Configuration>(
                        () =>
                        RegisterForCleanup(new Configuration(this, configurationGlobalFilePath, configurationXDGFilePath,
                                                             configurationSystemFilePath)));
                odb = new Lazy<ObjectDatabase>(() => new ObjectDatabase(this));
                diff = new Diff(this);
                notes = new NoteCollection(this);
                ignore = new Ignore(this);
                network = new Lazy<Network>(() => new Network(this));
                pathCase = new Lazy<PathCase>(() => new PathCase(this));
                submodules = new SubmoduleCollection(this);

                EagerlyLoadTheConfigIfAnyPathHaveBeenPassed(options);
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
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

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

        private void EagerlyLoadTheConfigIfAnyPathHaveBeenPassed(RepositoryOptions options)
        {
            if (options == null)
            {
                return;
            }

            if (options.GlobalConfigurationLocation == null &&
                options.XdgConfigurationLocation == null &&
                options.SystemConfigurationLocation == null)
            {
                return;
            }

            // Dirty hack to force the eager load of the configuration
            // without Resharper pestering about useless code

            if (!Config.HasConfig(ConfigurationLevel.Local))
            {
                throw new InvalidOperationException("Unexpected state.");
            }
        }

        internal RepositorySafeHandle Handle
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

                return index;
            }
        }

        /// <summary>
        /// Manipulate the currently ignored files.
        /// </summary>
        public Ignore Ignore
        {
            get
            {
                return ignore;
            }
        }

        /// <summary>
        /// Provides access to network functionality for a repository.
        /// </summary>
        public Network Network
        {
            get
            {
                return network.Value;
            }
        }

        /// <summary>
        /// Gets the database.
        /// </summary>
        public ObjectDatabase ObjectDatabase
        {
            get
            {
                return odb.Value;
            }
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

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
        /// <param name="isBare">true to initialize a bare repository. False otherwise, to initialize a standard ".git" repository.</param>
        /// <returns>The path to the created repository.</returns>
        public static string Init(string path, bool isBare = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            using (RepositorySafeHandle repo = Proxy.git_repository_init_ext(null, path, isBare))
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

            using (RepositorySafeHandle repo = Proxy.git_repository_init_ext(wd, gitDirectoryPath, false))
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

        internal GitObject LookupInternal(ObjectId id, GitObjectType type, FilePath knownPath)
        {
            Ensure.ArgumentNotNull(id, "id");

            GitObjectSafeHandle obj = null;

            try
            {
                obj = Proxy.git_object_lookup(handle, id, type);

                if (obj == null)
                {
                    return null;
                }

                return GitObject.BuildFrom(this, id, Proxy.git_object_type(obj), knownPath);
            }
            finally
            {
                obj.SafeDispose();
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
            Ensure.ArgumentNotNullOrEmptyString(objectish, "commitOrBranchSpec");

            GitObject obj;
            using (GitObjectSafeHandle sh = Proxy.git_revparse_single(handle, objectish))
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
                return obj.DereferenceToCommit(
                    lookUpOptions.HasFlag(LookUpOptions.ThrowWhenCanNotBeDereferencedToACommit));
            }

            return obj;
        }

        internal Commit LookupCommit(string committish)
        {
            return (Commit)Lookup(committish, GitObjectType.Any,
                LookUpOptions.ThrowWhenNoGitObjectHasBeenFound |
                LookUpOptions.DereferenceResultToCommit |
                LookUpOptions.ThrowWhenCanNotBeDereferencedToACommit);
        }

        /// <summary>
        /// Probe for a git repository.
        /// <para>The lookup start from <paramref name="startingPath"/> and walk upward parent directories if nothing has been found.</para>
        /// </summary>
        /// <param name="startingPath">The base path where the lookup starts.</param>
        /// <returns>The path to the git repository.</returns>
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
        /// Clone with specified options.
        /// </summary>
        /// <param name="sourceUrl">URI for the remote repository</param>
        /// <param name="workdirPath">Local path to clone into</param>
        /// <param name="bare">True will result in a bare clone, false a full clone.</param>
        /// <param name="checkout">If true, the origin's HEAD will be checked out. This only applies
        /// to non-bare repositories.</param>
        /// <param name="onTransferProgress">Handler for network transfer and indexing progress information</param>
        /// <param name="onCheckoutProgress">Handler for checkout progress information</param>
        /// <param name="credentials">Credentials to use for user/pass authentication</param>
        /// <returns>The path to the created repository.</returns>
        public static string Clone(string sourceUrl, string workdirPath,
            bool bare = false,
            bool checkout = true,
            TransferProgressHandler onTransferProgress = null,
            CheckoutProgressHandler onCheckoutProgress = null,
            Credentials credentials = null)
        {
            CheckoutCallbacks checkoutCallbacks = CheckoutCallbacks.GenerateCheckoutCallbacks(onCheckoutProgress, null);

            var callbacks = new RemoteCallbacks(null, onTransferProgress, null, credentials);
            GitRemoteCallbacks gitCallbacks = callbacks.GenerateCallbacks();

            var cloneOpts = new GitCloneOptions
            {
                Bare = bare ? 1 : 0,
                CheckoutOpts =
                {
                    version = 1,
                    progress_cb =
                                checkoutCallbacks.CheckoutProgressCallback,
                    checkout_strategy = checkout
                                            ? CheckoutStrategy.GIT_CHECKOUT_SAFE_CREATE
                                            : CheckoutStrategy.GIT_CHECKOUT_NONE
                },
                RemoteCallbacks = gitCallbacks,
            };

            FilePath repoPath;
            using (RepositorySafeHandle repo = Proxy.git_clone(sourceUrl, workdirPath, cloneOpts))
            {
                repoPath = Proxy.git_repository_path(repo);
            }

            return repoPath.Native;
        }

        /// <summary>
        /// Find where each line of a file originated.
        /// </summary>
        /// <param name="path">Path of the file to blame.</param>
        /// <param name="options">Specifies optional parameters; if null, the defaults are used.</param>
        /// <returns>The blame for the file.</returns>
        public BlameHunkCollection Blame(string path, BlameOptions options = null)
        {
            return new BlameHunkCollection(this, Handle, path, options ?? new BlameOptions());
        }

        /// <summary>
        /// Checkout the specified <see cref="Branch"/>, reference or SHA.
        /// <para>
        ///   If the committishOrBranchSpec parameter resolves to a branch name, then the checked out HEAD will
        ///   will point to the branch. Otherwise, the HEAD will be detached, pointing at the commit sha.
        /// </para>
        /// </summary>
        /// <param name="committishOrBranchSpec">A revparse spec for the commit or branch to checkout.</param>
        /// <param name="checkoutModifiers"><see cref="CheckoutModifiers"/> controlling checkout behavior.</param>
        /// <param name="onCheckoutProgress"><see cref="CheckoutProgressHandler"/> that checkout progress is reported through.</param>
        /// <param name="checkoutNotifications"><see cref="CheckoutNotificationOptions"/> to manage checkout notifications.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public Branch Checkout(string committishOrBranchSpec, CheckoutModifiers checkoutModifiers, CheckoutProgressHandler onCheckoutProgress, CheckoutNotificationOptions checkoutNotifications)
        {
            Ensure.ArgumentNotNullOrEmptyString(committishOrBranchSpec, "committishOrBranchSpec");

            var handles = Proxy.git_revparse_ext(Handle, committishOrBranchSpec);
            if (handles == null)
            {
                Ensure.GitObjectIsNotNull(null, committishOrBranchSpec);
            }

            var objH = handles.Item1;
            var refH = handles.Item2;
            GitObject obj;
            try
            {
                if (!refH.IsInvalid)
                {
                    var reference = Reference.BuildFromPtr<Reference>(refH, this);
                    if (reference.IsLocalBranch())
                    {
                        Branch branch = Branches[reference.CanonicalName];
                        return Checkout(branch, checkoutModifiers, onCheckoutProgress, checkoutNotifications);
                    }
                }

                obj = GitObject.BuildFrom(this, Proxy.git_object_id(objH), Proxy.git_object_type(objH),
                                              PathFromRevparseSpec(committishOrBranchSpec));
            }
            finally
            {
                objH.Dispose();
                refH.Dispose();
            }

            Commit commit = obj.DereferenceToCommit(true);
            Checkout(commit.Tree, checkoutModifiers, onCheckoutProgress, checkoutNotifications, commit.Id.Sha, committishOrBranchSpec,
                committishOrBranchSpec != "HEAD");

            return Head;
        }

        /// <summary>
        /// Checkout the tip commit of the specified <see cref="Branch"/> object. If this commit is the
        /// current tip of the branch, will checkout the named branch. Otherwise, will checkout the tip commit
        /// as a detached HEAD.
        /// </summary>
        /// <param name="branch">The <see cref="Branch"/> to check out.</param>
        /// <param name="checkoutModifiers"><see cref="CheckoutModifiers"/> controlling checkout behavior.</param>
        /// <param name="onCheckoutProgress"><see cref="CheckoutProgressHandler"/> that checkout progress is reported through.</param>
        /// <param name="checkoutNotificationOptions"><see cref="CheckoutNotificationOptions"/> to manage checkout notifications.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public Branch Checkout(Branch branch, CheckoutModifiers checkoutModifiers, CheckoutProgressHandler onCheckoutProgress, CheckoutNotificationOptions checkoutNotificationOptions)
        {
            Ensure.ArgumentNotNull(branch, "branch");

            // Make sure this is not an unborn branch.
            if (branch.Tip == null)
            {
                throw new UnbornBranchException(
                    string.Format(CultureInfo.InvariantCulture,
                    "The tip of branch '{0}' is null. There's nothing to checkout.", branch.Name));
            }

            var branchIsCurrentRepositoryHead = branch.IsCurrentRepositoryHead;

            if (!branch.IsRemote && !(branch is DetachedHead) &&
                string.Equals(Refs[branch.CanonicalName].TargetIdentifier, branch.Tip.Id.Sha,
                StringComparison.OrdinalIgnoreCase))
            {
                Checkout(branch.Tip.Tree, checkoutModifiers, onCheckoutProgress, checkoutNotificationOptions, branch.CanonicalName, branch.Name, !branchIsCurrentRepositoryHead);
            }
            else
            {
                Checkout(branch.Tip.Tree, checkoutModifiers, onCheckoutProgress, checkoutNotificationOptions, branch.Tip.Id.Sha, branch.Name, !branchIsCurrentRepositoryHead);
            }

            return Head;
        }

        /// <summary>
        /// Checkout the specified <see cref="LibGit2Sharp.Commit"/>.
        /// <para>
        ///   Will detach the HEAD and make it point to this commit sha.
        /// </para>
        /// </summary>
        /// <param name="commit">The <see cref="LibGit2Sharp.Commit"/> to check out.</param>
        /// <param name="checkoutModifiers"><see cref="CheckoutModifiers"/> controlling checkout behavior.</param>
        /// <param name="onCheckoutProgress"><see cref="CheckoutProgressHandler"/> that checkout progress is reported through.</param>
        /// <param name="checkoutNotificationOptions"><see cref="CheckoutNotificationOptions"/> to manage checkout notifications.</param>
        /// <returns>The <see cref="Branch"/> that was checked out.</returns>
        public Branch Checkout(Commit commit, CheckoutModifiers checkoutModifiers, CheckoutProgressHandler onCheckoutProgress, CheckoutNotificationOptions checkoutNotificationOptions)
        {
            Checkout(commit.Tree, checkoutModifiers, onCheckoutProgress, checkoutNotificationOptions, commit.Id.Sha, commit.Id.Sha, true);

            return Head;
        }

        private void LogCheckout(string previousHeadName, ObjectId newHeadTip, string newHeadSpec)
        {
            // Compute reflog message
            string reflogMessage = string.Format("checkout: moving from {0} to {1}", previousHeadName, newHeadSpec);

            // Log checkout
            Refs.Log(Refs.Head).Append(newHeadTip, reflogMessage);
        }

        /// <summary>
        /// Internal implementation of Checkout that expects the ID of the checkout target
        /// to already be in the form of a canonical branch name or a commit ID.
        /// </summary>
        /// <param name="tree">The <see cref="Tree"/> to checkout.</param>
        /// <param name="checkoutModifiers"><see cref="CheckoutModifiers"/> controlling checkout behavior.</param>
        /// <param name="onCheckoutProgress"><see cref="CheckoutProgressHandler"/> that checkout progress is reported through.</param>
        /// <param name="checkoutNotificationOptions"><see cref="CheckoutNotificationOptions"/> to manage checkout notifications.</param>
        /// <param name="headTarget">Target for the new HEAD.</param>
        /// <param name="refLogHeadSpec">The spec which will be written as target in the reflog.</param>
        /// <param name="writeReflogEntry">Will a reflog entry be created.</param>
        private void Checkout(
            Tree tree,
            CheckoutModifiers checkoutModifiers,
            CheckoutProgressHandler onCheckoutProgress,
            CheckoutNotificationOptions checkoutNotificationOptions,
            string headTarget, string refLogHeadSpec, bool writeReflogEntry)
        {
            var previousHeadName = Info.IsHeadDetached ? Head.Tip.Sha : Head.Name;

            var opts = new CheckoutOptions
                           {
                               CheckoutModifiers = checkoutModifiers,
                               OnCheckoutProgress = onCheckoutProgress,
                               CheckoutNotificationOptions = checkoutNotificationOptions
                           };

            CheckoutTree(tree, null, opts);

            Refs.UpdateTarget("HEAD", headTarget);

            if (writeReflogEntry)
            {
                LogCheckout(previousHeadName, Head.Tip.Id, refLogHeadSpec);
            }
        }

        /// <summary>
        /// Checkout the specified tree.
        /// </summary>
        /// <param name="tree">The <see cref="Tree"/> to checkout.</param>
        /// <param name="paths">The paths to checkout.</param>
        /// <param name="opts">Collection of parameters controlling checkout behavior.</param>
        private void CheckoutTree(
            Tree tree,
            IList<string> paths,
            CheckoutOptions opts)
        {
            CheckoutNotifyHandler onCheckoutNotify = opts.CheckoutNotificationOptions != null ? opts.CheckoutNotificationOptions.CheckoutNotifyHandler : null;
            CheckoutNotifyFlags checkoutNotifyFlags = opts.CheckoutNotificationOptions != null ? opts.CheckoutNotificationOptions.NotifyFlags : default(CheckoutNotifyFlags);
            CheckoutCallbacks checkoutCallbacks = CheckoutCallbacks.GenerateCheckoutCallbacks(opts.OnCheckoutProgress, onCheckoutNotify);

            GitStrArrayIn strArray = (paths != null && paths.Count > 0) ? GitStrArrayIn.BuildFrom(ToFilePaths(paths)) : null;

            var options = new GitCheckoutOpts
            {
                version = 1,
                checkout_strategy = CheckoutStrategy.GIT_CHECKOUT_SAFE,
                progress_cb = checkoutCallbacks.CheckoutProgressCallback,
                notify_cb = checkoutCallbacks.CheckoutNotifyCallback,
                notify_flags = checkoutNotifyFlags,
                paths = strArray
            };

            try
            {
                if (opts.CheckoutModifiers.HasFlag(CheckoutModifiers.Force))
                {
                    options.checkout_strategy = CheckoutStrategy.GIT_CHECKOUT_FORCE;
                }

                Proxy.git_checkout_tree(Handle, tree.Id, ref options);
            }
            finally
            {
                options.Dispose();
            }
        }

        /// <summary>
        /// Sets the current <see cref="Head"/> to the specified commit and optionally resets the <see cref="Index"/> and
        /// the content of the working tree to match.
        /// </summary>
        /// <param name="resetOptions">Flavor of reset operation to perform.</param>
        /// <param name="commit">The target commit object.</param>
        [Obsolete("This method will be removed in the next release. Please use Reset(ResetMode, Commit) instead.")]
        public void Reset(ResetOptions resetOptions, Commit commit)
        {
            Reset((ResetMode)resetOptions, commit);
        }

        /// <summary>
        /// Sets the current <see cref="Head"/> to the specified commit and optionally resets the <see cref="Index"/> and
        /// the content of the working tree to match.
        /// </summary>
        /// <param name="resetMode">Flavor of reset operation to perform.</param>
        /// <param name="commit">The target commit object.</param>
        public void Reset(ResetMode resetMode, Commit commit)
        {
            Ensure.ArgumentNotNull(commit, "commit");

            Proxy.git_reset(handle, commit.Id, resetMode);

            Refs.Log(Refs.Head).Append(commit.Id, string.Format("reset: moving to {0}", commit.Sha));
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
        public void CheckoutPaths(string committishOrBranchSpec, IEnumerable<string> paths, CheckoutOptions checkoutOptions = null)
        {
            Ensure.ArgumentNotNullOrEmptyString(committishOrBranchSpec, "committishOrBranchSpec");
            Ensure.ArgumentNotNull(paths, "paths");

            // If there are no paths, then there is nothing to do.
            if (!paths.Any())
            {
                return;
            }

            Commit commit = LookupCommit(committishOrBranchSpec);
            CheckoutTree(commit.Tree, paths.ToList(), checkoutOptions ?? new CheckoutOptions());
        }

        /// <summary>
        /// Replaces entries in the <see cref="Repository.Index"/> with entries from the specified commit.
        /// </summary>
        /// <param name="commit">The target commit object.</param>
        /// <param name="paths">The list of paths (either files or directories) that should be considered.</param>
        /// <param name="explicitPathsOptions">
        /// If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        /// Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public void Reset(Commit commit, IEnumerable<string> paths = null, ExplicitPathsOptions explicitPathsOptions = null)
        {
            if (Info.IsBare)
            {
                throw new BareRepositoryException("Reset is not allowed in a bare repository");
            }

            Ensure.ArgumentNotNull(commit, "commit");

            var changes = Diff.Compare<TreeChanges>(commit.Tree, DiffTargets.Index, paths, explicitPathsOptions);
            Index.Reset(changes);
        }

        /// <summary>
        /// Stores the content of the <see cref="Repository.Index"/> as a new <see cref="Commit"/> into the repository.
        /// The tip of the <see cref="Repository.Head"/> will be used as the parent of this new Commit.
        /// Once the commit is created, the <see cref="Repository.Head"/> will move forward to point at it.
        /// </summary>
        /// <param name="message">The description of why a change was made to the repository.</param>
        /// <param name="author">The <see cref="Signature"/> of who made the change.</param>
        /// <param name="committer">The <see cref="Signature"/> of who added the change to the repository.</param>
        /// <param name="amendPreviousCommit">True to amend the current <see cref="Commit"/> pointed at by <see cref="Repository.Head"/>, false otherwise.</param>
        /// <returns>The generated <see cref="Commit"/>.</returns>
        public Commit Commit(string message, Signature author, Signature committer, bool amendPreviousCommit = false)
        {
            bool isHeadOrphaned = Info.IsHeadUnborn;

            if (amendPreviousCommit && isHeadOrphaned)
            {
                throw new UnbornBranchException("Can not amend anything. The Head doesn't point at any commit.");
            }

            var treeId = Proxy.git_tree_create_fromindex(Index);
            var tree = this.Lookup<Tree>(treeId);

            var parents = RetrieveParentsOfTheCommitBeingCreated(amendPreviousCommit);

            Commit result = ObjectDatabase.CreateCommit(message, author, committer, tree, parents, "HEAD");

            Proxy.git_repository_merge_cleanup(handle);

            // Insert reflog entry
            LogCommit(result, amendPreviousCommit, isHeadOrphaned, parents.Count() > 1);

            return result;
        }

        private void LogCommit(Commit commit, bool amendPreviousCommit, bool isHeadOrphaned, bool isMergeCommit)
        {
            // Compute reflog message
            string reflogMessage = "commit";
            if (isHeadOrphaned)
            {
                reflogMessage += " (initial)";
            }
            else if (amendPreviousCommit)
            {
                reflogMessage += " (amend)";
            }
            else if (isMergeCommit)
            {
                reflogMessage += " (merge)";
            }

            reflogMessage = string.Format("{0}: {1}", reflogMessage, commit.MessageShort);

            var headRef = Refs.Head;

            // in case HEAD targets a symbolic reference, log commit on the targeted direct reference
            if (headRef is SymbolicReference)
            {
                Refs.Log(headRef.ResolveToDirectReference()).Append(commit.Id, reflogMessage, commit.Committer);
            }

            // Log commit on HEAD
            Refs.Log(headRef).Append(commit.Id, reflogMessage, commit.Committer);
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
        public void RemoveUntrackedFiles()
        {
            var options = new GitCheckoutOpts
            {
                version = 1,
                checkout_strategy = CheckoutStrategy.GIT_CHECKOUT_REMOVE_UNTRACKED
                                     | CheckoutStrategy.GIT_CHECKOUT_ALLOW_CONFLICTS,
            };

            try
            {
                Proxy.git_checkout_index(Handle, new NullGitObjectSafeHandle(), ref options);
            }
            finally
            {
                options.Dispose();
            }
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
        /// Gets the current LibGit2Sharp version.
        /// <para>
        ///   The format of the version number is as follows:
        ///   <para>Major.Minor.Patch-LibGit2Sharp_abbrev_hash-libgit2_abbrev_hash (x86|amd64)</para>
        /// </para>
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
        /// Gets the references to the tips that are currently being merged.
        /// </summary>
        public IEnumerable<MergeHead> MergeHeads
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

        internal bool PathStartsWith(string path, string value)
        {
            return pathCase.Value.StartsWith(path, value);
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
