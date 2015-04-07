using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;
using LibGit2Sharp.Core.Handles;

// ReSharper disable InconsistentNaming
namespace LibGit2Sharp.Core
{
    internal static class NativeMethods
    {
        public const uint GIT_PATH_MAX = 4096;
        private const string libgit2 = NativeDllName.Name;
        private static readonly LibraryLifetimeObject lifetimeObject;
        private static int handlesCount;

        /// <summary>
        /// Internal hack to ensure that the call to git_threads_shutdown is called after all handle finalizers
        /// have run to completion ensuring that no dangling git-related finalizer runs after git_threads_shutdown.
        /// There should never be more than one instance of this object per AppDomain.
        /// </summary>
        private sealed class LibraryLifetimeObject : CriticalFinalizerObject
        {
            // Ensure mono can JIT the .cctor and adjust the PATH before trying to load the native library.
            // See https://github.com/libgit2/libgit2sharp/pull/190
            [MethodImpl(MethodImplOptions.NoInlining)]
            public LibraryLifetimeObject()
            {
                int res = git_libgit2_init();
                Ensure.Int32Result(res);
                if (res == 1)
                {
                    // Ignore the error that this propagates. Call it in case openssl is being used.
                    git_openssl_set_locking();
                }
                AddHandle();
            }

            ~LibraryLifetimeObject()
            {
                RemoveHandle();
            }
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static void AddHandle()
        {
            Interlocked.Increment(ref handlesCount);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        internal static void RemoveHandle()
        {
            int count = Interlocked.Decrement(ref handlesCount);
            if (count == 0)
            {
                git_libgit2_shutdown();
            }
        }

        static NativeMethods()
        {
            if (Platform.OperatingSystem == OperatingSystemType.Windows)
            {
                string nativeLibraryPath = GlobalSettings.GetAndLockNativeLibraryPath();

                string path = Path.Combine(nativeLibraryPath, Platform.ProcessorArchitecture);

                const string pathEnvVariable = "PATH";
                Environment.SetEnvironmentVariable(pathEnvVariable,
                    String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", path, Path.PathSeparator, Environment.GetEnvironmentVariable(pathEnvVariable)));
            }

            // See LibraryLifetimeObject description.
            lifetimeObject = new LibraryLifetimeObject();
        }

        [DllImport(libgit2)]
        internal static extern GitErrorSafeHandle giterr_last();

        [DllImport(libgit2)]
        internal static extern void giterr_set_str(
            GitErrorCategory error_class,
            string errorString);

        [DllImport(libgit2)]
        internal static extern void giterr_set_oom();

        [DllImport(libgit2)]
        internal static extern UInt32 git_blame_get_hunk_count(BlameSafeHandle blame);

        [DllImport(libgit2)]
        internal static extern IntPtr git_blame_get_hunk_byindex(
            BlameSafeHandle blame, UInt32 index);

        [DllImport(libgit2)]
        internal static extern int git_blame_file(
            out BlameSafeHandle blame,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path,
            GitBlameOptions options);

        [DllImport(libgit2)]
        internal static extern void git_blame_free(IntPtr blame);

        [DllImport(libgit2)]
        internal static extern int git_blob_create_fromdisk(
            ref GitOid id,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path);

        [DllImport(libgit2)]
        internal static extern int git_blob_create_fromworkdir(
            ref GitOid id,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath relative_path);

        internal delegate int source_callback(
            IntPtr content,
            int max_length,
            IntPtr data);

        [DllImport(libgit2)]
        internal static extern int git_blob_create_fromchunks(
            ref GitOid oid,
            RepositorySafeHandle repositoryPtr,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath hintpath,
            source_callback fileCallback,
            IntPtr data);

        [DllImport(libgit2)]
        internal static extern int git_blob_filtered_content(
            GitBuf buf,
            GitObjectSafeHandle blob,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath as_path,
            [MarshalAs(UnmanagedType.Bool)] bool check_for_binary_data);

        [DllImport(libgit2)]
        internal static extern IntPtr git_blob_rawcontent(GitObjectSafeHandle blob);

        [DllImport(libgit2)]
        internal static extern Int64 git_blob_rawsize(GitObjectSafeHandle blob);

        [DllImport(libgit2)]
        internal static extern int git_branch_create_from_annotated(
            out ReferenceSafeHandle ref_out,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string branch_name,
            GitAnnotatedCommitHandle target,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2)]
        internal static extern int git_branch_delete(
            ReferenceSafeHandle reference);

        internal delegate int branch_foreach_callback(
            IntPtr branch_name,
            GitBranchType branch_type,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern void git_branch_iterator_free(
            IntPtr iterator);

        [DllImport(libgit2)]
        internal static extern int git_branch_iterator_new(
            out BranchIteratorSafeHandle iter_out,
            RepositorySafeHandle repo,
            GitBranchType branch_type);

        [DllImport(libgit2)]
        internal static extern int git_branch_move(
            out ReferenceSafeHandle ref_out,
            ReferenceSafeHandle reference,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string new_branch_name,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2)]
        internal static extern int git_branch_next(
            out ReferenceSafeHandle ref_out,
            out GitBranchType type_out,
            BranchIteratorSafeHandle iter);

        [DllImport(libgit2)]
        internal static extern int git_branch_remote_name(
            GitBuf buf,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string canonical_branch_name);

        [DllImport(libgit2)]
        internal static extern int git_remote_rename(
            ref GitStrArray problems,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string old_name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string new_name);

        internal delegate int git_remote_rename_problem_cb(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string problematic_refspec,
            IntPtr payload);


        [DllImport(libgit2)]
        internal static extern int git_branch_upstream_name(
            GitBuf buf,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string referenceName);

        [DllImport(libgit2)]
        internal static extern void git_buf_free(GitBuf buf);

        [DllImport(libgit2)]
        internal static extern int git_checkout_tree(
            RepositorySafeHandle repo,
            GitObjectSafeHandle treeish,
            ref GitCheckoutOpts opts);

        [DllImport(libgit2)]
        internal static extern int git_checkout_index(
            RepositorySafeHandle repo,
            GitObjectSafeHandle treeish,
            ref GitCheckoutOpts opts);

        [DllImport(libgit2)]
        internal static extern int git_clone(
            out RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string origin_url,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath workdir_path,
            ref GitCloneOptions opts);

        [DllImport(libgit2)]
        internal static extern IntPtr git_commit_author(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        internal static extern IntPtr git_commit_committer(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        internal static extern int git_commit_create_from_ids(
            out GitOid id,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string updateRef,
            SignatureSafeHandle author,
            SignatureSafeHandle committer,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string encoding,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string message,
            ref GitOid tree,
            UIntPtr parentCount,
            [MarshalAs(UnmanagedType.LPArray)] [In] IntPtr[] parents);

        [DllImport(libgit2)]
        [return : MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_commit_message(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_commit_summary(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_commit_message_encoding(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_commit_parent_id(GitObjectSafeHandle commit, uint n);

        [DllImport(libgit2)]
        internal static extern uint git_commit_parentcount(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_commit_tree_id(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        internal static extern int git_config_delete_entry(ConfigurationSafeHandle cfg, string name);

        [DllImport(libgit2)]
        internal static extern int git_config_find_global(GitBuf global_config_path);

        [DllImport(libgit2)]
        internal static extern int git_config_find_system(GitBuf system_config_path);

        [DllImport(libgit2)]
        internal static extern int git_config_find_xdg(GitBuf xdg_config_path);

        [DllImport(libgit2)]
        internal static extern void git_config_free(IntPtr cfg);

        [DllImport(libgit2)]
        internal static extern void git_config_entry_free(IntPtr entry);

        [DllImport(libgit2)]
        internal static extern int git_config_get_entry(
            out GitConfigEntryHandle entry,
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2)]
        internal static extern int git_config_add_file_ondisk(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path,
            uint level,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2)]
        internal static extern int git_config_new(out ConfigurationSafeHandle cfg);

        [DllImport(libgit2)]
        internal static extern int git_config_open_level(
            out ConfigurationSafeHandle cfg,
            ConfigurationSafeHandle parent,
            uint level);

        [DllImport(libgit2)]
        internal static extern int git_config_parse_bool(
            [MarshalAs(UnmanagedType.Bool)] out bool value,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string valueToParse);

        [DllImport(libgit2)]
        internal static extern int git_config_parse_int32(
            [MarshalAs(UnmanagedType.I4)] out int value,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string valueToParse);

        [DllImport(libgit2)]
        internal static extern int git_config_parse_int64(
            [MarshalAs(UnmanagedType.I8)] out long value,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string valueToParse);

        [DllImport(libgit2)]
        internal static extern int git_config_set_bool(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.Bool)] bool value);

        [DllImport(libgit2)]
        internal static extern int git_config_set_int32(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            int value);

        [DllImport(libgit2)]
        internal static extern int git_config_set_int64(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            long value);

        [DllImport(libgit2)]
        internal static extern int git_config_set_string(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string value);

        internal delegate int config_foreach_callback(
            IntPtr entry,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_config_foreach(
            ConfigurationSafeHandle cfg,
            config_foreach_callback callback,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_config_iterator_glob_new(
            out ConfigurationIteratorSafeHandle iter,
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string regexp);

        [DllImport(libgit2)]
        internal static extern int git_config_next(
            out IntPtr entry,
            ConfigurationIteratorSafeHandle iter);

        [DllImport(libgit2)]
        internal static extern void git_config_iterator_free(IntPtr iter);

        [DllImport(libgit2)]
        internal static extern int git_config_snapshot(out ConfigurationSafeHandle @out, ConfigurationSafeHandle config);

        // Ordinarily we would decorate the `url` parameter with the StrictUtf8Marshaler like we do everywhere
        // else, but apparently doing a native->managed callback with the 64-bit version of CLR 2.0 can
        // sometimes vomit when using a custom IMarshaler.  So yeah, don't do that.  If you need the url,
        // call StrictUtf8Marshaler.FromNative manually.  See the discussion here:
        // http://social.msdn.microsoft.com/Forums/en-US/netfx64bit/thread/1eb746c6-d695-4632-8a9e-16c4fa98d481
        internal delegate int git_cred_acquire_cb(
            out IntPtr cred,
            IntPtr url,
            IntPtr username_from_url,
            GitCredentialType allowed_types,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_cred_default_new(out IntPtr cred);

        [DllImport(libgit2)]
        internal static extern int git_cred_userpass_plaintext_new(
            out IntPtr cred,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof (StrictUtf8Marshaler))] string username,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof (StrictUtf8Marshaler))] string password);

        [DllImport(libgit2)]
        internal static extern int git_describe_commit(
            out DescribeResultSafeHandle describe,
            GitObjectSafeHandle committish,
            ref GitDescribeOptions options);

        [DllImport(libgit2)]
        internal static extern int git_describe_format(
            GitBuf buf,
            DescribeResultSafeHandle describe,
            ref GitDescribeFormatOptions options);

        [DllImport(libgit2)]
        internal static extern void git_describe_result_free(IntPtr describe);

        [DllImport(libgit2)]
        internal static extern void git_diff_free(IntPtr diff);

        [DllImport(libgit2)]
        internal static extern int git_diff_tree_to_tree(
            out DiffSafeHandle diff,
            RepositorySafeHandle repo,
            GitObjectSafeHandle oldTree,
            GitObjectSafeHandle newTree,
            GitDiffOptions options);

        [DllImport(libgit2)]
        internal static extern int git_diff_tree_to_index(
            out DiffSafeHandle diff,
            RepositorySafeHandle repo,
            GitObjectSafeHandle oldTree,
            IndexSafeHandle index,
            GitDiffOptions options);

        [DllImport(libgit2)]
        internal static extern int git_diff_merge(
            DiffSafeHandle onto,
            DiffSafeHandle from);

        [DllImport(libgit2)]
        internal static extern int git_diff_index_to_workdir(
            out DiffSafeHandle diff,
            RepositorySafeHandle repo,
            IndexSafeHandle index,
            GitDiffOptions options);

        [DllImport(libgit2)]
        internal static extern int git_diff_tree_to_workdir(
            out DiffSafeHandle diff,
            RepositorySafeHandle repo,
            GitObjectSafeHandle oldTree,
            GitDiffOptions options);

        internal delegate int git_diff_file_cb(
            [In] GitDiffDelta delta,
            float progress,
            IntPtr payload);

        internal delegate int git_diff_hunk_cb(
            [In] GitDiffDelta delta,
            [In] GitDiffHunk hunk,
            IntPtr payload);

        internal delegate int git_diff_line_cb(
            [In] GitDiffDelta delta,
            [In] GitDiffHunk hunk,
            [In] GitDiffLine line,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_diff_blobs(
            GitObjectSafeHandle oldBlob,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath old_as_path,
            GitObjectSafeHandle newBlob,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath new_as_path,
            GitDiffOptions options,
            git_diff_file_cb fileCallback,
            git_diff_hunk_cb hunkCallback,
            git_diff_line_cb lineCallback,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_diff_foreach(
            DiffSafeHandle diff,
            git_diff_file_cb fileCallback,
            git_diff_hunk_cb hunkCallback,
            git_diff_line_cb lineCallback,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_diff_find_similar(
            DiffSafeHandle diff,
            GitDiffFindOptions options);

        [DllImport(libgit2)]
        internal static extern UIntPtr git_diff_num_deltas(DiffSafeHandle diff);

        [DllImport(libgit2)]
        internal static extern IntPtr git_diff_get_delta(DiffSafeHandle diff, UIntPtr idx);

        [DllImport(libgit2)]
        internal static extern int git_libgit2_features();

        [DllImport(libgit2)]
        internal static extern int git_graph_ahead_behind(out UIntPtr ahead, out UIntPtr behind, RepositorySafeHandle repo, ref GitOid one, ref GitOid two);

        [DllImport(libgit2)]
        internal static extern int git_graph_descendant_of(
            RepositorySafeHandle repo,
            ref GitOid commit,
            ref GitOid ancestor);

        [DllImport(libgit2)]
        internal static extern int git_ignore_add_rule(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof (StrictUtf8Marshaler))] string rules);

        [DllImport(libgit2)]
        internal static extern int git_ignore_clear_internal_rules(RepositorySafeHandle repo);

        [DllImport(libgit2)]
        internal static extern int git_ignore_path_is_ignored(
            out int ignored,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path);

        [DllImport(libgit2)]
        internal static extern int git_index_add_bypath(
            IndexSafeHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path);

        [DllImport(libgit2)]
        internal static extern int git_index_add(
            IndexSafeHandle index,
            GitIndexEntry entry);

        [DllImport(libgit2)]
        internal static extern int git_index_conflict_get(
            out IndexEntrySafeHandle ancestor,
            out IndexEntrySafeHandle ours,
            out IndexEntrySafeHandle theirs,
            IndexSafeHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path);

        [DllImport(libgit2)]
        internal static extern UIntPtr git_index_entrycount(IndexSafeHandle index);

        [DllImport(libgit2)]
        internal static extern int git_index_entry_stage(IndexEntrySafeHandle indexentry);

        [DllImport(libgit2)]
        internal static extern void git_index_free(IntPtr index);

        [DllImport(libgit2)]
        internal static extern IndexEntrySafeHandle git_index_get_byindex(IndexSafeHandle index, UIntPtr n);

        [DllImport(libgit2)]
        internal static extern IndexEntrySafeHandle git_index_get_bypath(
            IndexSafeHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path,
            int stage);

        [DllImport(libgit2)]
        internal static extern int git_index_has_conflicts(IndexSafeHandle index);

        [DllImport(libgit2)]
        internal static extern uint git_index_name_entrycount(IndexSafeHandle handle);

        [DllImport(libgit2)]
        internal static extern IndexNameEntrySafeHandle git_index_name_get_byindex(IndexSafeHandle handle, UIntPtr n);

        [DllImport(libgit2)]
        internal static extern int git_index_open(
            out IndexSafeHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath indexpath);

        [DllImport(libgit2)]
        internal static extern int git_index_read(
            IndexSafeHandle index,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2)]
        internal static extern int git_index_remove_bypath(
            IndexSafeHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path);


        [DllImport(libgit2)]
        internal static extern uint git_index_reuc_entrycount(IndexSafeHandle handle);

        [DllImport(libgit2)]
        internal static extern IndexReucEntrySafeHandle git_index_reuc_get_byindex(IndexSafeHandle handle, UIntPtr n);

        [DllImport(libgit2)]
        internal static extern IndexReucEntrySafeHandle git_index_reuc_get_bypath(
            IndexSafeHandle handle,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path);

        [DllImport(libgit2)]
        internal static extern int git_index_write(IndexSafeHandle index);

        [DllImport(libgit2)]
        internal static extern int git_index_write_tree(out GitOid treeOid, IndexSafeHandle index);

        [DllImport(libgit2)]
        internal static extern int git_index_read_tree(IndexSafeHandle index, GitObjectSafeHandle tree);

        [DllImport(libgit2)]
        internal static extern int git_index_clear(IndexSafeHandle index);

        [DllImport(libgit2)]
        internal static extern int git_merge_base_many(
            out GitOid mergeBase,
            RepositorySafeHandle repo,
            int length,
            [In] GitOid[] input_array);

        [DllImport(libgit2)]
        internal static extern int git_merge_base_octopus(
            out GitOid mergeBase,
            RepositorySafeHandle repo,
            int length,
            [In] GitOid[] input_array);

        [DllImport(libgit2)]
        internal static extern int git_annotated_commit_from_ref(
            out GitAnnotatedCommitHandle annotatedCommit,
            RepositorySafeHandle repo,
            ReferenceSafeHandle reference);

        [DllImport(libgit2)]
        internal static extern int git_annotated_commit_from_fetchhead(
            out GitAnnotatedCommitHandle annotatedCommit,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string branch_name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string remote_url,
            ref GitOid oid);

        [DllImport(libgit2)]
        internal static extern int git_annotated_commit_from_revspec(
            out GitAnnotatedCommitHandle annotatedCommit,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string revspec);

        [DllImport(libgit2)]
        internal static extern int git_annotated_commit_lookup(
            out GitAnnotatedCommitHandle annotatedCommit,
            RepositorySafeHandle repo,
            ref GitOid id);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_annotated_commit_id(
            GitAnnotatedCommitHandle annotatedCommit);

        [DllImport(libgit2)]
        internal static extern int git_merge(
            RepositorySafeHandle repo,
            [In] IntPtr[] their_heads,
            UIntPtr their_heads_len,
            ref GitMergeOpts merge_opts,
            ref GitCheckoutOpts checkout_opts);

        [DllImport(libgit2)]
        internal static extern int git_merge_trees(
            out IndexSafeHandle index,
            RepositorySafeHandle repo,
            GitObjectSafeHandle ancestor_tree,
            GitObjectSafeHandle our_tree,
            GitObjectSafeHandle their_tree,
            ref GitMergeOpts merge_opts);

        [DllImport(libgit2)]
        internal static extern int git_merge_analysis(
            out GitMergeAnalysis status_out,
            out GitMergePreference preference_out,
            RepositorySafeHandle repo,
            [In] IntPtr[] their_heads,
            int their_heads_len);

        [DllImport(libgit2)]
        internal static extern void git_annotated_commit_free(
            IntPtr merge_head);

        [DllImport(libgit2)]
        internal static extern int git_message_prettify(
            GitBuf buf,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string message,
            [MarshalAs(UnmanagedType.Bool)] bool strip_comments,
            sbyte comment_char);

        [DllImport(libgit2)]
        internal static extern int git_note_create(
            out GitOid noteOid,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string notes_ref,
            SignatureSafeHandle author,
            SignatureSafeHandle committer,
            ref GitOid oid,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string note,
            int force);

        [DllImport(libgit2)]
        internal static extern void git_note_free(IntPtr note);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_note_message(NoteSafeHandle note);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_note_id(NoteSafeHandle note);

        [DllImport(libgit2)]
        internal static extern int git_note_read(
            out NoteSafeHandle note,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string notes_ref,
            ref GitOid oid);

        [DllImport(libgit2)]
        internal static extern int git_note_remove(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string notes_ref,
            SignatureSafeHandle author,
            SignatureSafeHandle committer,
            ref GitOid oid);

        [DllImport(libgit2)]
        internal static extern int git_note_default_ref(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] out string notes_ref,
            RepositorySafeHandle repo);

        internal delegate int git_note_foreach_cb(
            ref GitOid blob_id,
            ref GitOid annotated_object_id,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_note_foreach(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string notes_ref,
            git_note_foreach_cb cb,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_odb_add_backend(ObjectDatabaseSafeHandle odb, IntPtr backend, int priority);

        [DllImport(libgit2)]
        internal static extern IntPtr git_odb_backend_malloc(IntPtr backend, UIntPtr len);

        [DllImport(libgit2)]
        internal static extern int git_odb_exists(ObjectDatabaseSafeHandle odb, ref GitOid id);

        internal delegate int git_odb_foreach_cb(
            IntPtr id,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_odb_foreach(
            ObjectDatabaseSafeHandle odb,
            git_odb_foreach_cb cb,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_odb_open_wstream(out OdbStreamSafeHandle stream, ObjectDatabaseSafeHandle odb, UIntPtr size, GitObjectType type);

        [DllImport(libgit2)]
        internal static extern void git_odb_free(IntPtr odb);

        [DllImport(libgit2)]
        internal static extern int git_odb_read_header(out UIntPtr len_out, out GitObjectType type, ObjectDatabaseSafeHandle odb, ref GitOid id);

        [DllImport(libgit2)]
        internal static extern void git_object_free(IntPtr obj);

        [DllImport(libgit2)]
        internal static extern int git_odb_stream_write(OdbStreamSafeHandle Stream, IntPtr Buffer, UIntPtr len);

        [DllImport(libgit2)]
        internal static extern int git_odb_stream_finalize_write(out GitOid id, OdbStreamSafeHandle stream);

        [DllImport(libgit2)]
        internal static extern void git_odb_stream_free(IntPtr stream);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_object_id(GitObjectSafeHandle obj);

        [DllImport(libgit2)]
        internal static extern int git_object_lookup(out GitObjectSafeHandle obj, RepositorySafeHandle repo, ref GitOid id, GitObjectType type);

        [DllImport(libgit2)]
        internal static extern int git_object_peel(
            out GitObjectSafeHandle peeled,
            GitObjectSafeHandle obj,
            GitObjectType type);

        [DllImport(libgit2)]
        internal static extern int git_object_short_id(
            GitBuf buf,
            GitObjectSafeHandle obj);

        [DllImport(libgit2)]
        internal static extern GitObjectType git_object_type(GitObjectSafeHandle obj);

        [DllImport(libgit2)]
        internal static extern int git_patch_from_diff(out PatchSafeHandle patch, DiffSafeHandle diff, UIntPtr idx);

        [DllImport(libgit2)]
        internal static extern int git_patch_print(PatchSafeHandle patch, git_diff_line_cb print_cb, IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_patch_line_stats(
            out UIntPtr total_context,
            out UIntPtr total_additions,
            out UIntPtr total_deletions,
            PatchSafeHandle patch);

        [DllImport(libgit2)]
        internal static extern void git_patch_free(IntPtr patch);

        /* Push network progress notification function */
        internal delegate int git_push_transfer_progress(uint current, uint total, UIntPtr bytes, IntPtr payload);
        internal delegate int git_packbuilder_progress(int stage, uint current, uint total, IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_reference_create(
            out ReferenceSafeHandle reference,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            ref GitOid oid,
            [MarshalAs(UnmanagedType.Bool)] bool force,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string log_message);

        [DllImport(libgit2)]
        internal static extern int git_reference_symbolic_create(
            out ReferenceSafeHandle reference,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string target,
            [MarshalAs(UnmanagedType.Bool)] bool force,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string log_message);

        internal delegate int ref_glob_callback(
            IntPtr reference_name,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_reference_foreach_glob(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string glob,
            ref_glob_callback callback,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern void git_reference_free(IntPtr reference);

        [DllImport(libgit2)]
        internal static extern int git_reference_is_valid_name(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string refname);

        [DllImport(libgit2)]
        internal static extern int git_reference_list(out GitStrArray array, RepositorySafeHandle repo);

        [DllImport(libgit2)]
        internal static extern int git_reference_lookup(
            out ReferenceSafeHandle reference,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_reference_name(ReferenceSafeHandle reference);

        [DllImport(libgit2)]
        internal static extern int git_reference_remove(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_reference_target(ReferenceSafeHandle reference);

        [DllImport(libgit2)]
        internal static extern int git_reference_rename(
            out ReferenceSafeHandle ref_out,
            ReferenceSafeHandle reference,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string newName,
            [MarshalAs(UnmanagedType.Bool)] bool force,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string log_message);

        [DllImport(libgit2)]
        internal static extern int git_reference_set_target(
            out ReferenceSafeHandle ref_out,
            ReferenceSafeHandle reference,
            ref GitOid id,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string log_message);

        [DllImport(libgit2)]
        internal static extern int git_reference_symbolic_set_target(
            out ReferenceSafeHandle ref_out,
            ReferenceSafeHandle reference,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string target,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string log_message);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_reference_symbolic_target(ReferenceSafeHandle reference);

        [DllImport(libgit2)]
        internal static extern GitReferenceType git_reference_type(ReferenceSafeHandle reference);

        [DllImport(libgit2)]
        internal static extern int git_reference_ensure_log(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof (StrictUtf8Marshaler))] string refname);

        [DllImport(libgit2)]
        internal static extern void git_reflog_free(
            IntPtr reflog);

        [DllImport(libgit2)]
        internal static extern int git_reflog_read(
            out ReflogSafeHandle ref_out,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2)]
        internal static extern UIntPtr git_reflog_entrycount
            (ReflogSafeHandle reflog);

        [DllImport(libgit2)]
        internal static extern ReflogEntrySafeHandle git_reflog_entry_byindex(
            ReflogSafeHandle reflog,
            UIntPtr idx);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_reflog_entry_id_old(
            SafeHandle entry);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_reflog_entry_id_new(
            SafeHandle entry);

        [DllImport(libgit2)]
        internal static extern IntPtr git_reflog_entry_committer(
            SafeHandle entry);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_reflog_entry_message(SafeHandle entry);

        [DllImport(libgit2)]
        internal static extern int git_refspec_rtransform(
            GitBuf buf,
            GitRefSpecHandle refSpec,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_refspec_string(
            GitRefSpecHandle refSpec);

        [DllImport(libgit2)]
        internal static extern RefSpecDirection git_refspec_direction(GitRefSpecHandle refSpec);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_refspec_dst(
            GitRefSpecHandle refSpec);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_refspec_src(
            GitRefSpecHandle refSpec);

        [DllImport(libgit2)]
        internal static extern bool git_refspec_force(GitRefSpecHandle refSpec);

        [DllImport(libgit2)]
        internal static extern int git_remote_autotag(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        internal static extern int git_remote_connect(RemoteSafeHandle remote, GitDirection direction);

        [DllImport(libgit2)]
        internal static extern int git_remote_create(
            out RemoteSafeHandle remote,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string url);

        [DllImport(libgit2)]
        internal static extern int git_remote_create_anonymous(
            out RemoteSafeHandle remote,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string url,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string refspec);


        [DllImport(libgit2)]
        internal static extern int git_remote_create_with_fetchspec(
            out RemoteSafeHandle remote,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string url,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string refspec);

        [DllImport(libgit2)]
        internal static extern int git_remote_delete(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2)]
        internal static extern void git_remote_disconnect(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        internal static extern int git_remote_fetch(
            RemoteSafeHandle remote,
            ref GitStrArray refspecs,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string log_message);

        [DllImport(libgit2)]
        internal static extern void git_remote_free(IntPtr remote);

        [DllImport(libgit2)]
        internal static extern int git_remote_get_fetch_refspecs(out GitStrArray array, RemoteSafeHandle remote);

        [DllImport(libgit2)]
        internal static extern GitRefSpecHandle git_remote_get_refspec(RemoteSafeHandle remote, UIntPtr n);

        [DllImport(libgit2)]
        internal static extern int git_remote_get_push_refspecs(out GitStrArray array, RemoteSafeHandle remote);

        [DllImport(libgit2)]
        internal static extern int git_remote_push(
            RemoteSafeHandle remote,
            ref GitStrArray refSpecs,
            GitPushOptions opts);

        [DllImport(libgit2)]
        internal static extern UIntPtr git_remote_refspec_count(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        internal static extern int git_remote_set_fetch_refspecs(RemoteSafeHandle remote, ref GitStrArray array);

        [DllImport(libgit2)]
        internal static extern int git_remote_set_push_refspecs(RemoteSafeHandle remote, ref GitStrArray array);

        [DllImport(libgit2)]
        internal static extern int git_remote_set_url(
            RemoteSafeHandle remote,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string url);

        [DllImport(libgit2)]
        internal static extern int git_remote_set_pushurl(
            RemoteSafeHandle remote,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string url);

        [DllImport(libgit2)]
        internal static extern int git_remote_is_valid_name(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string remote_name);

        [DllImport(libgit2)]
        internal static extern int git_remote_list(out GitStrArray array, RepositorySafeHandle repo);

        [DllImport(libgit2)]
        internal static extern int git_remote_lookup(
            out RemoteSafeHandle remote,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        internal delegate int git_headlist_cb(ref GitRemoteHead remoteHeadPtr, IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_remote_ls(out IntPtr heads, out UIntPtr size, RemoteSafeHandle remote);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_remote_name(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        internal static extern int git_remote_save(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_remote_url(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_remote_pushurl(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        internal static extern void git_remote_set_autotag(RemoteSafeHandle remote, TagFetchMode option);

        [DllImport(libgit2)]
        internal static extern int git_remote_set_callbacks(
            RemoteSafeHandle remote,
            ref GitRemoteCallbacks callbacks);

        internal delegate int remote_progress_callback(IntPtr str, int len, IntPtr data);

        internal delegate int remote_completion_callback(RemoteCompletionType type, IntPtr data);

        internal delegate int remote_update_tips_callback(
            IntPtr refName,
            ref GitOid oldId,
            ref GitOid newId,
            IntPtr data);

        internal delegate int push_update_reference_callback(
            IntPtr refName,
            IntPtr status,
            IntPtr data
            );

        [DllImport(libgit2)]
        internal static extern int git_repository_discover(
            GitBuf buf,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath start_path,
            [MarshalAs(UnmanagedType.Bool)] bool across_fs,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath ceiling_dirs);

        internal delegate int git_repository_fetchhead_foreach_cb(
            IntPtr remote_name,
            IntPtr remote_url,
            ref GitOid oid,
            [MarshalAs(UnmanagedType.Bool)] bool is_merge,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_repository_fetchhead_foreach(
            RepositorySafeHandle repo,
            git_repository_fetchhead_foreach_cb cb,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern void git_repository_free(IntPtr repo);

        [DllImport(libgit2)]
        internal static extern int git_repository_head_detached(RepositorySafeHandle repo);

        [DllImport(libgit2)]
        internal static extern int git_repository_head_unborn(RepositorySafeHandle repo);

        [DllImport(libgit2)]
        internal static extern int git_repository_ident(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] out string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] out string email,
            RepositorySafeHandle repo);

        [DllImport(libgit2)]
        internal static extern int git_repository_index(out IndexSafeHandle index, RepositorySafeHandle repo);

        [DllImport(libgit2)]
        internal static extern int git_repository_init_ext(
            out RepositorySafeHandle repository,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path,
            GitRepositoryInitOptions options);

        [DllImport(libgit2)]
        internal static extern int git_repository_is_bare(RepositorySafeHandle handle);

        [DllImport(libgit2)]
        internal static extern int git_repository_is_shallow(RepositorySafeHandle repo);

        [DllImport(libgit2)]
        internal static extern int git_repository_state_cleanup(RepositorySafeHandle repo);

        internal delegate int git_repository_mergehead_foreach_cb(
            ref GitOid oid,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_repository_mergehead_foreach(
            RepositorySafeHandle repo,
            git_repository_mergehead_foreach_cb cb,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_repository_message(
            GitBuf buf,
            RepositorySafeHandle repository);

        [DllImport(libgit2)]
        internal static extern int git_repository_odb(out ObjectDatabaseSafeHandle odb, RepositorySafeHandle repo);

        [DllImport(libgit2)]
        internal static extern int git_repository_open(
            out RepositorySafeHandle repository,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path);

        [DllImport(libgit2)]
        internal static extern int git_repository_open_ext(
            NullRepositorySafeHandle repository,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path,
            RepositoryOpenFlags flags,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath ceilingDirs);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxFilePathNoCleanupMarshaler))]
        internal static extern FilePath git_repository_path(RepositorySafeHandle repository);

        [DllImport(libgit2)]
        internal static extern void git_repository_set_config(
            RepositorySafeHandle repository,
            ConfigurationSafeHandle config);

        [DllImport(libgit2)]
        internal static extern int git_repository_set_ident(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string email);


        [DllImport(libgit2)]
        internal static extern void git_repository_set_index(
            RepositorySafeHandle repository,
            IndexSafeHandle index);

        [DllImport(libgit2)]
        internal static extern int git_repository_set_workdir(
            RepositorySafeHandle repository,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath workdir,
            [MarshalAs(UnmanagedType.Bool)] bool update_gitlink);

        [DllImport(libgit2)]
        internal static extern int git_repository_set_head_detached(
            RepositorySafeHandle repo,
            ref GitOid commitish);

        [DllImport(libgit2)]
        internal static extern int git_repository_set_head_detached_from_annotated(
            RepositorySafeHandle repo,
            GitAnnotatedCommitHandle commit);

        [DllImport(libgit2)]
        internal static extern int git_repository_set_head(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string refname);

        [DllImport(libgit2)]
        internal static extern int git_repository_state(
            RepositorySafeHandle repository);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxFilePathNoCleanupMarshaler))]
        internal static extern FilePath git_repository_workdir(RepositorySafeHandle repository);

        [DllImport(libgit2)]
        internal static extern int git_reset(
            RepositorySafeHandle repo,
            GitObjectSafeHandle target,
            ResetMode reset_type,
            ref GitCheckoutOpts opts);

        [DllImport(libgit2)]
        internal static extern int git_revert(
            RepositorySafeHandle repo,
            GitObjectSafeHandle commit,
            GitRevertOpts opts);

        [DllImport(libgit2)]
        internal static extern int git_revparse_ext(
            out GitObjectSafeHandle obj,
            out ReferenceSafeHandle reference,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string spec);

        [DllImport(libgit2)]
        internal static extern void git_revwalk_free(IntPtr walker);

        [DllImport(libgit2)]
        internal static extern int git_revwalk_hide(RevWalkerSafeHandle walker, ref GitOid commit_id);

        [DllImport(libgit2)]
        internal static extern int git_revwalk_new(out RevWalkerSafeHandle walker, RepositorySafeHandle repo);

        [DllImport(libgit2)]
        internal static extern int git_revwalk_next(out GitOid id, RevWalkerSafeHandle walker);

        [DllImport(libgit2)]
        internal static extern int git_revwalk_push(RevWalkerSafeHandle walker, ref GitOid id);

        [DllImport(libgit2)]
        internal static extern void git_revwalk_reset(RevWalkerSafeHandle walker);

        [DllImport(libgit2)]
        internal static extern void git_revwalk_sorting(RevWalkerSafeHandle walk, CommitSortStrategies sort);

        [DllImport(libgit2)]
        internal static extern void git_revwalk_simplify_first_parent(RevWalkerSafeHandle walk);

        [DllImport(libgit2)]
        internal static extern void git_signature_free(IntPtr signature);

        [DllImport(libgit2)]
        internal static extern int git_signature_new(
            out SignatureSafeHandle signature,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string email,
            long time,
            int offset);

        [DllImport(libgit2)]
        internal static extern int git_signature_dup(out IntPtr dest, IntPtr sig);

        [DllImport(libgit2)]
        internal static extern int git_stash_save(
            out GitOid id,
            RepositorySafeHandle repo,
            SignatureSafeHandle stasher,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string message,
            StashModifiers flags);

        internal delegate int git_stash_cb(
            UIntPtr index,
            IntPtr message,
            ref GitOid stash_id,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_stash_foreach(
            RepositorySafeHandle repo,
            git_stash_cb callback,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_stash_drop(RepositorySafeHandle repo, UIntPtr index);

        [DllImport(libgit2)]
        internal static extern int git_status_file(
            out FileStatus statusflags,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath filepath);


        [DllImport(libgit2)]
        internal static extern int git_status_list_new(
            out StatusListSafeHandle git_status_list,
            RepositorySafeHandle repo,
            GitStatusOptions options);

        [DllImport(libgit2)]
        internal static extern int git_status_list_entrycount(
            StatusListSafeHandle statusList);

        [DllImport(libgit2)]
        internal static extern StatusEntrySafeHandle git_status_byindex(
            StatusListSafeHandle list,
            UIntPtr idx);

        [DllImport(libgit2)]
        internal static extern void git_status_list_free(
            IntPtr statusList);

        [DllImport(libgit2)]
        internal static extern void git_strarray_free(
            ref GitStrArray array);

        [DllImport(libgit2)]
        internal static extern int git_submodule_lookup(
            out SubmoduleSafeHandle reference,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath name);

        [DllImport(libgit2)]
        internal static extern int git_submodule_resolve_url(
            GitBuf buf,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string url);

        [DllImport(libgit2)]
        internal static extern int git_submodule_update(
            SubmoduleSafeHandle sm,
            [MarshalAs(UnmanagedType.Bool)] bool init,
            ref GitSubmoduleOptions submoduleUpdateOptions);

        internal delegate int submodule_callback(
            IntPtr sm,
            IntPtr name,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_submodule_foreach(
            RepositorySafeHandle repo,
            submodule_callback callback,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_submodule_add_to_index(
            SubmoduleSafeHandle submodule,
            [MarshalAs(UnmanagedType.Bool)] bool write_index);

        [DllImport(libgit2)]
        internal static extern int git_submodule_save(
            SubmoduleSafeHandle submodule);

        [DllImport(libgit2)]
        internal static extern void git_submodule_free(
            IntPtr submodule);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_submodule_path(
            SubmoduleSafeHandle submodule);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_submodule_url(
            SubmoduleSafeHandle submodule);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_submodule_index_id(
            SubmoduleSafeHandle submodule);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_submodule_head_id(
            SubmoduleSafeHandle submodule);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_submodule_wd_id(
            SubmoduleSafeHandle submodule);

        [DllImport(libgit2)]
        internal static extern SubmoduleIgnore git_submodule_ignore(
            SubmoduleSafeHandle submodule);

        [DllImport(libgit2)]
        internal static extern SubmoduleUpdate git_submodule_update_strategy(
            SubmoduleSafeHandle submodule);

        [DllImport(libgit2)]
        internal static extern SubmoduleRecurse git_submodule_fetch_recurse_submodules(
            SubmoduleSafeHandle submodule);

        [DllImport(libgit2)]
        internal static extern int git_submodule_reload(
            SubmoduleSafeHandle submodule,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2)]
        internal static extern int git_submodule_status(
            out SubmoduleStatus status,
            SubmoduleSafeHandle submodule);

        [DllImport(libgit2)]
        internal static extern int git_submodule_init(
            SubmoduleSafeHandle submodule,
            [MarshalAs(UnmanagedType.Bool)] bool overwrite);

        [DllImport(libgit2)]
        internal static extern int git_tag_annotation_create(
            out GitOid oid,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            GitObjectSafeHandle target,
            SignatureSafeHandle signature,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string message);

        [DllImport(libgit2)]
        internal static extern int git_tag_create(
            out GitOid oid,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            GitObjectSafeHandle target,
            SignatureSafeHandle signature,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string message,
            [MarshalAs(UnmanagedType.Bool)]
            bool force);

        [DllImport(libgit2)]
        internal static extern int git_tag_create_lightweight(
            out GitOid oid,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            GitObjectSafeHandle target,
            [MarshalAs(UnmanagedType.Bool)]
            bool force);

        [DllImport(libgit2)]
        internal static extern int git_tag_delete(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string tagName);

        [DllImport(libgit2)]
        internal static extern int git_tag_list(out GitStrArray array, RepositorySafeHandle repo);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_tag_message(GitObjectSafeHandle tag);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_tag_name(GitObjectSafeHandle tag);

        [DllImport(libgit2)]
        internal static extern IntPtr git_tag_tagger(GitObjectSafeHandle tag);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_tag_target_id(GitObjectSafeHandle tag);

        [DllImport(libgit2)]
        internal static extern GitObjectType git_tag_target_type(GitObjectSafeHandle tag);

        [DllImport(libgit2)]
        internal static extern int git_libgit2_init();

        [DllImport(libgit2)]
        internal static extern int git_libgit2_shutdown();

        [DllImport(libgit2)]
        internal static extern int git_openssl_set_locking();

        internal delegate void git_trace_cb(LogLevel level, IntPtr message);

        [DllImport(libgit2)]
        internal static extern int git_trace_set(LogLevel level, git_trace_cb trace_cb);

        internal delegate int git_transfer_progress_callback(ref GitTransferProgress stats, IntPtr payload);

        internal delegate int git_transport_cb(out IntPtr transport, IntPtr remote, IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_transport_register(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string prefix,
            IntPtr transport_cb,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_transport_smart(
            out IntPtr transport,
            IntPtr remote,
            IntPtr definition);

        [DllImport(libgit2)]
        internal static extern int git_transport_unregister(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string prefix);

        [DllImport(libgit2)]
        internal static extern uint git_tree_entry_filemode(SafeHandle entry);

        [DllImport(libgit2)]
        internal static extern TreeEntrySafeHandle git_tree_entry_byindex(GitObjectSafeHandle tree, UIntPtr idx);

        [DllImport(libgit2)]
        internal static extern int git_tree_entry_bypath(
            out TreeEntrySafeHandle_Owned tree,
            GitObjectSafeHandle root,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath treeentry_path);

        [DllImport(libgit2)]
        internal static extern void git_tree_entry_free(IntPtr treeEntry);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_tree_entry_id(SafeHandle entry);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_tree_entry_name(SafeHandle entry);

        [DllImport(libgit2)]
        internal static extern GitObjectType git_tree_entry_type(SafeHandle entry);

        [DllImport(libgit2)]
        internal static extern UIntPtr git_tree_entrycount(GitObjectSafeHandle tree);

        [DllImport(libgit2)]
        internal static extern int git_treebuilder_new(out TreeBuilderSafeHandle builder, RepositorySafeHandle repo, IntPtr src);

        [DllImport(libgit2)]
        internal static extern int git_treebuilder_insert(
            IntPtr entry_out,
            TreeBuilderSafeHandle builder,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string treeentry_name,
            ref GitOid id,
            uint attributes);

        [DllImport(libgit2)]
        internal static extern int git_treebuilder_write(out GitOid id, TreeBuilderSafeHandle bld);

        [DllImport(libgit2)]
        internal static extern void git_treebuilder_free(IntPtr bld);

        [DllImport(libgit2)]
        internal static extern int git_blob_is_binary(GitObjectSafeHandle blob);

        [DllImport(libgit2)]
        internal static extern int git_cherrypick(RepositorySafeHandle repo, GitObjectSafeHandle commit, GitCherryPickOptions options);
    }
}
// ReSharper restore InconsistentNaming
