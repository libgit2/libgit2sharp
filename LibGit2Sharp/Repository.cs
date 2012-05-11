using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Compat;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Repository is the primary interface into a git repository
    /// </summary>
    public class Repository : IDisposable
    {
        private readonly BranchCollection branches;
        private readonly CommitCollection commits;
        private readonly Lazy<Configuration> config;
        private readonly RepositorySafeHandle handle;
        private readonly Index index;
        private readonly ReferenceCollection refs;
        private readonly Lazy<RemoteCollection> remotes;
        private readonly TagCollection tags;
        private readonly Lazy<RepositoryInformation> info;
        private readonly Diff diff;
        private readonly bool isBare;
        private readonly Lazy<ObjectDatabase> odb;
        private readonly Stack<SafeHandleBase> handlesToCleanup = new Stack<SafeHandleBase>();
        private static readonly Lazy<string> versionRetriever = new Lazy<string>(RetrieveVersion);

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Repository" /> class.
        ///   <para>For a standard repository, <paramref name = "path" /> should either point to the ".git" folder or to the working directory. For a bare repository, <paramref name = "path" /> should directly point to the repository folder.</para>
        /// </summary>
        /// <param name = "path">
        ///   The path to the git repository to open, can be either the path to the git directory (for non-bare repositories this 
        ///   would be the ".git" folder inside the working directory) or the path to the working directory.
        /// </param>
        public Repository(string path)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            int res = NativeMethods.git_repository_open(out handle, path);
            Ensure.Success(res);

            RegisterForCleanup(handle);

            isBare = NativeMethods.RepositoryStateChecker(handle, NativeMethods.git_repository_is_bare);

            if (!isBare)
            {
                index = new Index(this);
            }

            commits = new CommitCollection(this);
            refs = new ReferenceCollection(this);
            branches = new BranchCollection(this);
            tags = new TagCollection(this);
            info = new Lazy<RepositoryInformation>(() => new RepositoryInformation(this, isBare));
            config = new Lazy<Configuration>(() => new Configuration(this));
            remotes = new Lazy<RemoteCollection>(() => new RemoteCollection(this));
            odb = new Lazy<ObjectDatabase>(() => new ObjectDatabase(this));
            diff = new Diff(this);
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
                    throw new LibGit2Exception("Index is not available in a bare repository.");
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
        public IQueryableCommitCollection Commits
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
            while (handlesToCleanup.Count > 0)
            {
                handlesToCleanup.Pop().SafeDispose();
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
                    res = NativeMethods.git_object_lookup_prefix(out obj, handle, ref oid, (uint)((AbbreviatedObjectId)id).Length, GitObjectType.Any);
                }
                else
                {
                    res = NativeMethods.git_object_lookup(out obj, handle, ref oid, GitObjectType.Any);
                }

                if (res == (int)GitErrorCode.GIT_ENOTFOUND)
                {
                    return null;
                }

                Ensure.Success(res);

                if (type != GitObjectType.Any && NativeMethods.git_object_type(obj) != type)
                {
                    return null;
                }

                if (id is AbbreviatedObjectId)
                {
                    id = GitObject.ObjectIdOf(obj);
                }

                return GitObject.CreateFromPtr(obj, id, this, knownPath);
            }
            finally
            {
                obj.SafeDispose();
            }
        }

        /// <summary>
        ///   Try to lookup an object by its sha or a reference canonical name and <see cref = "GitObjectType" />. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name = "shaOrReferenceName">The sha or reference canonical name to lookup.</param>
        /// <param name = "type">The kind of <see cref = "GitObject" /> being looked up</param>
        /// <returns>The <see cref = "GitObject" /> or null if it was not found.</returns>
        public GitObject Lookup(string shaOrReferenceName, GitObjectType type = GitObjectType.Any)
        {
            return Lookup(shaOrReferenceName, type, LookUpOptions.None);
        }

        internal GitObject Lookup(string shaOrReferenceName, GitObjectType type, LookUpOptions lookUpOptions)
        {
            ObjectId id;

            Reference reference = Refs[shaOrReferenceName];
            if (reference != null)
            {
                id = reference.PeelToTargetObjectId();
            }
            else
            {
                ObjectId.TryParse(shaOrReferenceName, out id);
            }

            if (id == null)
            {
                if (lookUpOptions.Has(LookUpOptions.ThrowWhenNoGitObjectHasBeenFound))
                {
                    Ensure.GitObjectIsNotNull(null, shaOrReferenceName);
                }

                return null;
            }

            GitObject gitObj = Lookup(id, type);

            if (lookUpOptions.Has(LookUpOptions.ThrowWhenNoGitObjectHasBeenFound))
            {
                Ensure.GitObjectIsNotNull(gitObj, shaOrReferenceName);
            }

            if (!lookUpOptions.Has(LookUpOptions.DereferenceResultToCommit))
            {
                return gitObj;
            }

            return gitObj.DereferenceToCommit(shaOrReferenceName, lookUpOptions.Has(LookUpOptions.ThrowWhenCanNotBeDereferencedToACommit));
        }

        /// <summary>
        ///   Lookup a commit by its SHA or name, or throw if a commit is not found.
        /// </summary>
        /// <param name="shaOrReferenceName">The SHA or name of the commit.</param>
        /// <returns>The commit.</returns>
        internal Commit LookupCommit(string shaOrReferenceName)
        {
            return (Commit)Lookup(shaOrReferenceName, GitObjectType.Any, LookUpOptions.ThrowWhenNoGitObjectHasBeenFound | LookUpOptions.DereferenceResultToCommit | LookUpOptions.ThrowWhenCanNotBeDereferencedToACommit);
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

            if ((GitErrorCode)result == GitErrorCode.GIT_ENOTFOUND)
            {
                return null;
            }

            Ensure.Success(result);

            FilePath discoveredPath = Utf8Marshaler.Utf8FromBuffer(buffer);

            return discoveredPath.Native;
        }

        /// <summary>
        ///   Checkout the specified branch, reference or SHA.
        /// </summary>
        /// <param name = "shaOrReferenceName">The sha of the commit, a canonical reference name or the name of the branch to checkout.</param>
        /// <returns>The new HEAD.</returns>
        public Branch Checkout(string shaOrReferenceName)
        {
            // TODO: This does not yet checkout (write) the working directory

            var branch = Branches[shaOrReferenceName];

            if (branch != null)
            {
                return Checkout(branch);
            }

            var commitId = LookupCommit(shaOrReferenceName).Id;
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
        /// <param name = "shaOrReferenceName">The sha or reference canonical name of the target commit object.</param>
        public void Reset(ResetOptions resetOptions, string shaOrReferenceName)
        {
            Ensure.ArgumentNotNullOrEmptyString(shaOrReferenceName, "shaOrReferenceName");

            if (resetOptions.Has(ResetOptions.Mixed) && Info.IsBare)
            {
                throw new LibGit2Exception("Mixed reset is not allowed in a bare repository");
            }

            var commit = LookupCommit(shaOrReferenceName);

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

        internal void RegisterForCleanup(SafeHandleBase handleToCleanup)
        {
            handlesToCleanup.Push(handleToCleanup);
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

            return string.Format("{0}-{1}-{2} ({3})", 
                version.ToString(3),
                libgit2sharpHash.Substring(0, 7),
                libgit2Hash.Substring(0,7),
                NativeMethods.ProcessorArchitecture
                );
        }

        private static string ReadContentFromResource(Assembly assembly, string partialResourceName)
        {
            using (var sr = new StreamReader(assembly.GetManifestResourceStream(string.Format("LibGit2Sharp.{0}", partialResourceName))))
            {
                return sr.ReadLine();
            }
        }
    }
}
