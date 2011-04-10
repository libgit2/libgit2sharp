using System;
using LibGit2Sharp.Core;

namespace LibGit2Sharp
{
    /// <summary>
    ///   A Repository is the primary interface into a git repository
    /// </summary>
    public class Repository : IDisposable
    {
        private const char posixDirectorySeparatorChar = '/';
        private readonly BranchCollection branches;
        private readonly CommitCollection commits;
        private readonly RepositorySafeHandle handle;
        private readonly ReferenceCollection refs;
        private readonly TagCollection tags;

        /// <summary>
        ///   Initializes a new instance of the <see cref = "Repository" /> class.
        /// 
        ///   Exceptions:
        ///   ArgumentException
        ///   ArgumentNullException
        ///   TODO: ApplicationException is thrown for all git errors right now
        /// </summary>
        /// <param name = "path">The path to the git repository to open.</param>
        public Repository(string path)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            string posixPath = path.Replace(System.IO.Path.DirectorySeparatorChar, posixDirectorySeparatorChar);

            var res = NativeMethods.git_repository_open(out handle, posixPath);
            Ensure.Success(res);

            string normalizedPath = NativeMethods.git_repository_path(handle);
            string normalizedWorkDir = NativeMethods.git_repository_workdir(handle);

            Path = normalizedPath.Replace(posixDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);
            WorkingDirectory = (normalizedWorkDir == null) ? null : normalizedWorkDir.Replace(posixDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar);

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
            get { return commits.StartingAt(Refs.Head); }
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
        ///   Gets the normalized path to the git repository.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        ///   Gets the normalized path to the working directory.
        /// <para>
        ///   Is the repository is bare, null is returned.
        /// </para>
        /// </summary>
        public string WorkingDirectory { get; private set; }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (handle != null && !handle.IsInvalid)
            {
                handle.Dispose();
            }
        }

        /// <summary>
        ///   Tells if the specified <see cref = "GitOid" /> exists in the repository.
        /// 
        ///   Exceptions:
        ///   ArgumentNullException
        /// </summary>
        /// <param name = "id">The id.</param>
        /// <returns></returns>
        public bool HasObject(ObjectId id)
        {
            Ensure.ArgumentNotNull(id, "id");

            var odb = NativeMethods.git_repository_database(handle);
            var oid = id.Oid;
            return NativeMethods.git_odb_exists(odb, ref oid);
        }

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

            return HasObject(new ObjectId(sha));
        }

        /// <summary>
        ///   Init a repo at the specified path
        /// </summary>
        /// <param name = "path">The path.</param>
        /// <param name = "bare"></param>
        /// <returns></returns>
        public static string Init(string path, bool bare = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(path, "path");

            var posixPath = path.Replace(System.IO.Path.DirectorySeparatorChar, posixDirectorySeparatorChar);

            RepositorySafeHandle repo;
            var res = NativeMethods.git_repository_init(out repo, posixPath, bare);
            Ensure.Success(res);
            repo.Dispose();

            return path;
        }

        /// <summary>
        ///   Try to lookup an object by its <see cref = "ObjectId" /> and <see cref="GitObjectType"/>. If no matching object is found, null will be returned.
        /// </summary>
        /// <param name = "id">The id to lookup.</param>
        /// <param name = "type"></param>
        /// <returns>the <see cref = "GitObject" /> or null if it was not found.</returns>
        public GitObject Lookup(ObjectId id, GitObjectType type = GitObjectType.Any)
        {
            Ensure.ArgumentNotNull(id, "id");

            var oid = id.Oid;
            IntPtr obj;
            var res = NativeMethods.git_object_lookup(out obj, handle, ref oid, type);
            if (res == (int) GitErrorCode.GIT_ENOTFOUND || res == (int) GitErrorCode.GIT_EINVALIDTYPE)
            {
                return null;
            }

            Ensure.Success(res);

            GitObject gitObject = GitObject.CreateFromPtr(obj, id, this);

            //TODO: Uncommenting the code below makes test CanReadCommitWithMultipleParents() fail. To be investigated.
            //NativeMethods.git_object_close(obj);

            return gitObject;
        }
    }
}