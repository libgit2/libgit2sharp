﻿using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core.Handles;

// ReSharper disable InconsistentNaming
namespace LibGit2Sharp.Core
{
    internal static class NativeMethods
    {
        public const uint GIT_PATH_MAX = 4096;
        private const string libgit2 = "git2";
        private static readonly LibraryLifetimeObject lifetimeObject;

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
            public LibraryLifetimeObject() { Ensure.Success(NativeMethods.git_threads_init()); }
            ~LibraryLifetimeObject() { NativeMethods.git_threads_shutdown(); }
        }

        static NativeMethods()
        {
            if (!IsRunningOnLinux())
            {
                string originalAssemblypath = new Uri(Assembly.GetExecutingAssembly().EscapedCodeBase).LocalPath;

                string currentArchSubPath = "NativeBinaries/" + ProcessorArchitecture;

                string path = Path.Combine(Path.GetDirectoryName(originalAssemblypath), currentArchSubPath);

                const string pathEnvVariable = "PATH";
                Environment.SetEnvironmentVariable(pathEnvVariable,
                                                   String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", path, Path.PathSeparator, Environment.GetEnvironmentVariable(pathEnvVariable)));
            }

            // See LibraryLifetimeObject description.
            lifetimeObject = new LibraryLifetimeObject();
        }

        public static string ProcessorArchitecture
        {
            get
            {
                if (Compat.Environment.Is64BitProcess)
                {
                    return "amd64";
                }

                return "x86";
            }
        }

        private static bool IsRunningOnLinux()
        {
            // see http://mono-project.com/FAQ%3a_Technical#Mono_Platforms
            var p = (int)Environment.OSVersion.Platform;
            return (p == 4) || (p == 6) || (p == 128);
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
        internal static extern int git_blob_create_fromdisk(
            ref GitOid id,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath path);

        [DllImport(libgit2)]
        internal static extern int git_blob_create_fromworkdir(
            ref GitOid id,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath relative_path);

        internal delegate int source_callback(
            IntPtr content,
            int max_length,
            IntPtr data);

        [DllImport(libgit2)]
        internal static extern int git_blob_create_fromchunks(
            ref GitOid oid,
            RepositorySafeHandle repositoryPtr,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath hintpath,
            source_callback fileCallback,
            IntPtr data);
        
        [DllImport(libgit2)]
        internal static extern IntPtr git_blob_rawcontent(GitObjectSafeHandle blob);

        [DllImport(libgit2)]
        internal static extern Int64 git_blob_rawsize(GitObjectSafeHandle blob);

        [DllImport(libgit2)]
        internal static extern int git_branch_create(
            out ReferenceSafeHandle ref_out,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string branch_name,
            GitObjectSafeHandle target, // TODO: GitCommitSafeHandle?
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2)]
        internal static extern int git_branch_delete(
            ReferenceSafeHandle reference);

        internal delegate int branch_foreach_callback(
            IntPtr branch_name,
            GitBranchType branch_type,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_branch_foreach(
            RepositorySafeHandle repo,
            GitBranchType branch_type,
            branch_foreach_callback branch_cb,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_branch_move(
            ReferenceSafeHandle reference,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string new_branch_name,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2)]
        internal static extern int git_branch_tracking(
            out ReferenceSafeHandle reference,
            ReferenceSafeHandle branch);

        [DllImport(libgit2)]
        internal static extern int git_checkout_tree(
            RepositorySafeHandle repo,
            GitObjectSafeHandle treeish,
            ref GitCheckoutOpts opts);

        [DllImport(libgit2)]
        internal static extern int git_checkout_head(
            RepositorySafeHandle repo,
            ref GitCheckoutOpts opts);

        [DllImport(libgit2)]
        internal static extern int git_checkout_index(
            RepositorySafeHandle repo,
            GitObjectSafeHandle treeish,
            ref GitCheckoutOpts opts);

        [DllImport(libgit2)]
        internal static extern int git_clone(
            out RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string origin_url,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath workdir_path,
            GitCloneOptions opts);

        [DllImport(libgit2)]
        internal static extern IntPtr git_commit_author(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        internal static extern IntPtr git_commit_committer(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        internal static extern int git_commit_create(
            out GitOid id,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string updateRef,
            SignatureSafeHandle author,
            SignatureSafeHandle committer,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string encoding,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string message,
            GitObjectSafeHandle tree,
            int parentCount,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 7)] [In] IntPtr[] parents);

        [DllImport(libgit2)]
        [return : MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8NoCleanupMarshaler))]
        internal static extern string git_commit_message(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8NoCleanupMarshaler))]
        internal static extern string git_commit_message_encoding(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        internal static extern int git_commit_parent(out GitObjectSafeHandle parentCommit, GitObjectSafeHandle commit, uint n);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_commit_parent_id(GitObjectSafeHandle commit, uint n);

        [DllImport(libgit2)]
        internal static extern uint git_commit_parentcount(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_commit_tree_id(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        internal static extern int git_config_delete_entry(ConfigurationSafeHandle cfg, string name);

        [DllImport(libgit2)]
        internal static extern int git_config_find_global(byte[] global_config_path, UIntPtr length);

        [DllImport(libgit2)]
        internal static extern int git_config_find_system(byte[] system_config_path, UIntPtr length);

        [DllImport(libgit2)]
        internal static extern int git_config_find_xdg(byte[] xdg_config_path, UIntPtr length);

        [DllImport(libgit2)]
        internal static extern void git_config_free(IntPtr cfg);

        [DllImport(libgit2)]
        internal static extern int git_config_get_entry(
            out GitConfigEntryHandle entry,
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name);

        [DllImport(libgit2)]
        internal static extern int git_config_add_file_ondisk(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath path,
            uint level,
            bool force);

        [DllImport(libgit2)]
        internal static extern int git_config_new(out ConfigurationSafeHandle cfg);

        [DllImport(libgit2)]
        internal static extern int git_config_open_level(
            out ConfigurationSafeHandle cfg,
            ConfigurationSafeHandle parent,
            uint level);

        [DllImport(libgit2)]
        internal static extern int git_config_open_ondisk(
            out ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath path);

        [DllImport(libgit2)]
        internal static extern int git_config_parse_bool(
            [MarshalAs(UnmanagedType.Bool)] out bool value,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string valueToParse);

        [DllImport(libgit2)]
        internal static extern int git_config_parse_int32(
            [MarshalAs(UnmanagedType.I4)] out int value,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string valueToParse);

        [DllImport(libgit2)]
        internal static extern int git_config_parse_int64(
            [MarshalAs(UnmanagedType.I8)] out long value,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string valueToParse);

        [DllImport(libgit2)]
        internal static extern int git_config_set_bool(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.Bool)] bool value);

        [DllImport(libgit2)]
        internal static extern int git_config_set_int32(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            int value);

        [DllImport(libgit2)]
        internal static extern int git_config_set_int64(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            long value);

        [DllImport(libgit2)]
        internal static extern int git_config_set_string(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string value);

        internal delegate int config_foreach_callback(
            IntPtr entry,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_config_foreach(
            ConfigurationSafeHandle cfg,
            config_foreach_callback callback,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern void git_diff_list_free(IntPtr diff);

        [DllImport(libgit2)]
        internal static extern int git_diff_tree_to_tree(
            out DiffListSafeHandle diff,
            RepositorySafeHandle repo,
            GitObjectSafeHandle oldTree,
            GitObjectSafeHandle newTree,
            GitDiffOptions options);

        [DllImport(libgit2)]
        internal static extern int git_diff_tree_to_index(
            out DiffListSafeHandle diff,
            RepositorySafeHandle repo,
            GitObjectSafeHandle oldTree,
            IndexSafeHandle index,
            GitDiffOptions options);

        [DllImport(libgit2)]
        internal static extern int git_diff_merge(
            DiffListSafeHandle onto,
            DiffListSafeHandle from);

        [DllImport(libgit2)]
        internal static extern int git_diff_index_to_workdir(
            out DiffListSafeHandle diff,
            RepositorySafeHandle repo,
            IndexSafeHandle index,
            GitDiffOptions options);

        [DllImport(libgit2)]
        internal static extern int git_diff_tree_to_workdir(
            out DiffListSafeHandle diff,
            RepositorySafeHandle repo,
            GitObjectSafeHandle oldTree,
            GitDiffOptions options);

        internal delegate int git_diff_file_cb(
            GitDiffDelta delta,
            float progress,
            IntPtr payload);

        internal delegate int git_diff_hunk_cb(
            GitDiffDelta delta,
            GitDiffRange range,
            IntPtr header,
            UIntPtr headerLen,
            IntPtr payload);

        internal delegate int git_diff_data_cb(
            GitDiffDelta delta,
            GitDiffRange range,
            GitDiffLineOrigin lineOrigin,
            IntPtr content,
            UIntPtr contentLen,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_diff_print_patch(
            DiffListSafeHandle diff,
            git_diff_data_cb printCallback,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_diff_blobs(
            GitObjectSafeHandle oldBlob,
            GitObjectSafeHandle newBlob,
            GitDiffOptions options,
            git_diff_file_cb fileCallback,
            git_diff_hunk_cb hunkCallback,
            git_diff_data_cb lineCallback,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_index_add_from_workdir(
            IndexSafeHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath path);

        [DllImport(libgit2)]
        internal static extern int git_index_add(
            IndexSafeHandle index,
            GitIndexEntry entry);

        [DllImport(libgit2)]
        internal static extern UIntPtr git_index_entrycount(IndexSafeHandle index);

        [DllImport(libgit2)]
        internal static extern int git_index_entry_stage(IndexEntrySafeHandle indexentry);

        [DllImport(libgit2)]
        internal static extern int git_index_find(
            IndexSafeHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath path);

        [DllImport(libgit2)]
        internal static extern void git_index_free(IntPtr index);

        [DllImport(libgit2)]
        internal static extern IndexEntrySafeHandle git_index_get_byindex(IndexSafeHandle index, UIntPtr n);

        [DllImport(libgit2)]
        internal static extern IndexEntrySafeHandle git_index_get_bypath(
            IndexSafeHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath path,
            int stage);

        [DllImport(libgit2)]
        internal static extern int git_index_has_conflicts(IndexSafeHandle index);

        [DllImport(libgit2)]
        internal static extern int git_index_open(
            out IndexSafeHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath indexpath);

        [DllImport(libgit2)]
        internal static extern int git_index_read_tree(IndexSafeHandle index, GitObjectSafeHandle tree, IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_index_remove(
            IndexSafeHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath path,
            int stage);

        [DllImport(libgit2)]
        internal static extern int git_index_write(IndexSafeHandle index);

        [DllImport(libgit2)]
        internal static extern int git_index_write_tree(out GitOid treeOid, IndexSafeHandle index);

        [DllImport(libgit2)]
        internal static extern int git_merge_base(
            out GitOid mergeBase,
            RepositorySafeHandle repo,
            GitObjectSafeHandle one,
            GitObjectSafeHandle two);

        [DllImport(libgit2)]
        internal static extern int git_message_prettify(
            byte[] message_out, // NB: This is more properly a StringBuilder, but it's UTF8
            UIntPtr buffer_size,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string message,
            bool strip_comments);

        [DllImport(libgit2)]
        internal static extern int git_note_create(
            out GitOid noteOid,
            RepositorySafeHandle repo,
            SignatureSafeHandle author,
            SignatureSafeHandle committer,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string notes_ref,
            ref GitOid oid,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string note,
            int force);

        [DllImport(libgit2)]
        internal static extern void git_note_free(IntPtr note);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8NoCleanupMarshaler))]
        internal static extern string git_note_message(NoteSafeHandle note);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_note_oid(NoteSafeHandle note);

        [DllImport(libgit2)]
        internal static extern int git_note_read(
            out NoteSafeHandle note,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string notes_ref,
            ref GitOid oid);

        [DllImport(libgit2)]
        internal static extern int git_note_remove(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string notes_ref,
            SignatureSafeHandle author,
            SignatureSafeHandle committer,
            ref GitOid oid);

        [DllImport(libgit2)]
        internal static extern int git_note_default_ref(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8NoCleanupMarshaler))] out string notes_ref,
            RepositorySafeHandle repo);

        internal delegate int git_note_foreach_cb(
            ref GitOid blob_id,
            ref GitOid annotated_object_id,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_note_foreach(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string notes_ref,
            git_note_foreach_cb cb,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_odb_add_backend(ObjectDatabaseSafeHandle odb, IntPtr backend, int priority);

        [DllImport(libgit2)]
        internal static extern IntPtr git_odb_backend_malloc(IntPtr backend, UIntPtr len);

        [DllImport(libgit2)]
        internal static extern int git_odb_exists(ObjectDatabaseSafeHandle odb, ref GitOid id);

        [DllImport(libgit2)]
        internal static extern void git_odb_free(IntPtr odb);

        [DllImport(libgit2)]
        internal static extern void git_object_free(IntPtr obj);

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
        internal static extern GitObjectType git_object_type(GitObjectSafeHandle obj);

        [DllImport(libgit2)]
        internal static extern int git_reference_create(
            out ReferenceSafeHandle reference,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            ref GitOid oid,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2)]
        internal static extern int git_reference_symbolic_create(
            out ReferenceSafeHandle reference,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string target,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2)]
        internal static extern int git_reference_delete(ReferenceSafeHandle reference);

        internal delegate int ref_glob_callback(
            IntPtr reference_name,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_reference_foreach_glob(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string glob,
            GitReferenceType flags,
            ref_glob_callback callback,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern void git_reference_free(IntPtr reference);

        [DllImport(libgit2)]
        internal static extern int git_reference_is_valid_name(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string refname);

        [DllImport(libgit2)]
        internal static extern int git_reference_lookup(
            out ReferenceSafeHandle reference,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8NoCleanupMarshaler))]
        internal static extern string git_reference_name(ReferenceSafeHandle reference);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_reference_target(ReferenceSafeHandle reference);

        [DllImport(libgit2)]
        internal static extern int git_reference_rename(
            ReferenceSafeHandle reference,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string newName,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2)]
        internal static extern int git_reference_resolve(out ReferenceSafeHandle resolvedReference, ReferenceSafeHandle reference);

        [DllImport(libgit2)]
        internal static extern int git_reference_set_target(ReferenceSafeHandle reference, ref GitOid id);

        [DllImport(libgit2)]
        internal static extern int git_reference_symbolic_set_target(
            ReferenceSafeHandle reference,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string target);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8NoCleanupMarshaler))]
        internal static extern string git_reference_symbolic_target(ReferenceSafeHandle reference);

        [DllImport(libgit2)]
        internal static extern GitReferenceType git_reference_type(ReferenceSafeHandle reference);

        [DllImport(libgit2)]
        internal static extern void git_remote_free(IntPtr remote);

        [DllImport(libgit2)]
        internal static extern int git_remote_load(
            out RemoteSafeHandle remote,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8NoCleanupMarshaler))]
        internal static extern string git_remote_name(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        internal static extern int git_remote_create(
            out RemoteSafeHandle remote,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string url);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8NoCleanupMarshaler))]
        internal static extern string git_remote_url(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        internal static extern int git_remote_save(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        internal static extern int git_repository_odb(out ObjectDatabaseSafeHandle odb, RepositorySafeHandle repo);

        [DllImport(libgit2)]
        internal static extern int git_remote_connect(RemoteSafeHandle remote, GitDirection direction);

        [DllImport(libgit2)]
        internal static extern void git_remote_disconnect(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        internal static extern int git_remote_download(
            RemoteSafeHandle remote,
            git_transfer_progress_callback progress_cb,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern void git_remote_set_autotag(RemoteSafeHandle remote, TagFetchMode option);

        [DllImport(libgit2)]
        internal static extern int git_remote_set_fetchspec(
            RemoteSafeHandle remote,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof (Utf8Marshaler))] string fetchrefspec);

        [DllImport(libgit2)]
        internal static extern int git_remote_set_callbacks(
            RemoteSafeHandle remote,
            ref GitRemoteCallbacks callbacks);

        internal delegate void remote_progress_callback(IntPtr str, int len, IntPtr data);

        internal delegate int remote_completion_callback(RemoteCompletionType type, IntPtr data);

        internal delegate int remote_update_tips_callback(
            IntPtr refName,
            ref GitOid oldId,
            ref GitOid newId,
            IntPtr data);

        [DllImport(libgit2)]
        internal static extern int git_remote_update_tips(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        internal static extern int git_repository_discover(
            byte[] repository_path, // NB: This is more properly a StringBuilder, but it's UTF8
            UIntPtr size,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath start_path,
            [MarshalAs(UnmanagedType.Bool)] bool across_fs,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath ceiling_dirs);

        [DllImport(libgit2)]
        internal static extern void git_repository_free(IntPtr repo);

        [DllImport(libgit2)]
        internal static extern int git_repository_head_detached(RepositorySafeHandle repo);

        [DllImport(libgit2)]
        internal static extern int git_repository_head_orphan(RepositorySafeHandle repo);

        [DllImport(libgit2)]
        internal static extern int git_repository_index(out IndexSafeHandle index, RepositorySafeHandle repo);

        [DllImport(libgit2)]
        internal static extern int git_repository_init(
            out RepositorySafeHandle repository,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath path,
            [MarshalAs(UnmanagedType.Bool)] bool isBare);

        [DllImport(libgit2)]
        internal static extern int git_repository_is_bare(RepositorySafeHandle handle);

        [DllImport(libgit2)]
        internal static extern int git_repository_is_empty(RepositorySafeHandle repo);

        [DllImport(libgit2)]
        internal static extern int git_repository_merge_cleanup(RepositorySafeHandle repo);

        internal delegate int git_repository_mergehead_foreach_cb(
            ref GitOid oid,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_repository_mergehead_foreach(
            RepositorySafeHandle repo,
            git_repository_mergehead_foreach_cb cb,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_repository_open(
            out RepositorySafeHandle repository,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath path);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathNoCleanupMarshaler))]
        internal static extern FilePath git_repository_path(RepositorySafeHandle repository);

        [DllImport(libgit2)]
        internal static extern void git_repository_set_config(
            RepositorySafeHandle repository,
            ConfigurationSafeHandle config);

        [DllImport(libgit2)]
        internal static extern void git_repository_set_index(
            RepositorySafeHandle repository,
            IndexSafeHandle index);

        [DllImport(libgit2)]
        internal static extern int git_repository_set_workdir(
            RepositorySafeHandle repository,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath workdir,
            bool update_gitlink);

        [DllImport(libgit2)]
        internal static extern int git_repository_state(
            RepositorySafeHandle repository);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathNoCleanupMarshaler))]
        internal static extern FilePath git_repository_workdir(RepositorySafeHandle repository);

        [DllImport(libgit2)]
        internal static extern int git_reset(
            RepositorySafeHandle repo,
            GitObjectSafeHandle target,
            ResetOptions reset_type);

        [DllImport(libgit2)]
        internal static extern int git_revparse_single(
            out GitObjectSafeHandle obj,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string spec);

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
        internal static extern void git_revwalk_sorting(RevWalkerSafeHandle walk, GitSortOptions sort);

        [DllImport(libgit2)]
        internal static extern void git_signature_free(IntPtr signature);

        [DllImport(libgit2)]
        internal static extern int git_signature_new(
            out SignatureSafeHandle signature,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string email,
            long time,
            int offset);

        [DllImport(libgit2)]
        internal static extern int git_status_file(
            out FileStatus statusflags,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath filepath);

        internal delegate int git_status_cb(
            IntPtr path,
            uint statusflags,
            IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_status_foreach(RepositorySafeHandle repo, git_status_cb cb, IntPtr payload);

        [DllImport(libgit2)]
        internal static extern int git_tag_create(
            out GitOid oid,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            GitObjectSafeHandle target,
            SignatureSafeHandle signature,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string message,
            [MarshalAs(UnmanagedType.Bool)]
            bool force);

        [DllImport(libgit2)]
        internal static extern int git_tag_create_lightweight(
            out GitOid oid,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            GitObjectSafeHandle target,
            [MarshalAs(UnmanagedType.Bool)]
            bool force);

        [DllImport(libgit2)]
        internal static extern int git_tag_delete(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string tagName);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8NoCleanupMarshaler))]
        internal static extern string git_tag_message(GitObjectSafeHandle tag);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8NoCleanupMarshaler))]
        internal static extern string git_tag_name(GitObjectSafeHandle tag);

        [DllImport(libgit2)]
        internal static extern IntPtr git_tag_tagger(GitObjectSafeHandle tag);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_tag_target_id(GitObjectSafeHandle tag);

        [DllImport(libgit2)]
        internal static extern GitObjectType git_tag_target_type(GitObjectSafeHandle tag);

        [DllImport(libgit2)]
        internal static extern int git_threads_init();

        [DllImport(libgit2)]
        internal static extern void git_threads_shutdown();

        internal delegate void git_transfer_progress_callback(ref GitTransferProgress stats, IntPtr payload);

        [DllImport(libgit2)]
        internal static extern uint git_tree_entry_filemode(SafeHandle entry);

        [DllImport(libgit2)]
        internal static extern TreeEntrySafeHandle git_tree_entry_byindex(GitObjectSafeHandle tree, UIntPtr idx);

        [DllImport(libgit2)]
        internal static extern int git_tree_entry_bypath(
            out TreeEntrySafeHandle_Owned tree,
            GitObjectSafeHandle root,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath treeentry_path);

        [DllImport(libgit2)]
        internal static extern void git_tree_entry_free(IntPtr treeEntry);

        [DllImport(libgit2)]
        internal static extern OidSafeHandle git_tree_entry_id(SafeHandle entry);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8NoCleanupMarshaler))]
        internal static extern string git_tree_entry_name(SafeHandle entry);

        [DllImport(libgit2)]
        internal static extern GitObjectType git_tree_entry_type(SafeHandle entry);

        [DllImport(libgit2)]
        internal static extern UIntPtr git_tree_entrycount(GitObjectSafeHandle tree);

        [DllImport(libgit2)]
        internal static extern int git_treebuilder_create(out TreeBuilderSafeHandle builder, IntPtr src);

        [DllImport(libgit2)]
        internal static extern int git_treebuilder_insert(
            IntPtr entry_out,
            TreeBuilderSafeHandle builder,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string treeentry_name,
            ref GitOid id,
            uint attributes);

        [DllImport(libgit2)]
        internal static extern int git_treebuilder_write(out GitOid id, RepositorySafeHandle repo, TreeBuilderSafeHandle bld);

        [DllImport(libgit2)]
        internal static extern void git_treebuilder_free(IntPtr bld);
    }
}
// ReSharper restore InconsistentNaming
