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
    ///   A Repository is the primary interface into a git repository
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Repository : IRepository
    {
        private readonly bool isBare;
        private readonly BranchCollection branches;
        private readonly CommitLog commits;
        private readonly Lazy<Configuration> config;
        private readonly RepositorySafeHandle handle;
        private readonly Index index;
        private readonly ConflictCollection conflicts;
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
                    conflicts = new ConflictCollection(this);
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
        ///   Check if parameter <paramref name="path"/> leads to a valid git repository.
        /// </summary>
        /// <param name = "path">
        ///   The path to the git repository to check, can be either the path to the git directory (for non-bare repositories this
        ///   would be the ".git" folder inside the working directory) or the path to the working directory.
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
        ///   Shortcut to return the branch pointed to by HEAD
        /// </summary>
        /// <returns></returns>
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
                if (isBare)
                {
                    throw new BareRepositoryException("Index is not available in a bare repository.");
                }

                return index;
            }
        }

        /// <summary>
        ///  Gets the conflicts that exist.
        /// </summary>
        public ConflictCollection Conflicts
        {
            get
            {
                if (isBare)
                {
                    throw new BareRepositoryException("Conflicts are not available in a bare repository.");
                }

                return conflicts;
            }
        }

        /// <summary>
        ///   Manipulate the currently ignored files.
        /// </summary>
        public Ignore Ignore
        {
            get
            {
                return ignore;
            }
        }

        /// <summary>
        ///   Provides access to network functionality for a repository.
        /// </summary>
        public Network Network
        {
            get
            {
                return network.Value;
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

        ///<summary>
        /// Lookup and enumerate stashes in the repository.
        ///</summary>
        public StashCollection Stashes
        {
            get { return stashes; }
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

        /// <summary>
        ///   Submodules in the repository.
        /// </summary>
        public SubmoduleCollection Submodules
        {
            get { return submodules; }
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
            CleanupDisposableDependencies();
        }

        #endregion

        /// <summary>
        ///   Initialize a repository at the specified <paramref name = "path" />.
        /// </summary>
        /// <param name = "path">The path to the working folder when initializing a standard ".git" repository. Otherwise, when initializing a bare repository, the path to the expected location of this later.</param>
        /// <param name = "isBare">true to initialize a bare repository. False otherwise, to initialize a standard ".git" repository.</param>
        /// <param name="options">Overrides to the way a repository is opened.</param>
        /// <returns> a new instance of the <see cref = "Repository" /> class. The client code is responsible for calling <see cref = "Dispose()" /> on this instance.</returns>
        public static Repository Init(string path, bool isBare = false, RepositoryOptions options = null)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            using (RepositorySafeHandle repo = Proxy.git_repository_init(path, isBare))
            {
                FilePath repoPath = Proxy.git_repository_path(repo);
                return new Repository(repoPath.Native, options);
            }
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

        /// <summary>
        ///   Lookup a commit by its SHA or name, or throw if a commit is not found.
        /// </summary>
        /// <param name="committish">A revparse spec for the commit.</param>
        /// <returns>The commit.</returns>
        internal Commit LookupCommit(string committish)
        {
            return (Commit)Lookup(committish, GitObjectType.Any, LookUpOptions.ThrowWhenNoGitObjectHasBeenFound | LookUpOptions.DereferenceResultToCommit | LookUpOptions.ThrowWhenCanNotBeDereferencedToACommit);
        }

        /// <summary>
        ///   Probe for a git repository.
        ///   <para>The lookup start from <paramref name = "startingPath" /> and walk upward parent directories if nothing has been found.</para>
        /// </summary>
        /// <param name = "startingPath">The base path where the lookup starts.</param>
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
        /// <param name="options">Overrides to the way a repository is opened.</param>
        /// <param name="credentials">Credentials to use for user/pass authentication</param>
        /// <returns></returns>
        public static Repository Clone(string sourceUrl, string workdirPath,
            bool bare = false,
            bool checkout = true,
            TransferProgressHandler onTransferProgress = null,
            CheckoutProgressHandler onCheckoutProgress = null,
            RepositoryOptions options = null,
            Credentials credentials = null)
        {
            var cloneOpts = new GitCloneOptions
                {
                    Bare = bare ? 1 : 0,
                    TransferProgressCallback = TransferCallbacks.GenerateCallback(onTransferProgress),
                    CheckoutOpts =
                        {
                            version = 1,
                            progress_cb =
                                CheckoutCallbacks.GenerateCheckoutCallbacks(onCheckoutProgress),
                            checkout_strategy = checkout
                                                    ? CheckoutStrategy.GIT_CHECKOUT_SAFE_CREATE
                                                    : CheckoutStrategy.GIT_CHECKOUT_NONE
                        },
                };

            if (credentials != null)
            {
                cloneOpts.CredAcquireCallback =
                    (out IntPtr cred, IntPtr url, IntPtr username_from_url, uint types, IntPtr payload) =>
                    NativeMethods.git_cred_userpass_plaintext_new(out cred, credentials.Username, credentials.Password);
            }

            using(Proxy.git_clone(sourceUrl, workdirPath, cloneOpts)) {}

            // To be safe, make sure the credential callback is kept until
            // alive until at least this point.
            GC.KeepAlive(cloneOpts.CredAcquireCallback);

            return new Repository(workdirPath, options);
        }

        /// <summary>
        ///   Checkout the specified <see cref = "Branch" />, reference or SHA.
        /// </summary>
        /// <param name = "committishOrBranchSpec">A revparse spec for the commit or branch to checkout.</param>
        /// <param name="checkoutOptions"><see cref = "CheckoutOptions" /> controlling checkout behavior.</param>
        /// <param name="onCheckoutProgress"><see cref = "CheckoutProgressHandler" /> that checkout progress is reported through.</param>
        /// <returns>The <see cref = "Branch" /> that was checked out.</returns>
        public Branch Checkout(string committishOrBranchSpec, CheckoutOptions checkoutOptions, CheckoutProgressHandler onCheckoutProgress)
        {
            Ensure.ArgumentNotNullOrEmptyString(committishOrBranchSpec, "committishOrBranchSpec");

            var branch = Branches[committishOrBranchSpec];

            if (branch != null)
            {
                return Checkout(branch, checkoutOptions, onCheckoutProgress);
            }

            Commit commit = LookupCommit(committishOrBranchSpec);
            CheckoutTree(commit.Tree, checkoutOptions, onCheckoutProgress);

            // Update HEAD.
            Refs.UpdateTarget("HEAD", commit.Id.Sha);

            return Head;
        }

        /// <summary>
        ///   Checkout the tip commit of the specified <see cref = "Branch" /> object. If this commit is the
        ///   current tip of the branch, will checkout the named branch. Otherwise, will checkout the tip commit
        ///   as a detached HEAD.
        /// </summary>
        /// <param name="branch">The <see cref = "Branch" /> to check out. </param>
        /// <param name="checkoutOptions"><see cref = "CheckoutOptions" /> controlling checkout behavior.</param>
        /// <param name="onCheckoutProgress"><see cref = "CheckoutProgressHandler" /> that checkout progress is reported through.</param>
        /// <returns>The <see cref = "Branch" /> that was checked out.</returns>
        public Branch Checkout(Branch branch, CheckoutOptions checkoutOptions, CheckoutProgressHandler onCheckoutProgress)
        {
            Ensure.ArgumentNotNull(branch, "branch");

            // Make sure this is not an unborn branch.
            if (branch.Tip == null)
            {
                throw new OrphanedHeadException(
                    string.Format(CultureInfo.InvariantCulture,
                    "The tip of branch '{0}' is null. There's nothing to checkout.", branch.Name));
            }

            CheckoutTree(branch.Tip.Tree, checkoutOptions, onCheckoutProgress);

            // Update HEAD.
            if (!branch.IsRemote &&
                string.Equals(Refs[branch.CanonicalName].TargetIdentifier, branch.Tip.Id.Sha,
                StringComparison.OrdinalIgnoreCase))
            {
                Refs.UpdateTarget("HEAD", branch.CanonicalName);
            }
            else
            {
                Refs.UpdateTarget("HEAD", branch.Tip.Id.Sha);
            }

            return Head;
        }

        /// <summary>
        ///   Internal implementation of Checkout that expects the ID of the checkout target
        ///   to already be in the form of a canonical branch name or a commit ID.
        /// </summary>
        /// <param name="tree">The <see cref="Tree"/> to checkout.</param>
        /// <param name="checkoutOptions"><see cref = "CheckoutOptions" /> controlling checkout behavior.</param>
        /// <param name="onCheckoutProgress"><see cref = "CheckoutProgressHandler" /> that checkout progress is reported through.</param>
        private void CheckoutTree(Tree tree, CheckoutOptions checkoutOptions, CheckoutProgressHandler onCheckoutProgress)
        {
            GitCheckoutOpts options = new GitCheckoutOpts
            {
                version = 1,
                checkout_strategy = CheckoutStrategy.GIT_CHECKOUT_SAFE,
                progress_cb = CheckoutCallbacks.GenerateCheckoutCallbacks(onCheckoutProgress)
            };

            if (checkoutOptions.HasFlag(CheckoutOptions.Force))
            {
                options.checkout_strategy = CheckoutStrategy.GIT_CHECKOUT_FORCE;
            }

            Proxy.git_checkout_tree(this.Handle, tree.Id, ref options);
        }

        /// <summary>
        ///   Sets the current <see cref = "Head" /> to the specified commit and optionally resets the <see cref = "Index" /> and
        ///   the content of the working tree to match.
        /// </summary>
        /// <param name = "resetOptions">Flavor of reset operation to perform.</param>
        /// <param name = "commit">The target commit object.</param>
        public void Reset(ResetOptions resetOptions, Commit commit)
        {
            Ensure.ArgumentNotNull(commit, "commit");

            Proxy.git_reset(handle, commit.Id, resetOptions);
        }

        /// <summary>
        ///   Replaces entries in the <see cref="Repository.Index"/> with entries from the specified commit.
        /// </summary>
        /// <param name = "commit">The target commit object.</param>
        /// <param name = "paths">The list of paths (either files or directories) that should be considered.</param>
        /// <param name = "explicitPathsOptions">
        ///   If set, the passed <paramref name="paths"/> will be treated as explicit paths.
        ///   Use these options to determine how unmatched explicit paths should be handled.
        /// </param>
        public void Reset(Commit commit, IEnumerable<string> paths = null, ExplicitPathsOptions explicitPathsOptions = null)
        {
            if (Info.IsBare)
            {
                throw new BareRepositoryException("Reset is not allowed in a bare repository");
            }

            Ensure.ArgumentNotNull(commit, "commit");

            TreeChanges changes = Diff.Compare(commit.Tree, DiffTargets.Index, paths, explicitPathsOptions);
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
            bool isHeadOrphaned = Info.IsHeadOrphaned;

            if (amendPreviousCommit && isHeadOrphaned)
            {
                throw new OrphanedHeadException("Can not amend anything. The Head doesn't point at any commit.");
            }

            var treeId = Proxy.git_tree_create_fromindex(Index);
            var tree = this.Lookup<Tree>(treeId);

            var parents = RetrieveParentsOfTheCommitBeingCreated(amendPreviousCommit);

            Commit result = ObjectDatabase.CreateCommit(message, author, committer, tree, parents, "HEAD");

            Proxy.git_repository_merge_cleanup(handle);

            // Insert reflog entry
            LogCommit(result, amendPreviousCommit, isHeadOrphaned);

            return result;
        }

        private void LogCommit(Commit commit, bool amendPreviousCommit, bool isHeadOrphaned)
        {
            // Compute reflog message
            string reflogMessage = "commit";
            if (isHeadOrphaned)
            {
                reflogMessage += " (initial)";
            }
            else if(amendPreviousCommit)
            {
                reflogMessage += " (amend)";
            }

            reflogMessage = string.Format("{0}: {1}", reflogMessage, commit.Message);

            var headRef = Refs.Head;

            // in case HEAD targets a symbolic reference, log commit on the targeted direct reference
            if(headRef is SymbolicReference)
            {
                Refs.Log(headRef.ResolveToDirectReference()).Append(commit.Id, commit.Committer, reflogMessage);
            }

            // Log commit on HEAD
            Refs.Log(headRef).Append(commit.Id, commit.Committer, reflogMessage);
        }

        private IEnumerable<Commit> RetrieveParentsOfTheCommitBeingCreated(bool amendPreviousCommit)
        {
            if (amendPreviousCommit)
            {
                return Head.Tip.Parents;
            }

            if (Info.IsHeadOrphaned)
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
        public virtual void RemoveUntrackedFiles()
        {
            var options = new GitCheckoutOpts
            {
                version = 1,
                checkout_strategy = CheckoutStrategy.GIT_CHECKOUT_REMOVE_UNTRACKED
                                     | CheckoutStrategy.GIT_CHECKOUT_ALLOW_CONFLICTS,
            };

            Proxy.git_checkout_index(Handle, new NullGitObjectSafeHandle(), ref options);
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
        ///   Gets the references to the tips that are currently being merged.
        /// </summary>
        public virtual IEnumerable<MergeHead> MergeHeads
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
