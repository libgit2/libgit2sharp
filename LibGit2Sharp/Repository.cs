using System;
using System.IO;
using System.Text;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Repository is the primary interface into a git repository
    /// </summary>
    public class Repository : IDisposable
    {
        private readonly BranchCollection branches;
        private readonly CommitCollection commits;
        private Configuration config;
        private readonly RepositorySafeHandle handle;
        private readonly Index index;
        private readonly ReferenceCollection refs;
        private RemoteCollection remotes;
        private readonly TagCollection tags;
        private readonly Lazy<RepositoryInformation> info;
        private readonly bool isBare;

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Repository" /> class.
        ///     <para>For a standard repository, <paramref name="path"/> should point to the ".git" folder. For a bare repository, <paramref name="path"/> should directly point to the repository folder.</para>
        /// </summary>
        /// <param name = "path">The path to the git repository to open.</param>
        public Repository(string path)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            var res = NativeMethods.git_repository_open(out handle, PosixPathHelper.ToPosix(path));
            Ensure.Success(res);

            isBare = NativeMethods.git_repository_is_bare(handle);

            if (!isBare)
            {
                index = new Index(this);
            }

            commits = new CommitCollection(this);
            refs = new ReferenceCollection(this);
            branches = new BranchCollection(this);
            tags = new TagCollection(this);
            info = new Lazy<RepositoryInformation>(() => new RepositoryInformation(this, isBare));
        }

        internal RepositorySafeHandle Handle
        {
            get { return handle; }
        }

        /// <summary>
        ///   Shortcut to return the reference to HEAD
        /// </summary>
        /// <returns></returns>
        public Branch Head
        {
            get
            {
                Reference headRef = Refs["HEAD"];

                if (Info.IsEmpty)
                {
                    return new Branch(headRef.TargetIdentifier, null, this);
                }

                return Refs.Resolve<Branch>(headRef.ResolveToDirectReference().CanonicalName);
            }
        }

        /// <summary>
        ///   Provides access to the configuration settings for this repository.
        /// </summary>
        public Configuration Config
        {
            get { return config ?? (config = new Configuration(this)); }
        }

        /// <summary>
        ///   Gets the index.
        /// </summary>
        public Index Index
        {
            get { return index; }
        }

        /// <summary>
        ///   Lookup and enumerate references in the repository.
        /// </summary>
        public ReferenceCollection Refs
        {
            get { return refs; }
        }

        public RemoteCollection Remotes
        {
            get { return remotes ?? (remotes = new RemoteCollection(this)); }
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
        public RepositoryInformation Info { get { return info.Value; } }

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
        protected virtual void Dispose(bool disposing)
        {
            if (handle != null && !handle.IsInvalid)
            {
                handle.Dispose();
            }

            if (index != null)
            {
                index.Dispose();
            }
        }

        #endregion

        /// <summary>
        ///   Tells if the specified sha exists in the repository.
        ///
        ///   Exceptions:
        ///   ArgumentException
        ///   ArgumentNullException
        /// </summary>
        /// <param name = "sha">The sha.</param>
        /// <returns></returns>
        public bool HasObject(string sha)   //TODO: To be removed from front facing API (maybe should we create an Repository.Advanced to hold those kind of functions)?
        {
            var id = new ObjectId(sha);

            var odb = NativeMethods.git_repository_database(handle);
            var oid = id.Oid;
            return NativeMethods.git_odb_exists(odb, ref oid);
        }

        /// <summary>
        ///   Init a repo at the specified <paramref name="path"/>.
        /// </summary>
        /// <param name = "path">The path to the working folder when initializing a standard ".git" repository. Otherwise, when initializing a bare repository, the path to the expected location of this later.</param>
        /// <param name = "isBare">true to initialize a bare repository. False otherwise, to initialize a standard ".git" repository.</param>
        /// <returns>Path the git repository.</returns>
        public static string Init(string path, bool isBare = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            RepositorySafeHandle repo;
            var res = NativeMethods.git_repository_init(out repo, PosixPathHelper.ToPosix(path), isBare);
            Ensure.Success(res);

            string normalizedPath = NativeMethods.git_repository_path(repo, GitRepositoryPathId.GIT_REPO_PATH).MarshallAsString();
            repo.Dispose();

            string nativePath = PosixPathHelper.ToNative(normalizedPath);

            // TODO: To be removed once it's being dealt with by libgit2
            // libgit2 doesn't currently create the git config file, so create a minimal one if we can't find it
            // See https://github.com/libgit2/libgit2sharp/issues/56 for details
            string configFile = Path.Combine(nativePath, "config");
            if (!File.Exists(configFile))
            {
                File.WriteAllText(configFile, "[core]\n\trepositoryformatversion = 0\n");
            }

            return nativePath;
        }

        /// <summary>
        ///   Try to lookup an object by its <see cref = "ObjectId" /> and <see cref = "GitObjectType" />. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name = "id">The id to lookup.</param>
        /// <param name = "type">The kind of GitObject being looked up</param>
        /// <returns>The <see cref = "GitObject" /> or null if it was not found.</returns>
        public GitObject Lookup(ObjectId id, GitObjectType type = GitObjectType.Any)
        {
            Ensure.ArgumentNotNull(id, "id");

            var oid = id.Oid;
            IntPtr obj;
            int res;

            if (id is AbbreviatedObjectId)
            {
                res = NativeMethods.git_object_lookup_prefix(out obj, handle, ref oid, (uint)((AbbreviatedObjectId)id).Length, type);
            }
            else
            {
                res = NativeMethods.git_object_lookup(out obj, handle, ref oid, type);
            }

            if (res == (int)GitErrorCode.GIT_ENOTFOUND || res == (int)GitErrorCode.GIT_EINVALIDTYPE)
            {
                return null;
            }

            Ensure.Success(res);

            if (id is AbbreviatedObjectId)
            {
                id = GitObject.ObjectIdOf(obj);
            }

            return GitObject.CreateFromPtr(obj, id, this);
        }

        /// <summary>
        ///   Try to lookup an object by its sha or a reference canonical name and <see cref="GitObjectType"/>. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name = "shaOrReferenceName">The sha or reference canonical name to lookup.</param>
        /// <param name = "type">The kind of <see cref="GitObject"/> being looked up</param>
        /// <returns>The <see cref = "GitObject" /> or null if it was not found.</returns>
        public GitObject Lookup(string shaOrReferenceName, GitObjectType type = GitObjectType.Any)
        {
            ObjectId id;

            if (ObjectId.TryParse(shaOrReferenceName, out id))
            {
                return Lookup(id, type);
            }

            var reference = Refs[shaOrReferenceName];

            if (!IsReferencePeelable(reference))
            {
                return null;
            }

            return Lookup(reference.ResolveToDirectReference().TargetIdentifier, type);
        }

        private static bool IsReferencePeelable(Reference reference)
        {
            return reference != null && ((reference is DirectReference) || (reference is SymbolicReference && ((SymbolicReference)reference).Target != null));
        }

        /// <summary>
        /// Probe for a git repository.
        /// <para>The lookup start from <paramref name="startingPath"/> and walk upward parent directories if nothing has been found.</para>
        /// </summary>
        /// <param name="startingPath">The base path where the lookup starts.</param>
        /// <returns>The path to the git repository.</returns>
        public static string Discover(string startingPath)
        {
            var buffer = new StringBuilder(4096);

            int result = NativeMethods.git_repository_discover(buffer, buffer.Capacity, PosixPathHelper.ToPosix(startingPath), false, null);
            Ensure.Success(result);

            return PosixPathHelper.ToNative(buffer.ToString());
        }
    }
}