using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;
using LibGit2Sharp.Handlers;

namespace LibGit2Sharp
{
    /// <summary>
    /// Provides access to network functionality for a repository.
    /// </summary>
    public class Network
    {
        private readonly Repository repository;
        private readonly Lazy<RemoteCollection> remotes;

        /// <summary>
        /// Needed for mocking purposes.
        /// </summary>
        protected Network()
        { }

        internal Network(Repository repository)
        {
            this.repository = repository;
            remotes = new Lazy<RemoteCollection>(() => new RemoteCollection(repository));
        }

        /// <summary>
        /// Lookup and manage remotes in the repository.
        /// </summary>
        public virtual RemoteCollection Remotes
        {
            get { return remotes.Value; }
        }

        /// <summary>
        /// List references in a <see cref="Remote"/> repository.
        /// <para>
        /// When the remote tips are ahead of the local ones, the retrieved
        /// <see cref="DirectReference"/>s may point to non existing
        /// <see cref="GitObject"/>s in the local repository. In that
        /// case, <see cref="DirectReference.Target"/> will return <c>null</c>.
        /// </para>
        /// </summary>
        /// <param name="remote">The <see cref="Remote"/> to list from.</param>
        /// <returns>The references in the <see cref="Remote"/> repository.</returns>
        public virtual IEnumerable<Reference> ListReferences(Remote remote)
        {
            Ensure.ArgumentNotNull(remote, "remote");

            return ListReferencesInternal(remote.Url, null);
        }

        /// <summary>
        /// List references in a <see cref="Remote"/> repository.
        /// <para>
        /// When the remote tips are ahead of the local ones, the retrieved
        /// <see cref="DirectReference"/>s may point to non existing
        /// <see cref="GitObject"/>s in the local repository. In that
        /// case, <see cref="DirectReference.Target"/> will return <c>null</c>.
        /// </para>
        /// </summary>
        /// <param name="remote">The <see cref="Remote"/> to list from.</param>
        /// <param name="credentialsProvider">The <see cref="Func{Credentials}"/> used to connect to remote repository.</param>
        /// <returns>The references in the <see cref="Remote"/> repository.</returns>
        public virtual IEnumerable<Reference> ListReferences(Remote remote, CredentialsHandler credentialsProvider)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNull(credentialsProvider, "credentialsProvider");

            return ListReferencesInternal(remote.Url, credentialsProvider);
        }

        /// <summary>
        /// List references in a remote repository.
        /// <para>
        /// When the remote tips are ahead of the local ones, the retrieved
        /// <see cref="DirectReference"/>s may point to non existing
        /// <see cref="GitObject"/>s in the local repository. In that
        /// case, <see cref="DirectReference.Target"/> will return <c>null</c>.
        /// </para>
        /// </summary>
        /// <param name="url">The url to list from.</param>
        /// <returns>The references in the remote repository.</returns>
        public virtual IEnumerable<Reference> ListReferences(string url)
        {
            Ensure.ArgumentNotNull(url, "url");

            return ListReferencesInternal(url, null);
        }

        /// <summary>
        /// List references in a remote repository.
        /// <para>
        /// When the remote tips are ahead of the local ones, the retrieved
        /// <see cref="DirectReference"/>s may point to non existing
        /// <see cref="GitObject"/>s in the local repository. In that
        /// case, <see cref="DirectReference.Target"/> will return <c>null</c>.
        /// </para>
        /// </summary>
        /// <param name="url">The url to list from.</param>
        /// <param name="credentialsProvider">The <see cref="Func{Credentials}"/> used to connect to remote repository.</param>
        /// <returns>The references in the remote repository.</returns>
        public virtual IEnumerable<Reference> ListReferences(string url, CredentialsHandler credentialsProvider)
        {
            Ensure.ArgumentNotNull(url, "url");
            Ensure.ArgumentNotNull(credentialsProvider, "credentialsProvider");

            return ListReferencesInternal(url, credentialsProvider);
        }

        private IEnumerable<Reference> ListReferencesInternal(string url, CredentialsHandler credentialsProvider)
        {
            using (RemoteSafeHandle remoteHandle = BuildRemoteSafeHandle(repository.Handle, url))
            {
                GitRemoteCallbacks gitCallbacks = new GitRemoteCallbacks { version = 1 };

                if (credentialsProvider != null)
                {
                    var callbacks = new RemoteCallbacks(credentialsProvider);
                    gitCallbacks = callbacks.GenerateCallbacks();
                }

                Proxy.git_remote_connect(remoteHandle, GitDirection.Fetch, ref gitCallbacks);
                return Proxy.git_remote_ls(repository, remoteHandle);
            }
        }

        static RemoteSafeHandle BuildRemoteSafeHandle(RepositorySafeHandle repoHandle, Remote remote)
        {
            Debug.Assert(repoHandle != null && !repoHandle.IsClosed && !repoHandle.IsInvalid);
            Debug.Assert(remote != null && remote.Name != null);

            RemoteSafeHandle remoteHandle = Proxy.git_remote_lookup(repoHandle, remote.Name, true);
            Debug.Assert(remoteHandle != null && !(remoteHandle.IsClosed || remoteHandle.IsInvalid));

            return remoteHandle;
        }

        static RemoteSafeHandle BuildRemoteSafeHandle(RepositorySafeHandle repoHandle, string url)
        {
            Debug.Assert(repoHandle != null && !repoHandle.IsClosed && !repoHandle.IsInvalid);
            Debug.Assert(url != null);

            RemoteSafeHandle remoteHandle = Proxy.git_remote_create_anonymous(repoHandle, url);
            Debug.Assert(remoteHandle != null && !(remoteHandle.IsClosed || remoteHandle.IsInvalid));

            return remoteHandle;
        }

        static void DoFetch(
            RepositorySafeHandle repoHandle,
            Remote remote,
            FetchOptions options,
            string logMessage,
            IEnumerable<string> refspecs)
        {
            using (RemoteSafeHandle remoteHandle = BuildRemoteSafeHandle(repoHandle, remote))
            {
                DoFetch(options, remoteHandle, logMessage, refspecs);
            }
        }

        static void DoFetch(
            RepositorySafeHandle repoHandle,
            string url,
            FetchOptions options,
            string logMessage,
            IEnumerable<string> refspecs)
        {
            using (RemoteSafeHandle remoteHandle = BuildRemoteSafeHandle(repoHandle, url))
            {
                DoFetch(options, remoteHandle, logMessage, refspecs);
            }
        }

        private static void DoFetch(FetchOptions options, RemoteSafeHandle remoteHandle, string logMessage, IEnumerable<string> refspecs)
        {
            Debug.Assert(remoteHandle != null && !remoteHandle.IsClosed && !remoteHandle.IsInvalid);

            options = options ?? new FetchOptions();

            var callbacks = new RemoteCallbacks(options);
            GitRemoteCallbacks gitCallbacks = callbacks.GenerateCallbacks();

            // It is OK to pass the reference to the GitCallbacks directly here because libgit2 makes a copy of
            // the data in the git_remote_callbacks structure. If, in the future, libgit2 changes its implementation
            // to store a reference to the git_remote_callbacks structure this would introduce a subtle bug
            // where the managed layer could move the git_remote_callbacks to a different location in memory,
            // but libgit2 would still reference the old address.
            //
            // Also, if GitRemoteCallbacks were a class instead of a struct, we would need to guard against
            // GC occuring in between setting the remote callbacks and actual usage in one of the functions afterwords.
            var fetchOptions = new GitFetchOptions
            {
                RemoteCallbacks = gitCallbacks,
                download_tags = Proxy.git_remote_autotag(remoteHandle),
            };

            if (options.TagFetchMode.HasValue)
            {
                fetchOptions.download_tags = options.TagFetchMode.Value;
            }

            Proxy.git_remote_fetch(remoteHandle, refspecs, fetchOptions, logMessage);
        }

        /// <summary>
        /// Fetch from the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The remote to fetch</param>
        public virtual void Fetch(Remote remote)
        {
            Fetch(remote, (FetchOptions)null, null);
        }

        /// <summary>
        /// Fetch from the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The remote to fetch</param>
        /// <param name="options"><see cref="FetchOptions"/> controlling fetch behavior</param>
        public virtual void Fetch(Remote remote, FetchOptions options)
        {
            Fetch(remote, options, null);
        }

        /// <summary>
        /// Fetch from the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The remote to fetch</param>
        /// <param name="logMessage">Message to use when updating the reflog.</param>
        public virtual void Fetch(Remote remote, string logMessage)
        {
            Fetch(remote, (FetchOptions)null, logMessage);
        }

        /// <summary>
        /// Fetch from the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The remote to fetch</param>
        /// <param name="options"><see cref="FetchOptions"/> controlling fetch behavior</param>
        /// <param name="logMessage">Message to use when updating the reflog.</param>
        public virtual void Fetch(Remote remote, FetchOptions options, string logMessage)
        {
            Ensure.ArgumentNotNull(remote, "remote");

            DoFetch(repository.Handle, remote, options, logMessage, new string[0]);
        }

        /// <summary>
        /// Fetch from the <see cref="Remote"/>, using custom refspecs.
        /// </summary>
        /// <param name="remote">The remote to fetch</param>
        /// <param name="refspecs">Refspecs to use, replacing the remote's fetch refspecs</param>
        public virtual void Fetch(Remote remote, IEnumerable<string> refspecs)
        {
            Fetch(remote, refspecs, null, null);
        }

        /// <summary>
        /// Fetch from the <see cref="Remote"/>, using custom refspecs.
        /// </summary>
        /// <param name="remote">The remote to fetch</param>
        /// <param name="refspecs">Refspecs to use, replacing the remote's fetch refspecs</param>
        /// <param name="options"><see cref="FetchOptions"/> controlling fetch behavior</param>
        public virtual void Fetch(Remote remote, IEnumerable<string> refspecs, FetchOptions options)
        {
            Fetch(remote, refspecs, options, null);
        }

        /// <summary>
        /// Fetch from the <see cref="Remote"/>, using custom refspecs.
        /// </summary>
        /// <param name="remote">The remote to fetch</param>
        /// <param name="refspecs">Refspecs to use, replacing the remote's fetch refspecs</param>
        /// <param name="logMessage">Message to use when updating the reflog.</param>
        public virtual void Fetch(Remote remote, IEnumerable<string> refspecs, string logMessage)
        {
            Fetch(remote, refspecs, null, logMessage);
        }

        /// <summary>
        /// Fetch from the <see cref="Remote"/>, using custom refspecs.
        /// </summary>
        /// <param name="remote">The remote to fetch</param>
        /// <param name="refspecs">Refspecs to use, replacing the remote's fetch refspecs</param>
        /// <param name="options"><see cref="FetchOptions"/> controlling fetch behavior</param>
        /// <param name="logMessage">Message to use when updating the reflog.</param>
        public virtual void Fetch(Remote remote, IEnumerable<string> refspecs, FetchOptions options, string logMessage)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNull(refspecs, "refspecs");

            DoFetch(repository.Handle, remote, options, logMessage, refspecs);
        }

        /// <summary>
        /// Fetch from a url with a set of fetch refspecs
        /// </summary>
        /// <param name="url">The url to fetch from</param>
        /// <param name="refspecs">The list of resfpecs to use</param>
        public virtual void Fetch(string url, IEnumerable<string> refspecs)
        {
            Fetch(url, refspecs, null, null);
        }

        /// <summary>
        /// Fetch from a url with a set of fetch refspecs
        /// </summary>
        /// <param name="url">The url to fetch from</param>
        /// <param name="refspecs">The list of resfpecs to use</param>
        /// <param name="options"><see cref="FetchOptions"/> controlling fetch behavior</param>
        public virtual void Fetch(string url, IEnumerable<string> refspecs, FetchOptions options)
        {
            Fetch(url, refspecs, options, null);
        }

        /// <summary>
        /// Fetch from a url with a set of fetch refspecs
        /// </summary>
        /// <param name="url">The url to fetch from</param>
        /// <param name="refspecs">The list of resfpecs to use</param>
        /// <param name="logMessage">Message to use when updating the reflog.</param>
        public virtual void Fetch(string url, IEnumerable<string> refspecs, string logMessage)
        {
            Fetch(url, refspecs, null, logMessage);
        }

        /// <summary>
        /// Fetch from a url with a set of fetch refspecs
        /// </summary>
        /// <param name="url">The url to fetch from</param>
        /// <param name="refspecs">The list of resfpecs to use</param>
        /// <param name="options"><see cref="FetchOptions"/> controlling fetch behavior</param>
        /// <param name="logMessage">Message to use when updating the reflog.</param>
        public virtual void Fetch(
            string url,
            IEnumerable<string> refspecs,
            FetchOptions options,
            string logMessage)
        {
            Ensure.ArgumentNotNull(url, "url");
            Ensure.ArgumentNotNull(refspecs, "refspecs");

            DoFetch(repository.Handle, url, options, logMessage, refspecs);
        }

        /// <summary>
        /// Push the specified branch to its tracked branch on the remote.
        /// </summary>
        /// <param name="branch">The branch to push.</param>
        /// <exception cref="LibGit2SharpException">Throws if either the Remote or the UpstreamBranchCanonicalName is not set.</exception>
        public virtual void Push(
            Branch branch)
        {
            Push(new[] { branch });
        }
        /// <summary>
        /// Push the specified branch to its tracked branch on the remote.
        /// </summary>
        /// <param name="branch">The branch to push.</param>
        /// <param name="pushOptions"><see cref="PushOptions"/> controlling push behavior</param>
        /// <exception cref="LibGit2SharpException">Throws if either the Remote or the UpstreamBranchCanonicalName is not set.</exception>
        public virtual void Push(
            Branch branch,
            PushOptions pushOptions)
        {
            Push(new[] { branch }, pushOptions);
        }

        /// <summary>
        /// Push the specified branches to their tracked branches on the remote.
        /// </summary>
        /// <param name="branches">The branches to push.</param>
        /// <exception cref="LibGit2SharpException">Throws if either the Remote or the UpstreamBranchCanonicalName is not set.</exception>
        public virtual void Push(
            IEnumerable<Branch> branches)
        {
            Push(branches, null);
        }

        /// <summary>
        /// Push the specified branches to their tracked branches on the remote.
        /// </summary>
        /// <param name="branches">The branches to push.</param>
        /// <param name="pushOptions"><see cref="PushOptions"/> controlling push behavior</param>
        /// <exception cref="LibGit2SharpException">Throws if either the Remote or the UpstreamBranchCanonicalName is not set.</exception>
        public virtual void Push(
            IEnumerable<Branch> branches,
            PushOptions pushOptions)
        {
            var enumeratedBranches = branches as IList<Branch> ?? branches.ToList();

            foreach (var branch in enumeratedBranches)
            {
                if (string.IsNullOrEmpty(branch.UpstreamBranchCanonicalName))
                {
                    throw new LibGit2SharpException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The branch '{0}' (\"{1}\") that you are trying to push does not track an upstream branch.",
                            branch.FriendlyName, branch.CanonicalName));
                }
            }

            foreach (var branch in enumeratedBranches)
            {
                Push(branch.Remote, string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}:{1}", branch.CanonicalName, branch.UpstreamBranchCanonicalName), pushOptions);
            }
        }

        /// <summary>
        /// Push the objectish to the destination reference on the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The <see cref="Remote"/> to push to.</param>
        /// <param name="objectish">The source objectish to push.</param>
        /// <param name="destinationSpec">The reference to update on the remote.</param>
        public virtual void Push(
            Remote remote,
            string objectish,
            string destinationSpec)
        {
            Ensure.ArgumentNotNull(objectish, "objectish");
            Ensure.ArgumentNotNullOrEmptyString(destinationSpec, "destinationSpec");

            Push(remote,
                 string.Format(CultureInfo.InvariantCulture,
                               "{0}:{1}",
                               objectish,
                               destinationSpec));
        }

        /// <summary>
        /// Push the objectish to the destination reference on the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The <see cref="Remote"/> to push to.</param>
        /// <param name="objectish">The source objectish to push.</param>
        /// <param name="destinationSpec">The reference to update on the remote.</param>
        /// <param name="pushOptions"><see cref="PushOptions"/> controlling push behavior</param>
        public virtual void Push(
            Remote remote,
            string objectish,
            string destinationSpec,
            PushOptions pushOptions)
        {
            Ensure.ArgumentNotNull(objectish, "objectish");
            Ensure.ArgumentNotNullOrEmptyString(destinationSpec, "destinationSpec");

            Push(remote,
                 string.Format(CultureInfo.InvariantCulture,
                               "{0}:{1}",
                               objectish,
                               destinationSpec),
                 pushOptions);
        }

        /// <summary>
        /// Push specified reference to the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The <see cref="Remote"/> to push to.</param>
        /// <param name="pushRefSpec">The pushRefSpec to push.</param>
        public virtual void Push(Remote remote, string pushRefSpec)
        {
            Ensure.ArgumentNotNullOrEmptyString(pushRefSpec, "pushRefSpec");

            Push(remote, new[] { pushRefSpec });
        }
        /// <summary>
        /// Push specified reference to the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The <see cref="Remote"/> to push to.</param>
        /// <param name="pushRefSpec">The pushRefSpec to push.</param>
        /// <param name="pushOptions"><see cref="PushOptions"/> controlling push behavior</param>
        public virtual void Push(
            Remote remote,
            string pushRefSpec,
            PushOptions pushOptions)
        {
            Ensure.ArgumentNotNullOrEmptyString(pushRefSpec, "pushRefSpec");

            Push(remote, new[] { pushRefSpec }, pushOptions);
        }

        /// <summary>
        /// Push specified references to the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The <see cref="Remote"/> to push to.</param>
        /// <param name="pushRefSpecs">The pushRefSpecs to push.</param>
        public virtual void Push(Remote remote, IEnumerable<string> pushRefSpecs)
        {
            Push(remote, pushRefSpecs, null);
        }

        /// <summary>
        /// Push specified references to the <see cref="Remote"/>.
        /// </summary>
        /// <param name="remote">The <see cref="Remote"/> to push to.</param>
        /// <param name="pushRefSpecs">The pushRefSpecs to push.</param>
        /// <param name="pushOptions"><see cref="PushOptions"/> controlling push behavior</param>
        public virtual void Push(Remote remote, IEnumerable<string> pushRefSpecs, PushOptions pushOptions)
        {
            Ensure.ArgumentNotNull(remote, "remote");
            Ensure.ArgumentNotNull(pushRefSpecs, "pushRefSpecs");

            // Return early if there is nothing to push.
            if (!pushRefSpecs.Any())
            {
                return;
            }

            if (pushOptions == null)
            {
                pushOptions = new PushOptions();
            }

            // Load the remote.
            using (RemoteSafeHandle remoteHandle = Proxy.git_remote_lookup(repository.Handle, remote.Name, true))
            {
                var callbacks = new RemoteCallbacks(pushOptions);
                GitRemoteCallbacks gitCallbacks = callbacks.GenerateCallbacks();

                Proxy.git_remote_push(remoteHandle,
                                      pushRefSpecs,
                                      new GitPushOptions()
                                      {
                                          PackbuilderDegreeOfParallelism = pushOptions.PackbuilderDegreeOfParallelism,
                                          RemoteCallbacks = gitCallbacks,
                                      });
            }
        }

        /// <summary>
        /// Pull changes from the configured upstream remote and branch into the branch pointed at by HEAD.
        /// </summary>
        /// <param name="merger">If the merge is a non-fast forward merge that generates a merge commit, the <see cref="Signature"/> of who made the merge.</param>
        /// <param name="options">Specifies optional parameters controlling merge behavior of pull; if null, the defaults are used.</param>
        public virtual MergeResult Pull(Signature merger, PullOptions options)
        {
            Ensure.ArgumentNotNull(merger, "merger");
            Ensure.ArgumentNotNull(options, "options");

            Branch currentBranch = repository.Head;

            if (!currentBranch.IsTracking)
            {
                throw new LibGit2SharpException("There is no tracking information for the current branch.");
            }

            if (currentBranch.Remote == null)
            {
                throw new LibGit2SharpException("No upstream remote for the current branch.");
            }

            Fetch(currentBranch.Remote, options.FetchOptions);
            return repository.MergeFetchedRefs(merger, options.MergeOptions);
        }

        /// <summary>
        /// The heads that have been updated during the last fetch.
        /// </summary>
        internal virtual IEnumerable<FetchHead> FetchHeads
        {
            get
            {
                int i = 0;

                Func<string, string, GitOid, bool, FetchHead> resultSelector =
                    (name, url, oid, isMerge) => new FetchHead(repository, name, url, oid, isMerge, i++);

                return Proxy.git_repository_fetchhead_foreach(repository.Handle, resultSelector);
            }
        }
    }
}
