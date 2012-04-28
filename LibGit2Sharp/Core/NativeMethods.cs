using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    internal static class NativeMethods
    {
        public const int GIT_PATH_MAX = 4096;
        private const string libgit2 = "git2";

        static NativeMethods()
        {
            if (!IsRunningOnLinux())
            {
                string originalAssemblypath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;

                string currentArchSubPath = "NativeBinaries/" + ProcessorArchitecture;

                string path = Path.Combine(Path.GetDirectoryName(originalAssemblypath), currentArchSubPath);

                const string pathEnvVariable = "PATH";
                Environment.SetEnvironmentVariable(pathEnvVariable,
                                                   String.Format(CultureInfo.InvariantCulture, "{0}{1}{2}", path, Path.PathSeparator, Environment.GetEnvironmentVariable(pathEnvVariable)));
            }

            git_threads_init();
            AppDomain.CurrentDomain.ProcessExit += ThreadsShutdown;
        }

        private static void ThreadsShutdown(object sender, EventArgs e)
        {
            git_threads_shutdown();
        }

        public static string ProcessorArchitecture
        {
            get
            {
                //TODO: When amd64 version of libgit2.dll is available, uncomment the following lines
                //if (Compat.Environment.Is64BitProcess)
                //{
                //    return "amd64";
                //}

                return "x86";
            }
        }

        private static bool IsRunningOnLinux()
        {
            // see http://mono-project.com/FAQ%3a_Technical#Mono_Platforms
            var p = (int)Environment.OSVersion.Platform;
            return (p == 4) || (p == 6) || (p == 128);
        }

        public static bool RepositoryStateChecker(RepositorySafeHandle repositoryPtr, Func<RepositorySafeHandle, int> checker)
        {
            int res = checker(repositoryPtr);
            Ensure.Success(res, true);

            return (res == 1);
        }

        [DllImport(libgit2)]
        public static extern int git_blob_create_fromfile(
            ref GitOid oid,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath path);

        [DllImport(libgit2)]
        public static extern IntPtr git_blob_rawcontent(GitObjectSafeHandle blob);

        [DllImport(libgit2)]
        public static extern int git_blob_rawsize(GitObjectSafeHandle blob);

        [DllImport(libgit2)]
        public static extern IntPtr git_commit_author(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        public static extern IntPtr git_commit_committer(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        public static extern int git_commit_create(
            out GitOid oid,
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
        [return : MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        public static extern string git_commit_message(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        [return : MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        public static extern string git_commit_message_encoding(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        public static extern int git_commit_parent(out GitObjectSafeHandle parentCommit, GitObjectSafeHandle commit, uint n);

        [DllImport(libgit2)]
        public static extern uint git_commit_parentcount(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        public static extern int git_commit_tree(out GitObjectSafeHandle tree, GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        public static extern OidSafeHandle git_commit_tree_oid(GitObjectSafeHandle commit);

        [DllImport(libgit2)]
        public static extern int git_config_delete(ConfigurationSafeHandle cfg, string name);

        [DllImport(libgit2)]
        public static extern int git_config_find_global(byte[] global_config_path);

        [DllImport(libgit2)]
        public static extern int git_config_find_system(byte[] system_config_path);

        [DllImport(libgit2)]
        public static extern void git_config_free(IntPtr cfg);

        [DllImport(libgit2)]
        public static extern int git_config_get_bool(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.Bool)]
            out bool value);

        [DllImport(libgit2)]
        public static extern int git_config_get_int32(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            out int value);

        [DllImport(libgit2)]
        public static extern int git_config_get_int64(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            out long value);

        [DllImport(libgit2)]
        public static extern int git_config_get_string(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] out string value);

        [DllImport(libgit2)]
        public static extern int git_config_open_global(out ConfigurationSafeHandle cfg);

        [DllImport(libgit2)]
        public static extern int git_config_open_ondisk(
            out ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath path);

        [DllImport(libgit2)]
        public static extern int git_config_set_bool(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.Bool)] bool value);

        [DllImport(libgit2)]
        public static extern int git_config_set_int32(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            int value);

        [DllImport(libgit2)]
        public static extern int git_config_set_int64(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            long value);

        [DllImport(libgit2)]
        public static extern int git_config_set_string(
            ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string value);

        [DllImport(libgit2)]
        public static extern void git_diff_list_free(IntPtr diff);

        [DllImport(libgit2)]
        public static extern int git_diff_tree_to_tree(
            RepositorySafeHandle repo,
            GitDiffOptions options,
            GitObjectSafeHandle oldTree,
            GitObjectSafeHandle newTree,
            out DiffListSafeHandle diff);

        [DllImport(libgit2)]
        public static extern int git_diff_index_to_tree(
            RepositorySafeHandle repo,
            GitDiffOptions options,
            IntPtr oldTree,
            out IntPtr diff);

        [DllImport(libgit2)]
        public static extern int git_diff_workdir_to_index(
            RepositorySafeHandle repo,
            GitDiffOptions options,
            out IntPtr diff);

        [DllImport(libgit2)]
        public static extern int git_diff_workdir_to_tree(
            RepositorySafeHandle repo,
            GitDiffOptions options,
            IntPtr oldTree,
            out IntPtr diff);

        [DllImport(libgit2)]
        public static extern int git_diff_merge(IntPtr onto, IntPtr from);

        internal delegate int git_diff_file_fn(
            IntPtr data,
            GitDiffDelta delta,
            float progress);

        internal delegate int git_diff_hunk_fn(
            IntPtr data,
            GitDiffDelta delta,
            GitDiffRange range,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string header,
            IntPtr headerLen);

        internal delegate int git_diff_line_fn(
            IntPtr data,
            GitDiffDelta delta,
            GitDiffLineOrigin lineOrigin,
            IntPtr content,
            IntPtr contentLen);

        [DllImport(libgit2)]
        public static extern int git_diff_foreach(
            DiffListSafeHandle diff,
            IntPtr callbackData,
            git_diff_file_fn fileCallback,
            git_diff_hunk_fn hunkCallback,
            git_diff_line_fn lineCallback);

        internal delegate int git_diff_output_fn(
            IntPtr data,
            GitDiffLineOrigin lineOrigin,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string formattedOutput);

        [DllImport(libgit2)]
        public static extern int git_diff_print_patch(
            DiffListSafeHandle diff,
            IntPtr data,
            git_diff_output_fn printCallback);

        [DllImport(libgit2)]
        public static extern int git_diff_blobs(
            RepositorySafeHandle repository,
            IntPtr oldBlob,
            IntPtr newBlob,
            GitDiffOptions options,
            object data,
            git_diff_hunk_fn hunkCallback,
            git_diff_line_fn lineCallback);

        [DllImport(libgit2)]
        public static extern int git_index_add(
            IndexSafeHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath path,
            int stage = 0);

        [DllImport(libgit2)]
        public static extern int git_index_add2(
            IndexSafeHandle index,
            GitIndexEntry entry);

        [DllImport(libgit2)]
        public static extern uint git_index_entrycount(IndexSafeHandle index);

        [DllImport(libgit2)]
        public static extern int git_index_find(
            IndexSafeHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath path);

        [DllImport(libgit2)]
        public static extern void git_index_free(IntPtr index);

        [DllImport(libgit2)]
        public static extern IndexEntrySafeHandle git_index_get(IndexSafeHandle index, uint n);

        [DllImport(libgit2)]
        public static extern int git_index_read_tree(IndexSafeHandle index, GitObjectSafeHandle tree);

        [DllImport(libgit2)]
        public static extern int git_index_remove(IndexSafeHandle index, int n);

        [DllImport(libgit2)]
        public static extern int git_index_write(IndexSafeHandle index);

        [DllImport(libgit2)]
        [return : MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        public static extern string git_lasterror();

        [DllImport(libgit2)]
        public static extern int git_odb_exists(ObjectDatabaseSafeHandle odb, ref GitOid id);

        [DllImport(libgit2)]
        public static extern void git_odb_free(IntPtr odb);

        [DllImport(libgit2)]
        public static extern void git_object_free(IntPtr obj);

        [DllImport(libgit2)]
        public static extern OidSafeHandle git_object_id(GitObjectSafeHandle obj);

        [DllImport(libgit2)]
        public static extern int git_object_lookup(out GitObjectSafeHandle obj, RepositorySafeHandle repo, ref GitOid id, GitObjectType type);

        [DllImport(libgit2)]
        public static extern int git_object_lookup_prefix(out GitObjectSafeHandle obj, RepositorySafeHandle repo, ref GitOid id, uint len, GitObjectType type);

        [DllImport(libgit2)]
        public static extern GitObjectType git_object_type(GitObjectSafeHandle obj);

        [DllImport(libgit2)]
        public static extern int git_oid_cmp(ref GitOid a, ref GitOid b);

        [DllImport(libgit2)]
        public static extern int git_reference_create_oid(
            out ReferenceSafeHandle reference,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            ref GitOid oid,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2)]
        public static extern int git_reference_create_symbolic(
            out ReferenceSafeHandle reference,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string target,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2)]
        public static extern int git_reference_delete(ReferenceSafeHandle reference);

        [DllImport(libgit2)]
        public static extern void git_reference_free(IntPtr reference);

        [DllImport(libgit2)]
        public static extern int git_reference_lookup(
            out ReferenceSafeHandle reference,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name);

        [DllImport(libgit2)]
        [return : MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        public static extern string git_reference_name(ReferenceSafeHandle reference);

        [DllImport(libgit2)]
        public static extern OidSafeHandle git_reference_oid(ReferenceSafeHandle reference);

        [DllImport(libgit2)]
        public static extern int git_reference_rename(
            ReferenceSafeHandle reference,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string newName,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2)]
        public static extern int git_reference_resolve(out ReferenceSafeHandle resolvedReference, ReferenceSafeHandle reference);

        [DllImport(libgit2)]
        public static extern int git_reference_set_oid(ReferenceSafeHandle reference, ref GitOid id);

        [DllImport(libgit2)]
        public static extern int git_reference_set_target(
            ReferenceSafeHandle reference,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string target);

        [DllImport(libgit2)]
        [return : MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        public static extern string git_reference_target(ReferenceSafeHandle reference);

        [DllImport(libgit2)]
        public static extern GitReferenceType git_reference_type(ReferenceSafeHandle reference);

        [DllImport(libgit2)]
        public static extern void git_remote_free(IntPtr remote);

        [DllImport(libgit2)]
        public static extern int git_remote_load(
            out RemoteSafeHandle remote,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name);

        [DllImport(libgit2)]
        [return : MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        public static extern string git_remote_name(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        public static extern int git_remote_new(
            out RemoteSafeHandle remote,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string url,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name);

        [DllImport(libgit2)]
        [return : MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        public static extern string git_remote_url(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        public static extern int git_remote_save(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        public static extern int git_repository_config(
            out ConfigurationSafeHandle cfg,
            RepositorySafeHandle repo);

        [DllImport(libgit2)]
        public static extern int git_repository_odb(out ObjectDatabaseSafeHandle odb, RepositorySafeHandle repo);

        [DllImport(libgit2)]
        public static extern int git_repository_discover(
            byte[] repository_path, // NB: This is more properly a StringBuilder, but it's UTF8
            int size,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath start_path,
            [MarshalAs(UnmanagedType.Bool)] bool across_fs,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath ceiling_dirs);

        [DllImport(libgit2)]
        public static extern void git_repository_free(IntPtr repository);

        [DllImport(libgit2)]
        public static extern int git_repository_head_detached(RepositorySafeHandle repo);

        [DllImport(libgit2)]
        public static extern int git_repository_index(out IndexSafeHandle index, RepositorySafeHandle repo);

        [DllImport(libgit2)]
        public static extern int git_repository_init(
            out RepositorySafeHandle repository,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath path,
            [MarshalAs(UnmanagedType.Bool)] bool isBare);

        [DllImport(libgit2)]
        public static extern int git_repository_is_bare(RepositorySafeHandle handle);

        [DllImport(libgit2)]
        public static extern int git_repository_is_empty(RepositorySafeHandle repo);

        [DllImport(libgit2)]
        public static extern int git_repository_open(
            out RepositorySafeHandle repository,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath path);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))]
        public static extern FilePath git_repository_path(RepositorySafeHandle repository);

        [DllImport(libgit2)]
        [return : MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))]
        public static extern FilePath git_repository_workdir(RepositorySafeHandle repository);

        [DllImport(libgit2)]
        public static extern void git_revwalk_free(IntPtr walker);

        [DllImport(libgit2)]
        public static extern int git_revwalk_hide(RevWalkerSafeHandle walker, ref GitOid oid);

        [DllImport(libgit2)]
        public static extern int git_revwalk_new(out RevWalkerSafeHandle walker, RepositorySafeHandle repo);

        [DllImport(libgit2)]
        public static extern int git_revwalk_next(out GitOid oid, RevWalkerSafeHandle walker);

        [DllImport(libgit2)]
        public static extern int git_revwalk_push(RevWalkerSafeHandle walker, ref GitOid oid);

        [DllImport(libgit2)]
        public static extern void git_revwalk_reset(RevWalkerSafeHandle walker);

        [DllImport(libgit2)]
        public static extern void git_revwalk_sorting(RevWalkerSafeHandle walk, GitSortOptions sort);

        [DllImport(libgit2)]
        public static extern void git_signature_free(IntPtr signature);

        [DllImport(libgit2)]
        public static extern int git_signature_new(
            out SignatureSafeHandle signature,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string email,
            long time,
            int offset);

        [DllImport(libgit2)]
        public static extern int git_status_file(
            out FileStatus statusflags,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath filepath);

        internal delegate int status_callback(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath statuspath,
            uint statusflags,
            IntPtr payload);

        [DllImport(libgit2)]
        public static extern int git_status_foreach(RepositorySafeHandle repo, status_callback callback, IntPtr payload);

        [DllImport(libgit2)]
        public static extern int git_tag_create(
            out GitOid oid,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            GitObjectSafeHandle target,
            SignatureSafeHandle signature,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string message,
            [MarshalAs(UnmanagedType.Bool)]
            bool force);

        [DllImport(libgit2)]
        public static extern int git_tag_create_lightweight(
            out GitOid oid,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            GitObjectSafeHandle target,
            [MarshalAs(UnmanagedType.Bool)]
            bool force);

        [DllImport(libgit2)]
        public static extern int git_tag_delete(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string tagName);

        [DllImport(libgit2)]
        [return : MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        public static extern string git_tag_message(GitObjectSafeHandle tag);

        [DllImport(libgit2)]
        [return : MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        public static extern string git_tag_name(GitObjectSafeHandle tag);

        [DllImport(libgit2)]
        public static extern IntPtr git_tag_tagger(GitObjectSafeHandle tag);

        [DllImport(libgit2)]
        public static extern OidSafeHandle git_tag_target_oid(GitObjectSafeHandle tag);

        [DllImport(libgit2)]
        public static extern void git_threads_init();

        [DllImport(libgit2)]
        public static extern void git_threads_shutdown();

        [DllImport(libgit2)]
        public static extern int git_tree_create_fromindex(out GitOid treeOid, IndexSafeHandle index);

        [DllImport(libgit2)]
        public static extern int git_tree_entry_2object(out GitObjectSafeHandle obj, RepositorySafeHandle repo, TreeEntrySafeHandle entry);

        [DllImport(libgit2)]
        public static extern uint git_tree_entry_attributes(TreeEntrySafeHandle entry);

        [DllImport(libgit2)]
        public static extern TreeEntrySafeHandle git_tree_entry_byindex(GitObjectSafeHandle tree, uint idx);

        [DllImport(libgit2)]
        public static extern TreeEntrySafeHandle git_tree_entry_byname(
            GitObjectSafeHandle tree,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath filename);

        [DllImport(libgit2)]
        public static extern OidSafeHandle git_tree_entry_id(TreeEntrySafeHandle entry);

        [DllImport(libgit2)]
        [return : MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        public static extern string git_tree_entry_name(TreeEntrySafeHandle entry);

        [DllImport(libgit2)]
        public static extern GitObjectType git_tree_entry_type(TreeEntrySafeHandle entry);

        [DllImport(libgit2)]
        public static extern uint git_tree_entrycount(GitObjectSafeHandle tree);

        [DllImport(libgit2)]
        public static extern int git_tree_get_subtree(out GitObjectSafeHandle tree, GitObjectSafeHandle root,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(FilePathMarshaler))] FilePath treeentry_path);

        [DllImport(libgit2)]
        public static extern int git_treebuilder_create(out TreeBuilderSafeHandle builder, IntPtr src);

        [DllImport(libgit2)]
        public static extern int git_treebuilder_insert(
            IntPtr entry_out,
            TreeBuilderSafeHandle builder,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string treeentry_name,
            ref GitOid id,
            uint attributes);

        [DllImport(libgit2)]
        public static extern int git_treebuilder_write(out GitOid oid, RepositorySafeHandle repo, TreeBuilderSafeHandle bld);

        [DllImport(libgit2)]
        public static extern int git_treebuilder_free(IntPtr bld);

        [DllImport(libgit2)]
        public static extern int git_note_read(
            out NoteSafeHandle note,
            RepositorySafeHandle repo,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string notes_ref,
            ref GitOid oid);

        [DllImport(libgit2)]
        [return : MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))]
        public static extern string git_note_message(NoteSafeHandle note);

        [DllImport(libgit2)]
        public static extern OidSafeHandle git_note_oid(NoteSafeHandle note);

        [DllImport(libgit2)]
        public static extern int git_note_create(
            out GitOid noteOid,
            RepositorySafeHandle repo,
		    SignatureSafeHandle author,
            SignatureSafeHandle committer,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string notes_ref,
            ref GitOid oid,
		    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string note);

        [DllImport(libgit2)]
        public static extern int git_note_remove(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string notes_ref,
		    SignatureSafeHandle author, 
            SignatureSafeHandle committer,
		    ref GitOid oid);

        [DllImport(libgit2)]
        public static extern void git_note_free(IntPtr note);
    }
}
