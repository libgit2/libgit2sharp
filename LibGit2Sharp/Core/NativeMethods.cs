using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

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

                //TODO: When amd64 version of libgit2.dll is available, value this depending of the size of an IntPtr
                const string currentArchSubPath = "NativeBinaries/x86";

                string path = Path.Combine(Path.GetDirectoryName(originalAssemblypath), currentArchSubPath);

                const string pathEnvVariable = "PATH";
                Environment.SetEnvironmentVariable(pathEnvVariable,
                                                   String.Format("{0}{1}{2}", path, Path.PathSeparator, Environment.GetEnvironmentVariable(pathEnvVariable)));
            }

            AppDomain.CurrentDomain.ProcessExit += ThreadsShutdown;

            git_threads_init();
        }

        private static void ThreadsShutdown(object sender, EventArgs e)
        {
            git_threads_shutdown();
        }

        private static bool IsRunningOnLinux()
        {
            // see http://mono-project.com/FAQ%3a_Technical#Mono_Platforms
            var p = (int)Environment.OSVersion.Platform;
            return (p == 4) || (p == 6) || (p == 128);
        }

        [DllImport(libgit2)]
        public static extern IntPtr git_blob_rawcontent(IntPtr blob);

        [DllImport(libgit2)]
        public static extern int git_blob_rawsize(IntPtr blob);

        [DllImport(libgit2)]
        public static extern IntPtr git_commit_author(IntPtr commit);

        [DllImport(libgit2)]
        public static extern IntPtr git_commit_committer(IntPtr commit);

        [DllImport(libgit2)]
        public static extern int git_commit_create(
            out GitOid oid,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string updateRef,
            GitSignature author,
            GitSignature committer,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string encoding,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string message,
            IntPtr tree,
            int parentCount,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 7)] [In] IntPtr[] parents);

        [DllImport(libgit2)]
        public static extern IntPtr git_commit_message(IntPtr commit);

        [DllImport(libgit2)]
        public static extern IntPtr git_commit_message_encoding(IntPtr commit);

        [DllImport(libgit2)]
        public static extern int git_commit_parent(out IntPtr parentCommit, IntPtr commit, uint n);

        [DllImport(libgit2)]
        public static extern uint git_commit_parentcount(IntPtr commit);

        [DllImport(libgit2)]
        public static extern int git_commit_tree(out IntPtr tree, IntPtr commit);

        [DllImport(libgit2)]
        public static extern IntPtr git_commit_tree_oid(IntPtr commit);

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
            out IntPtr value);

        [DllImport(libgit2)]
        public static extern int git_config_open_global(out ConfigurationSafeHandle cfg);

        [DllImport(libgit2)]
        public static extern int git_config_open_ondisk(
            out ConfigurationSafeHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string path);

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
        public static extern int git_index_add(
            IndexSafeHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string path,
            int stage = 0);

        [DllImport(libgit2)]
        public static extern uint git_index_entrycount(IndexSafeHandle index);

        [DllImport(libgit2)]
        public static extern int git_index_find(
            IndexSafeHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string path);

        [DllImport(libgit2)]
        public static extern void git_index_free(IntPtr index);

        [DllImport(libgit2)]
        public static extern IntPtr git_index_get(IndexSafeHandle index, uint n);

        [DllImport(libgit2)]
        public static extern int git_index_read_tree(IndexSafeHandle index, IntPtr tree);

        [DllImport(libgit2)]
        public static extern int git_index_remove(IndexSafeHandle index, int n);

        [DllImport(libgit2)]
        public static extern int git_index_write(IndexSafeHandle index);

        [DllImport(libgit2)]
        public static extern IntPtr git_lasterror();

        [DllImport(libgit2)]
        public static extern void git_object_free(IntPtr obj);

        [DllImport(libgit2)]
        public static extern IntPtr git_object_id(IntPtr obj);

        [DllImport(libgit2)]
        public static extern int git_object_lookup(out IntPtr obj, RepositorySafeHandle repo, ref GitOid id, GitObjectType type);

        [DllImport(libgit2)]
        public static extern int git_object_lookup_prefix(out IntPtr obj, RepositorySafeHandle repo, ref GitOid id, uint len, GitObjectType type);

        [DllImport(libgit2)]
        public static extern GitObjectType git_object_type(IntPtr obj);

        [DllImport(libgit2)]
        public static extern int git_oid_cmp(ref GitOid a, ref GitOid b);

        [DllImport(libgit2)]
        public static extern int git_reference_create_oid(
            out IntPtr reference,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            ref GitOid oid,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2)]
        public static extern int git_reference_create_symbolic(
            out IntPtr reference,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string target,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2)]
        public static extern int git_reference_delete(IntPtr reference);

        [DllImport(libgit2)]
        public static extern void git_reference_free(IntPtr reference);

        [DllImport(libgit2)]
        public static extern int git_reference_lookup(
            out IntPtr reference,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name);

        [DllImport(libgit2)]
        public static extern IntPtr git_reference_name(IntPtr reference);

        [DllImport(libgit2)]
        public static extern IntPtr git_reference_oid(IntPtr reference);

        [DllImport(libgit2)]
        public static extern int git_reference_rename(
            IntPtr reference,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string newName,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2)]
        public static extern int git_reference_resolve(out IntPtr resolvedReference, IntPtr reference);

        [DllImport(libgit2)]
        public static extern int git_reference_set_oid(IntPtr reference, ref GitOid id);

        [DllImport(libgit2)]
        public static extern int git_reference_set_target(
            IntPtr reference,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string target);

        [DllImport(libgit2)]
        public static extern IntPtr git_reference_target(IntPtr reference);

        [DllImport(libgit2)]
        public static extern GitReferenceType git_reference_type(IntPtr reference);

        [DllImport(libgit2)]
        public static extern void git_remote_free(IntPtr remote);

        [DllImport(libgit2)]
        public static extern int git_remote_load(
            out RemoteSafeHandle remote,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name);

        [DllImport(libgit2)]
        public static extern IntPtr git_remote_name(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        public static extern IntPtr git_remote_url(RemoteSafeHandle remote);

        [DllImport(libgit2)]
        public static extern int git_repository_config(
            out ConfigurationSafeHandle cfg,
            RepositorySafeHandle repo);

        [DllImport(libgit2)]
        public static extern int git_repository_discover(
            byte[] repository_path, // NB: This is more properly a StringBuilder, but it's UTF8
            int size,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string start_path,
            [MarshalAs(UnmanagedType.Bool)] bool across_fs,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string ceiling_dirs);

        [DllImport(libgit2)]
        public static extern void git_repository_free(IntPtr repository);

        [DllImport(libgit2)]
        public static extern int git_repository_head_detached(RepositorySafeHandle repo);

        [DllImport(libgit2)]
        public static extern int git_repository_index(out IndexSafeHandle index, RepositorySafeHandle repo);

        [DllImport(libgit2)]
        public static extern int git_repository_init(
            out RepositorySafeHandle repository,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string path,
            [MarshalAs(UnmanagedType.Bool)] bool isBare);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool git_repository_is_bare(RepositorySafeHandle handle);

        [DllImport(libgit2)]
        public static extern int git_repository_is_empty(RepositorySafeHandle repo);

        [DllImport(libgit2)]
        public static extern int git_repository_open(
            out RepositorySafeHandle repository,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string path);

        [DllImport(libgit2)]
        public static extern IntPtr git_repository_path(RepositorySafeHandle repository);

        [DllImport(libgit2)]
        public static extern IntPtr git_repository_workdir(RepositorySafeHandle repository);

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
            out IntPtr signature,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string email,
            long time,
            int offset);

        [DllImport(libgit2)]
        public static extern int git_status_file(
            out FileStatus statusflags,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string filepath);

        internal delegate int status_callback(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string statuspath,
            uint statusflags,
            IntPtr payload);

        [DllImport(libgit2)]
        public static extern int git_status_foreach(RepositorySafeHandle repo, status_callback callback, IntPtr payload);

        [DllImport(libgit2)]
        public static extern int git_tag_create(
            out GitOid oid,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            IntPtr target,
            GitSignature signature,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string message,
            bool force);

        [DllImport(libgit2)]
        public static extern int git_tag_create_lightweight(
            out GitOid oid,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            IntPtr target,
            bool force);

        [DllImport(libgit2)]
        public static extern int git_tag_delete(
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string tagName);

        [DllImport(libgit2)]
        public static extern IntPtr git_tag_message(IntPtr tag);

        [DllImport(libgit2)]
        public static extern IntPtr git_tag_name(IntPtr tag);

        [DllImport(libgit2)]
        public static extern IntPtr git_tag_tagger(IntPtr tag);

        [DllImport(libgit2)]
        public static extern IntPtr git_tag_target_oid(IntPtr tag);

        [DllImport(libgit2)]
        public static extern void git_threads_init();

        [DllImport(libgit2)]
        public static extern void git_threads_shutdown();

        [DllImport(libgit2)]
        public static extern int git_tree_create_fromindex(out GitOid treeOid, IndexSafeHandle index);

        [DllImport(libgit2)]
        public static extern int git_tree_entry_2object(out IntPtr obj, RepositorySafeHandle repo, IntPtr entry);

        [DllImport(libgit2)]
        public static extern int git_tree_entry_attributes(IntPtr entry);

        [DllImport(libgit2)]
        public static extern IntPtr git_tree_entry_byindex(IntPtr tree, uint idx);

        [DllImport(libgit2)]
        public static extern IntPtr git_tree_entry_byname(
            IntPtr tree,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string filename);

        [DllImport(libgit2)]
        public static extern IntPtr git_tree_entry_id(IntPtr entry);

        [DllImport(libgit2)]
        public static extern IntPtr git_tree_entry_name(IntPtr entry);

        [DllImport(libgit2)]
        public static extern GitObjectType git_tree_entry_type(IntPtr entry);

        [DllImport(libgit2)]
        public static extern uint git_tree_entrycount(IntPtr tree);

        [DllImport(libgit2)]
        public static extern int git_tree_get_subtree(out IntPtr tree, IntPtr root, string treeentry_path);
    }
}
