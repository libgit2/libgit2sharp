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

            handle = Proxy.git_repository_open(path);
            RegisterForCleanup(handle);

            bool isBare = Proxy.git_repository_is_bare(handle);

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
                    Proxy.git_repository_set_workdir(handle, options.WorkingDirectoryPath);
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
                    throw new BareRepositoryException("Index is not available in a bare repository.");
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

            using (RepositorySafeHandle repo = Proxy.git_repository_init(path, isBare))
            {
                FilePath repoPath = Proxy.git_repository_path(repo);
                return new Repository(repoPath.Native);
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

            GitObject obj;
            using (GitObjectSafeHandle sh = Proxy.git_revparse_single(handle, objectish))
            {
                if (sh == null)
                {
                    if (lookUpOptions.Has(LookUpOptions.ThrowWhenNoGitObjectHasBeenFound))
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
            FilePath discoveredPath = Proxy.git_repository_discover(startingPath);

            if (discoveredPath == null)
            {
                return null;
            }

            return discoveredPath.Native;
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
        ///   Sets the current <see cref = "Head" /> to the specified commit and optionally resets the <see cref = "Index" /> and
        ///   the content of the working tree to match.
        /// </summary>
        /// <param name = "resetOptions">Flavor of reset operation to perform.</param>
        /// <param name = "commitish">A revparse spec for the target commit object.</param>
        public void Reset(ResetOptions resetOptions, string commitish = "HEAD")
        {
            Ensure.ArgumentNotNullOrEmptyString(commitish, "commitish");

            GitObject obj = Lookup(commitish, GitObjectType.Any, LookUpOptions.ThrowWhenNoGitObjectHasBeenFound);

            Proxy.git_reset(handle, obj.Id, resetOptions);
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

            GitOid treeOid = Proxy.git_tree_create_fromindex(Index);
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
    }
}
