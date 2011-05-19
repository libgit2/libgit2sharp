using System;
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
        private readonly RepositorySafeHandle handle;
        private readonly Index index;
        private readonly ReferenceCollection refs;
        private readonly TagCollection tags;

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

            string normalizedPath = NativeMethods.git_repository_path(handle);
            string normalizedWorkDir = NativeMethods.git_repository_workdir(handle);

            Info = new RepositoryInformation(this, normalizedPath, normalizedWorkDir, normalizedWorkDir == null);

            if (!Info.IsBare)
                index = new Index(this);

            commits = new CommitCollection(this);
            refs = new ReferenceCollection(this);
            branches = new BranchCollection(this);
            tags = new TagCollection(this);
        }

        internal RepositorySafeHandle Handle
        {
            get { return handle; }
        }
        /// <summary>
        ///   Shortcut to return the reference to HEAD
        /// </summary>
        /// <returns></returns>
        
        public Reference Head
        {
            get { return Refs["HEAD"]; }
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

        /// <summary>
        ///   Lookup and enumerate commits in the repository. 
        ///   Iterating this collection directly starts walking from the HEAD.
        /// </summary>
        public CommitCollection Commits
        {
            get { return commits.StartingAt(Head); }
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
        public RepositoryInformation Info { get; set; }

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
        public bool HasObject(string sha)
        {
            Ensure.ArgumentNotNullOrEmptyString(sha, "sha");

            var id = new ObjectId(sha);

            var odb = NativeMethods.git_repository_database(handle);
            var oid = id.Oid;
            return NativeMethods.git_odb_exists(odb, ref oid);
        }

        /// <summary>
        ///   Init a repo at the specified <paramref name="path"/>.
        /// </summary>
        /// <param name = "path">The path to the working folder when initializing a standard ".git" repository. Otherwise, when initializing a bare repository, the path to the expected location of this later.</param>
        /// <param name = "bare">true to initialize a bare repository. False otherwise, to initialize a standard ".git" repository.</param>
        /// <returns>Path the git repository.</returns>
        public static string Init(string path, bool bare = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            RepositorySafeHandle repo;
            var res = NativeMethods.git_repository_init(out repo, PosixPathHelper.ToPosix(path), bare);
            Ensure.Success(res);

            string normalizedPath = NativeMethods.git_repository_path(repo);
            repo.Dispose();

            return PosixPathHelper.ToNative(normalizedPath);
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
            var res = NativeMethods.git_object_lookup(out obj, handle, ref oid, type);
            if (res == (int)GitErrorCode.GIT_ENOTFOUND || res == (int)GitErrorCode.GIT_EINVALIDTYPE)
            {
                return null;
            }

            Ensure.Success(res);

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
            ObjectId id = ObjectId.CreateFromMaybeSha(shaOrReferenceName);
            if (id != null)
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
            return reference != null && ((reference is DirectReference) ||(reference is SymbolicReference && ((SymbolicReference)reference).Target != null));
        }
    }
}