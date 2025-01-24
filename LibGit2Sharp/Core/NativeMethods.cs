using System;
using System.IO;
#if NET
using System.Reflection;
#endif
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core.Handles;

// Restrict the set of directories where the native library is loaded from to safe directories.
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.ApplicationDirectory | DllImportSearchPath.SafeDirectories)]

namespace LibGit2Sharp.Core
{
    internal static class NativeMethods
    {
        public const uint GIT_PATH_MAX = 4096;
        private const string libgit2 = NativeDllName.Name;

        // An object tied to the lifecycle of the NativeMethods static class.
        // This will handle initialization and shutdown of the underlying
        // native library.
        private static NativeShutdownObject shutdownObject;

        static NativeMethods()
        {
            if (Platform.IsRunningOnNetFramework() || Platform.IsRunningOnNetCore())
            {
                // Use NativeLibrary when available.
                if (!TryUseNativeLibrary())
                {
                    // NativeLibrary is not available, fall back.

                    // Use GlobalSettings.NativeLibraryPath when set.
                    // Try to load the .dll from the path explicitly.
                    // If this call succeeds further DllImports will find the library loaded and not attempt to load it again.
                    // If it fails the next DllImport will load the library from safe directories.
                    string nativeLibraryPath = GetGlobalSettingsNativeLibraryPath();

                    if (nativeLibraryPath != null)
                    {
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))

                        {
                            LoadWindowsLibrary(nativeLibraryPath);
                        }
                        else
                        {
                            LoadUnixLibrary(nativeLibraryPath, RTLD_NOW);
                        }
                    }
                }
            }

            InitializeNativeLibrary();
        }

        private static string GetGlobalSettingsNativeLibraryPath()
        {
            string nativeLibraryDir = GlobalSettings.GetAndLockNativeLibraryPath();

            if (nativeLibraryDir == null)
            {
                return null;
            }

            return Path.Combine(nativeLibraryDir, libgit2 + Platform.GetNativeLibraryExtension());
        }

#if NETFRAMEWORK
        private static bool TryUseNativeLibrary() => false;
#else
        private static bool TryUseNativeLibrary()
        {
            NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, ResolveDll);

            return true;
        }

        private static IntPtr ResolveDll(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            IntPtr handle = IntPtr.Zero;

            if (libraryName == libgit2)
            {
                // Use GlobalSettings.NativeLibraryPath when set.
                string nativeLibraryPath = GetGlobalSettingsNativeLibraryPath();

                if (nativeLibraryPath != null && NativeLibrary.TryLoad(nativeLibraryPath, out handle))
                {
                    return handle;
                }

                // Use Default DllImport resolution.
                if (NativeLibrary.TryLoad(libraryName, assembly, searchPath, out handle))
                {
                    return handle;
                }

                // We carry a number of .so files for Linux which are linked against various
                // libc/OpenSSL libraries. Try them out.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // The libraries are located at 'runtimes/<rid>/native/lib{libraryName}.so'
                    // The <rid> ends with the processor architecture. e.g. fedora-x64.
                    string assemblyDirectory = Path.GetDirectoryName(AppContext.BaseDirectory);
                    string processorArchitecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
                    string runtimesDirectory = Path.Combine(assemblyDirectory, "runtimes");

                    if (Directory.Exists(runtimesDirectory))
                    {
                        foreach (var runtimeFolder in Directory.GetDirectories(runtimesDirectory, $"*-{processorArchitecture}"))
                        {
                            string libPath = Path.Combine(runtimeFolder, "native", $"lib{libraryName}.so");

                            if (NativeLibrary.TryLoad(libPath, out handle))
                            {
                                return handle;
                            }
                        }
                    }
                }
            }

            return handle;
        }
#endif

        public const int RTLD_NOW = 0x002;

        [DllImport("libdl", EntryPoint = "dlopen")]
        private static extern IntPtr LoadUnixLibrary(string path, int flags);

        [DllImport("kernel32", EntryPoint = "LoadLibrary")]
        private static extern IntPtr LoadWindowsLibrary(string path);

        // Avoid inlining this method because otherwise mono's JITter may try
        // to load the library _before_ we've configured the path.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InitializeNativeLibrary()
        {
            int initCounter;
            try
            {
            }
            finally // avoid thread aborts
            {
                // Initialization can be called multiple times as long as there is a corresponding shutdown to each initialization.
                initCounter = git_libgit2_init();
                shutdownObject = new NativeShutdownObject();
            }

            // Configure the OpenSSL locking on the first initialization of the library in the current process.
            if (initCounter == 1)
            {
                git_openssl_set_locking();
            }
        }

        // Shutdown the native library in a finalizer.
        private sealed class NativeShutdownObject : CriticalFinalizerObject
        {
            ~NativeShutdownObject()
            {
                git_libgit2_shutdown();
            }
        }

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe GitError* git_error_last();

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_error_set_str(
            GitErrorCategory error_class,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string errorString);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void git_error_set_oom();

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe uint git_blame_get_hunk_count(BlameHandle blame);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_blame_hunk* git_blame_get_hunk_byindex(BlameHandle blame, uint index);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_blame_file(
            out BlameHandle blame,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string path,
            git_blame_options options);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_blame_free(nint blame);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_blob_create_from_disk(
            ref GitOid id,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_blob_create_from_workdir(
            ref GitOid id,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath relative_path);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_blob_create_from_stream(
            out IntPtr stream,
            RepositoryHandle repositoryPtr,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string hintpath);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_blob_create_from_stream_commit(
            ref GitOid oid,
            IntPtr stream);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_blob_filtered_content(
            GitBuf buf,
            ObjectHandle blob,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string as_path,
            [MarshalAs(UnmanagedType.Bool)] bool check_for_binary_data);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe IntPtr git_blob_rawcontent(ObjectHandle blob);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe long git_blob_rawsize(ObjectHandle blob);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_branch_create_from_annotated(
            out ReferenceHandle ref_out,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string branch_name,
            AnnotatedCommitHandle target,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_branch_delete(
            ReferenceHandle reference);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int branch_foreach_callback(
            IntPtr branch_name,
            GitBranchType branch_type,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void git_branch_iterator_free(
            IntPtr iterator);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_branch_iterator_new(
            out IntPtr iter_out,
            RepositoryHandle repo,
            GitBranchType branch_type);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_branch_move(
            out ReferenceHandle ref_out,
            ReferenceHandle reference,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string new_branch_name,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_branch_next(
            out IntPtr ref_out,
            out GitBranchType type_out,
            IntPtr iter);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_branch_remote_name(
            GitBuf buf,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string canonical_branch_name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int commit_signing_callback(
            IntPtr signature,
            IntPtr signature_field,
            IntPtr commit_content,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_rebase_init(
            out RebaseHandle rebase,
            RepositoryHandle repo,
            AnnotatedCommitHandle branch,
            AnnotatedCommitHandle upstream,
            AnnotatedCommitHandle onto,
            GitRebaseOptions options);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_rebase_open(
            out RebaseHandle rebase,
            RepositoryHandle repo,
            GitRebaseOptions options);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe UIntPtr git_rebase_operation_entrycount(
            RebaseHandle rebase);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe UIntPtr git_rebase_operation_current(
            RebaseHandle rebase);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_rebase_operation* git_rebase_operation_byindex(
            RebaseHandle rebase,
            UIntPtr index);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_rebase_next(
            out git_rebase_operation* operation,
            RebaseHandle rebase);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_rebase_commit(
            ref GitOid id,
            RebaseHandle rebase,
            SignatureHandle author,
            SignatureHandle committer,
            IntPtr message_encoding,
            IntPtr message);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_rebase_abort(
            RebaseHandle rebase);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_rebase_finish(
            RebaseHandle repo,
            SignatureHandle signature);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_rebase_free(nint rebase);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_rename(
            ref GitStrArray problems,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string old_name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string new_name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_remote_rename_problem_cb(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string problematic_refspec,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_branch_upstream_name(
            GitBuf buf,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string referenceName);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void git_buf_dispose(GitBuf buf);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_checkout_tree(
            RepositoryHandle repo,
            ObjectHandle treeish,
            ref GitCheckoutOpts opts);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_checkout_index(
            RepositoryHandle repo,
            ObjectHandle treeish,
            ref GitCheckoutOpts opts);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_clone(
            out RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string origin_url,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath workdir_path,
            ref GitCloneOptions opts);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe SignatureHandle git_commit_author(ObjectHandle commit);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe SignatureHandle git_commit_committer(ObjectHandle commit);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_commit_create_from_ids(
            out GitOid id,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string updateRef,
            SignatureHandle author,
            SignatureHandle committer,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string encoding,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string message,
            ref GitOid tree,
            UIntPtr parentCount,
            [MarshalAs(UnmanagedType.LPArray)][In] IntPtr[] parents);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_commit_create_buffer(
            GitBuf res,
            RepositoryHandle repo,
            SignatureHandle author,
            SignatureHandle committer,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string encoding,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string message,
            ObjectHandle tree,
            UIntPtr parent_count,
            IntPtr* parents /* git_commit** originally */);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_commit_create_with_signature(
            out GitOid id,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string commit_content,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string signature,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string signature_field);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern unsafe string git_commit_message(ObjectHandle commit);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern unsafe string git_commit_summary(ObjectHandle commit);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern unsafe string git_commit_message_encoding(ObjectHandle commit);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_oid* git_commit_parent_id(ObjectHandle commit, uint n);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe uint git_commit_parentcount(ObjectHandle commit);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_oid* git_commit_tree_id(ObjectHandle commit);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_commit_extract_signature(
            GitBuf signature,
            GitBuf signed_data,
            RepositoryHandle repo,
            ref GitOid commit_id,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string field);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_config_delete_entry(
            ConfigurationHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_config_lock(out IntPtr txn, ConfigurationHandle config);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_config_delete_multivar(
            ConfigurationHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string regexp);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_config_set_multivar(
            ConfigurationHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string regexp,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string value);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_config_find_global(GitBuf global_config_path);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_config_find_system(GitBuf system_config_path);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_config_find_xdg(GitBuf xdg_config_path);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_config_find_programdata(GitBuf programdata_config_path);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_config_free(nint cfg);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_config_entry_free(GitConfigEntry* entry);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_config_get_entry(
            out GitConfigEntry* entry,
            ConfigurationHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_config_add_file_ondisk(
            ConfigurationHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path,
            uint level,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_config_new(out ConfigurationHandle cfg);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_config_open_level(
            out ConfigurationHandle cfg,
            ConfigurationHandle parent,
            uint level);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_config_parse_bool(
            [MarshalAs(UnmanagedType.Bool)] out bool value,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string valueToParse);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_config_parse_int32(
            [MarshalAs(UnmanagedType.I4)] out int value,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string valueToParse);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_config_parse_int64(
            [MarshalAs(UnmanagedType.I8)] out long value,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string valueToParse);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_config_set_bool(
            ConfigurationHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.Bool)] bool value);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_config_set_int32(
            ConfigurationHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            int value);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_config_set_int64(
            ConfigurationHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            long value);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_config_set_string(
            ConfigurationHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int config_foreach_callback(
            IntPtr entry,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_config_foreach(
            ConfigurationHandle cfg,
            config_foreach_callback callback,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_config_iterator_glob_new(
            out IntPtr iter,
            ConfigurationHandle cfg,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string regexp);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_config_next(
            out IntPtr entry,
            IntPtr iter);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void git_config_iterator_free(IntPtr iter);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_config_snapshot(out ConfigurationHandle @out, ConfigurationHandle config);

        // Ordinarily we would decorate the `url` parameter with the StrictUtf8Marshaler like we do everywhere
        // else, but apparently doing a native->managed callback with the 64-bit version of CLR 2.0 can
        // sometimes vomit when using a custom IMarshaler.  So yeah, don't do that.  If you need the url,
        // call StrictUtf8Marshaler.FromNative manually.  See the discussion here:
        // http://social.msdn.microsoft.com/Forums/en-US/netfx64bit/thread/1eb746c6-d695-4632-8a9e-16c4fa98d481
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_cred_acquire_cb(
            out IntPtr cred,
            IntPtr url,
            IntPtr username_from_url,
            GitCredentialType allowed_types,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_cred_default_new(out IntPtr cred);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_cred_userpass_plaintext_new(
            out IntPtr cred,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string username,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string password);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void git_cred_free(IntPtr cred);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_describe_commit(
            out DescribeResultHandle describe,
            ObjectHandle committish,
            ref GitDescribeOptions options);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_describe_format(
            GitBuf buf,
            DescribeResultHandle describe,
            ref GitDescribeFormatOptions options);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_describe_result_free(nint describe);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_diff_free(nint diff);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_diff_tree_to_tree(
            out DiffHandle diff,
            RepositoryHandle repo,
            ObjectHandle oldTree,
            ObjectHandle newTree,
            GitDiffOptions options);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_diff_tree_to_index(
            out DiffHandle diff,
            RepositoryHandle repo,
            ObjectHandle oldTree,
            IndexHandle index,
            GitDiffOptions options);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_diff_merge(
            DiffHandle onto,
            DiffHandle from);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_diff_index_to_workdir(
            out DiffHandle diff,
            RepositoryHandle repo,
            IndexHandle index,
            GitDiffOptions options);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_diff_tree_to_workdir(
            out DiffHandle diff,
            RepositoryHandle repo,
            ObjectHandle oldTree,
            GitDiffOptions options);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate int git_diff_file_cb(
            [In] git_diff_delta* delta,
            float progress,
            IntPtr payload);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate int git_diff_hunk_cb(
            [In] git_diff_delta* delta,
            [In] GitDiffHunk hunk,
            IntPtr payload);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate int git_diff_line_cb(
            [In] git_diff_delta* delta,
            [In] GitDiffHunk hunk,
            [In] GitDiffLine line,
            IntPtr payload);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate int git_diff_binary_cb(
            [In] git_diff_delta* delta,
            [In] GitDiffBinary binary,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_diff_blobs(
            ObjectHandle oldBlob,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string old_as_path,
            ObjectHandle newBlob,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string new_as_path,
            GitDiffOptions options,
            git_diff_file_cb fileCallback,
            git_diff_binary_cb binaryCallback,
            git_diff_hunk_cb hunkCallback,
            git_diff_line_cb lineCallback,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_diff_foreach(
            DiffHandle diff,
            git_diff_file_cb fileCallback,
            git_diff_binary_cb binaryCallback,
            git_diff_hunk_cb hunkCallback,
            git_diff_line_cb lineCallback,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_diff_find_similar(
            DiffHandle diff,
            GitDiffFindOptions options);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe UIntPtr git_diff_num_deltas(DiffHandle diff);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_diff_delta* git_diff_get_delta(DiffHandle diff, UIntPtr idx);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_filter_register(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            IntPtr gitFilter, int priority);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_filter_unregister(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_filter_source_mode(git_filter_source* source);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_libgit2_features();

        #region git_libgit2_opts

        // Bindings for git_libgit2_opts(int option, ...):
        // Currently only GIT_OPT_GET_SEARCH_PATH and GIT_OPT_SET_SEARCH_PATH are supported,
        // but other overloads could be added using a similar pattern.
        // CallingConvention.Cdecl is used to allow binding the the C varargs signature, and each possible call signature must be enumerated.
        // __argslist was an option, but is an undocumented feature that should likely not be used here.

        // git_libgit2_opts(GIT_OPT_GET_SEARCH_PATH, int level, git_buf *buf)
        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_libgit2_opts(int option, uint level, GitBuf buf);

        // git_libgit2_opts(GIT_OPT_SET_SEARCH_PATH, int level, const char *path)
        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_libgit2_opts(int option, uint level,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string path);

        // git_libgit2_opts(GIT_OPT_ENABLE_*, int enabled)
        // git_libgit2_opts(GIT_OPT_SET_OWNER_VALIDATION, int enabled)
        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_libgit2_opts(int option, int enabled);

        // git_libgit2_opts(GIT_OPT_SET_USER_AGENT, const char *path)
        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_libgit2_opts(int option,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string path);

        // git_libgit2_opts(GIT_OPT_GET_USER_AGENT, git_buf *buf)
        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_libgit2_opts(int option, GitBuf buf);

        // git_libgit2_opts(GIT_OPT_SET_EXTENSIONS, const char **extensions, size_t len)
        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_libgit2_opts(int option, IntPtr extensions, UIntPtr len);

        // git_libgit2_opts(GIT_OPT_GET_EXTENSIONS, git_strarray *out)
        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_libgit2_opts(int option, out GitStrArray extensions);

        // git_libgit2_opts(GIT_OPT_GET_OWNER_VALIDATION, int *enabled)
        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_libgit2_opts(int option, int* enabled);
        #endregion

        #region git_libgit2_opts_osxarm64

        // For RID osx-arm64 the calling convention is different: we need to pad out to 8 arguments before varargs
        // (see discussion at https://github.com/dotnet/runtime/issues/48796)

        // git_libgit2_opts(GIT_OPT_GET_SEARCH_PATH, int level, git_buf *buf)
        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl, EntryPoint = "git_libgit2_opts")]
        internal static extern int git_libgit2_opts_osxarm64(int option, IntPtr nop2, IntPtr nop3, IntPtr nop4, IntPtr nop5, IntPtr nop6, IntPtr nop7, IntPtr nop8, uint level, GitBuf buf);

        // git_libgit2_opts(GIT_OPT_SET_SEARCH_PATH, int level, const char *path)
        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl, EntryPoint = "git_libgit2_opts")]
        internal static extern int git_libgit2_opts_osxarm64(int option, IntPtr nop2, IntPtr nop3, IntPtr nop4, IntPtr nop5, IntPtr nop6, IntPtr nop7, IntPtr nop8, uint level,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string path);

        // git_libgit2_opts(GIT_OPT_ENABLE_*, int enabled)
        // git_libgit2_opts(GIT_OPT_SET_OWNER_VALIDATION, int enabled)
        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl, EntryPoint = "git_libgit2_opts")]
        internal static extern int git_libgit2_opts_osxarm64(int option, IntPtr nop2, IntPtr nop3, IntPtr nop4, IntPtr nop5, IntPtr nop6, IntPtr nop7, IntPtr nop8, int enabled);

        // git_libgit2_opts(GIT_OPT_SET_USER_AGENT, const char *path)
        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl, EntryPoint = "git_libgit2_opts")]
        internal static extern int git_libgit2_opts_osxarm64(int option, IntPtr nop2, IntPtr nop3, IntPtr nop4, IntPtr nop5, IntPtr nop6, IntPtr nop7, IntPtr nop8,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string path);

        // git_libgit2_opts(GIT_OPT_GET_USER_AGENT, git_buf *buf)
        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl, EntryPoint = "git_libgit2_opts")]
        internal static extern int git_libgit2_opts_osxarm64(int option, IntPtr nop2, IntPtr nop3, IntPtr nop4, IntPtr nop5, IntPtr nop6, IntPtr nop7, IntPtr nop8, GitBuf buf);

        // git_libgit2_opts(GIT_OPT_SET_EXTENSIONS, const char **extensions, size_t len)
        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl, EntryPoint = "git_libgit2_opts")]
        internal static extern int git_libgit2_opts_osxarm64(int option, IntPtr nop2, IntPtr nop3, IntPtr nop4, IntPtr nop5, IntPtr nop6, IntPtr nop7, IntPtr nop8, IntPtr extensions, UIntPtr len);

        // git_libgit2_opts(GIT_OPT_GET_EXTENSIONS, git_strarray *out)
        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl, EntryPoint = "git_libgit2_opts")]
        internal static extern int git_libgit2_opts_osxarm64(int option, IntPtr nop2, IntPtr nop3, IntPtr nop4, IntPtr nop5, IntPtr nop6, IntPtr nop7, IntPtr nop8, out GitStrArray extensions);

        // git_libgit2_opts(GIT_OPT_GET_OWNER_VALIDATION, int *enabled)
        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl, EntryPoint = "git_libgit2_opts")]
        internal static extern unsafe int git_libgit2_opts_osxarm64(int option, IntPtr nop2, IntPtr nop3, IntPtr nop4, IntPtr nop5, IntPtr nop6, IntPtr nop7, IntPtr nop8, int* enabled);
        #endregion

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_graph_ahead_behind(out UIntPtr ahead, out UIntPtr behind, RepositoryHandle repo, ref GitOid one, ref GitOid two);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_graph_descendant_of(
            RepositoryHandle repo,
            ref GitOid commit,
            ref GitOid ancestor);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_ignore_add_rule(
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string rules);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_ignore_clear_internal_rules(RepositoryHandle repo);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_ignore_path_is_ignored(
            out int ignored,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string path);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_index_add_bypath(
            IndexHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_index_add(
            IndexHandle index,
            git_index_entry* entry);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_index_conflict_get(
            out git_index_entry* ancestor,
            out git_index_entry* ours,
            out git_index_entry* theirs,
            IndexHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string path);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_index_conflict_iterator_new(
            out ConflictIteratorHandle iterator,
            IndexHandle index);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_index_conflict_next(
            out git_index_entry* ancestor,
            out git_index_entry* ours,
            out git_index_entry* theirs,
            ConflictIteratorHandle iterator);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_index_conflict_iterator_free(nint iterator);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe UIntPtr git_index_entrycount(IndexHandle index);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_index_entry_stage(git_index_entry* indexentry);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_index_free(nint index);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_index_entry* git_index_get_byindex(IndexHandle index, UIntPtr n);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_index_entry* git_index_get_bypath(
            IndexHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string path,
            int stage);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_index_has_conflicts(IndexHandle index);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe UIntPtr git_index_name_entrycount(IndexHandle handle);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_index_name_entry* git_index_name_get_byindex(IndexHandle handle, UIntPtr n);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_index_open(
            out IndexHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath indexpath);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_index_read(
            IndexHandle index,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_index_remove_bypath(
            IndexHandle index,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string path);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe UIntPtr git_index_reuc_entrycount(IndexHandle handle);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_index_reuc_entry* git_index_reuc_get_byindex(IndexHandle handle, UIntPtr n);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_index_reuc_entry* git_index_reuc_get_bypath(
            IndexHandle handle,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string path);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_index_write(IndexHandle index);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_index_write_tree(out GitOid treeOid, IndexHandle index);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_index_write_tree_to(out GitOid treeOid, IndexHandle index, RepositoryHandle repo);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_index_read_tree(IndexHandle index, ObjectHandle tree);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_index_clear(IndexHandle index);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_merge_base_many(
            out GitOid mergeBase,
            RepositoryHandle repo,
            int length,
            [In] GitOid[] input_array);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_merge_base_octopus(
            out GitOid mergeBase,
            RepositoryHandle repo,
            int length,
            [In] GitOid[] input_array);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_annotated_commit_from_ref(
            out AnnotatedCommitHandle annotatedCommit,
            RepositoryHandle repo,
            ReferenceHandle reference);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_annotated_commit_from_fetchhead(
            out AnnotatedCommitHandle annotatedCommit,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string branch_name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string remote_url,
            ref GitOid oid);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_annotated_commit_from_revspec(
            out AnnotatedCommitHandle annotatedCommit,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string revspec);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_annotated_commit_lookup(
            out AnnotatedCommitHandle annotatedCommit,
            RepositoryHandle repo,
            ref GitOid id);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_oid* git_annotated_commit_id(
            AnnotatedCommitHandle annotatedCommit);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_merge(
            RepositoryHandle repo,
            [In] IntPtr[] their_heads,
            UIntPtr their_heads_len,
            ref GitMergeOpts merge_opts,
            ref GitCheckoutOpts checkout_opts);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_merge_commits(
            out IndexHandle index,
            RepositoryHandle repo,
            ObjectHandle our_commit,
            ObjectHandle their_commit,
            ref GitMergeOpts merge_opts);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_merge_analysis(
            out GitMergeAnalysis status_out,
            out GitMergePreference preference_out,
            RepositoryHandle repo,
            [In] IntPtr[] their_heads,
            int their_heads_len);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_annotated_commit_free(nint commit);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_message_prettify(
            GitBuf buf,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string message,
            [MarshalAs(UnmanagedType.Bool)] bool strip_comments,
            sbyte comment_char);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_note_create(
            out GitOid noteOid,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string notes_ref,
            SignatureHandle author,
            SignatureHandle committer,
            ref GitOid oid,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string note,
            int force);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_note_free(nint note);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern unsafe string git_note_message(NoteHandle note);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_oid* git_note_id(NoteHandle note);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_note_read(
            out NoteHandle note,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string notes_ref,
            ref GitOid oid);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_note_remove(
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string notes_ref,
            SignatureHandle author,
            SignatureHandle committer,
            ref GitOid oid);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_note_default_ref(
            GitBuf notes_ref,
            RepositoryHandle repo);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_note_foreach_cb(
            ref GitOid blob_id,
            ref GitOid annotated_object_id,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_note_foreach(
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string notes_ref,
            git_note_foreach_cb cb,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_odb_add_backend(ObjectDatabaseHandle odb, IntPtr backend, int priority);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr git_odb_backend_malloc(IntPtr backend, UIntPtr len);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_odb_exists(ObjectDatabaseHandle odb, ref GitOid id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_odb_foreach_cb(
            IntPtr id,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_odb_foreach(
            ObjectDatabaseHandle odb,
            git_odb_foreach_cb cb,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_odb_open_wstream(out OdbStreamHandle stream, ObjectDatabaseHandle odb, long size, GitObjectType type);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_odb_free(nint odb);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_odb_read_header(out UIntPtr len_out, out GitObjectType type, ObjectDatabaseHandle odb, ref GitOid id);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_object_free(nint obj);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_odb_stream_write(OdbStreamHandle Stream, IntPtr Buffer, UIntPtr len);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_odb_stream_finalize_write(out GitOid id, OdbStreamHandle stream);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_odb_stream_free(nint stream);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_odb_write(out GitOid id, ObjectDatabaseHandle odb, byte* data, UIntPtr len, GitObjectType type);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_oid* git_object_id(ObjectHandle obj);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_object_lookup(out ObjectHandle obj, RepositoryHandle repo, ref GitOid id, GitObjectType type);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_object_peel(
            out ObjectHandle peeled,
            ObjectHandle obj,
            GitObjectType type);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_object_short_id(
            GitBuf buf,
            ObjectHandle obj);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe GitObjectType git_object_type(ObjectHandle obj);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_patch_from_diff(out PatchHandle patch, DiffHandle diff, UIntPtr idx);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_patch_print(PatchHandle patch, git_diff_line_cb print_cb, IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_patch_line_stats(
            out UIntPtr total_context,
            out UIntPtr total_additions,
            out UIntPtr total_deletions,
            PatchHandle patch);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_patch_free(nint patch);

        /* Push network progress notification function */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_push_transfer_progress(uint current, uint total, UIntPtr bytes, IntPtr payload);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_packbuilder_progress(int stage, uint current, uint total, IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_packbuilder_free(nint packbuilder);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_packbuilder_insert(
            PackBuilderHandle packbuilder,
            ref GitOid id,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_packbuilder_insert_commit(
            PackBuilderHandle packbuilder,
            ref GitOid id);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_packbuilder_insert_recur(
            PackBuilderHandle packbuilder,
            ref GitOid id,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_packbuilder_insert_tree(
            PackBuilderHandle packbuilder,
            ref GitOid id);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_packbuilder_new(out PackBuilderHandle packbuilder, RepositoryHandle repo);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe UIntPtr git_packbuilder_object_count(PackBuilderHandle packbuilder);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe uint git_packbuilder_set_threads(PackBuilderHandle packbuilder, uint numThreads);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_packbuilder_write(
            PackBuilderHandle packbuilder,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path,
            uint mode,
            IntPtr progressCallback,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe UIntPtr git_packbuilder_written(PackBuilderHandle packbuilder);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_reference_create(
            out ReferenceHandle reference,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            ref GitOid oid,
            [MarshalAs(UnmanagedType.Bool)] bool force,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string log_message);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_reference_symbolic_create(
            out ReferenceHandle reference,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string target,
            [MarshalAs(UnmanagedType.Bool)] bool force,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string log_message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int ref_glob_callback(
            IntPtr reference_name,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_reference_foreach_glob(
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string glob,
            ref_glob_callback callback,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_reference_free(nint reference);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_reference_is_valid_name(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string refname);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_reference_list(out GitStrArray array, RepositoryHandle repo);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_reference_lookup(
            out ReferenceHandle reference,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern unsafe string git_reference_name(ReferenceHandle reference);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_reference_remove(
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_oid* git_reference_target(ReferenceHandle reference);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_reference_rename(
            out ReferenceHandle ref_out,
            ReferenceHandle reference,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string newName,
            [MarshalAs(UnmanagedType.Bool)] bool force,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string log_message);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_reference_set_target(
            out ReferenceHandle ref_out,
            ReferenceHandle reference,
            ref GitOid id,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string log_message);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_reference_symbolic_set_target(
            out ReferenceHandle ref_out,
            ReferenceHandle reference,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string target,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string log_message);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern unsafe string git_reference_symbolic_target(ReferenceHandle reference);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe GitReferenceType git_reference_type(ReferenceHandle reference);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_reference_ensure_log(
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string refname);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_reflog_free(nint reflog);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_reflog_read(
            out ReflogHandle ref_out,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe UIntPtr git_reflog_entrycount
            (ReflogHandle reflog);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe nint git_reflog_entry_byindex(ReflogHandle reflog, UIntPtr idx);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_oid* git_reflog_entry_id_old(nint entry);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_oid* git_reflog_entry_id_new(nint entry);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe SignatureHandle git_reflog_entry_committer(nint entry);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern unsafe string git_reflog_entry_message(nint entry);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_refspec_transform(
            GitBuf buf,
            IntPtr refspec,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);


        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_refspec_rtransform(
            GitBuf buf,
            IntPtr refspec,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_refspec_string(
            IntPtr refSpec);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern RefSpecDirection git_refspec_direction(IntPtr refSpec);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_refspec_dst(
            IntPtr refSpec);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern string git_refspec_src(
            IntPtr refspec);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool git_refspec_force(IntPtr refSpec);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool git_refspec_src_matches(
            IntPtr refspec,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string reference);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool git_refspec_dst_matches(
            IntPtr refspec,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string reference);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_autotag(RemoteHandle remote);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_connect(
            RemoteHandle remote,
            GitDirection direction,
            ref GitRemoteCallbacks callbacks,
            ref GitProxyOptions proxy_options,
            ref GitStrArray custom_headers);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_create(
            out RemoteHandle remote,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string url);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_create_anonymous(
            out RemoteHandle remote,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string url);


        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_create_with_fetchspec(
            out RemoteHandle remote,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string url,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string refspec);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_delete(
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_fetch(
            RemoteHandle remote,
            ref GitStrArray refspecs,
            GitFetchOptions fetch_opts,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string log_message);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_remote_free(nint remote);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_get_fetch_refspecs(out GitStrArray array, RemoteHandle remote);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe nint git_remote_get_refspec(RemoteHandle remote, UIntPtr n);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_get_push_refspecs(out GitStrArray array, RemoteHandle remote);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_push(
            RemoteHandle remote,
            ref GitStrArray refSpecs,
            GitPushOptions opts);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe UIntPtr git_remote_refspec_count(RemoteHandle remote);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_set_url(
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string remote,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string url);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_add_fetch(
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string remote,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string url);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_set_pushurl(
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string remote,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string url);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_add_push(
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string remote,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string url);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_remote_is_valid_name(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string remote_name);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_list(out GitStrArray array, RepositoryHandle repo);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_lookup(
            out RemoteHandle remote,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_remote_ls(out git_remote_head** heads, out UIntPtr size, RemoteHandle remote);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern unsafe string git_remote_name(RemoteHandle remote);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern unsafe string git_remote_url(RemoteHandle remote);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern unsafe string git_remote_pushurl(RemoteHandle remote);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_remote_set_autotag(
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            TagFetchMode option);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int remote_progress_callback(IntPtr str, int len, IntPtr data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int remote_completion_callback(RemoteCompletionType type, IntPtr data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int remote_update_tips_callback(
            IntPtr refName,
            ref GitOid oldId,
            ref GitOid newId,
            IntPtr data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int push_negotiation_callback(
            IntPtr updates,
            UIntPtr len,
            IntPtr payload);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int push_update_reference_callback(
            IntPtr refName,
            IntPtr status,
            IntPtr data
            );

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_repository_discover(
            GitBuf buf,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath start_path,
            [MarshalAs(UnmanagedType.Bool)] bool across_fs,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath ceiling_dirs);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_repository_fetchhead_foreach_cb(
            IntPtr remote_name,
            IntPtr remote_url,
            ref GitOid oid,
            [MarshalAs(UnmanagedType.Bool)] bool is_merge,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_fetchhead_foreach(
            RepositoryHandle repo,
            git_repository_fetchhead_foreach_cb cb,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_repository_free(nint repo);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_repository_head_detached(RepositoryHandle repo);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_repository_head_unborn(RepositoryHandle repo);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_ident(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] out string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] out string email,
            RepositoryHandle repo);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_index(out IndexHandle index, RepositoryHandle repo);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_init_ext(
            out RepositoryHandle repository,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path,
            GitRepositoryInitOptions options);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_repository_is_bare(RepositoryHandle handle);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_repository_is_shallow(RepositoryHandle repo);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_state_cleanup(RepositoryHandle repo);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_repository_mergehead_foreach_cb(
            ref GitOid oid,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_mergehead_foreach(
            RepositoryHandle repo,
            git_repository_mergehead_foreach_cb cb,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_message(
            GitBuf buf,
            RepositoryHandle repository);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_new(
            out RepositoryHandle repo);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_odb(out ObjectDatabaseHandle odb, RepositoryHandle repo);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_open(
            out RepositoryHandle repository,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_open_ext(
            out RepositoryHandle repository,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath path,
            RepositoryOpenFlags flags,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath ceilingDirs);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxFilePathNoCleanupMarshaler))]
        internal static extern unsafe FilePath git_repository_path(RepositoryHandle repository);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_set_config(
            RepositoryHandle repository,
            ConfigurationHandle config);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_set_ident(
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string email);


        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_set_index(
            RepositoryHandle repository,
            IndexHandle index);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_set_workdir(
            RepositoryHandle repository,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath workdir,
            [MarshalAs(UnmanagedType.Bool)] bool update_gitlink);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_set_head_detached(
            RepositoryHandle repo,
            ref GitOid commitish);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_set_head_detached_from_annotated(
            RepositoryHandle repo,
            AnnotatedCommitHandle commit);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_set_head(
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string refname);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_state(
            RepositoryHandle repository);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxFilePathNoCleanupMarshaler))]
        internal static extern unsafe FilePath git_repository_workdir(RepositoryHandle repository);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxFilePathNoCleanupMarshaler))]
        internal static extern FilePath git_repository_workdir(IntPtr repository);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_reset(
            RepositoryHandle repo,
            ObjectHandle target,
            ResetMode reset_type,
            ref GitCheckoutOpts opts);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_revert(
            RepositoryHandle repo,
            ObjectHandle commit,
            GitRevertOpts opts);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_revert_commit(
            out IndexHandle index,
            RepositoryHandle repo,
            ObjectHandle revert_commit,
            ObjectHandle our_commit,
            uint mainline,
            ref GitMergeOpts opts);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_revparse_ext(
            out ObjectHandle obj,
            out ReferenceHandle reference,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string spec);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_revwalk_free(nint walker);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_revwalk_hide(RevWalkerHandle walker, ref GitOid commit_id);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_revwalk_new(out RevWalkerHandle walker, RepositoryHandle repo);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_revwalk_next(out GitOid id, RevWalkerHandle walker);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_revwalk_push(RevWalkerHandle walker, ref GitOid id);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_revwalk_reset(RevWalkerHandle walker);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_revwalk_sorting(RevWalkerHandle walk, CommitSortStrategies sort);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_revwalk_simplify_first_parent(RevWalkerHandle walk);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_signature_free(nint signature);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_signature_new(
            out SignatureHandle signature,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string email,
            long time,
            int offset);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_signature_now(
            out SignatureHandle signature,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string email);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_signature_dup(out SignatureHandle dest, SignatureHandle sig);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_stash_save(
            out GitOid id,
            RepositoryHandle repo,
            SignatureHandle stasher,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string message,
            StashModifiers flags);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_stash_cb(
            UIntPtr index,
            IntPtr message,
            ref GitOid stash_id,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_stash_foreach(
            RepositoryHandle repo,
            git_stash_cb callback,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_stash_drop(RepositoryHandle repo, UIntPtr index);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_stash_apply(
            RepositoryHandle repo,
            UIntPtr index,
            GitStashApplyOpts opts);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_stash_pop(
            RepositoryHandle repo,
            UIntPtr index,
            GitStashApplyOpts opts);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_status_file(
            out FileStatus statusflags,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath filepath);


        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_status_list_new(
            out StatusListHandle git_status_list,
            RepositoryHandle repo,
            GitStatusOptions options);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_status_list_entrycount(
            StatusListHandle statusList);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_status_entry* git_status_byindex(
            StatusListHandle list,
            UIntPtr idx);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_status_list_free(nint statusList);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void git_strarray_free(
            ref GitStrArray array);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_submodule_lookup(
            out SubmoduleHandle reference,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_submodule_resolve_url(
            GitBuf buf,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string url);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_submodule_update(
            SubmoduleHandle sm,
            [MarshalAs(UnmanagedType.Bool)] bool init,
            ref GitSubmoduleUpdateOptions submoduleUpdateOptions);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int submodule_callback(
            IntPtr sm,
            IntPtr name,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_submodule_foreach(
            RepositoryHandle repo,
            submodule_callback callback,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_submodule_add_to_index(
            SubmoduleHandle submodule,
            [MarshalAs(UnmanagedType.Bool)] bool write_index);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_submodule_free(nint submodule);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern unsafe string git_submodule_path(
            SubmoduleHandle submodule);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern unsafe string git_submodule_url(
            SubmoduleHandle submodule);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_oid* git_submodule_index_id(
            SubmoduleHandle submodule);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_oid* git_submodule_head_id(
            SubmoduleHandle submodule);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_oid* git_submodule_wd_id(
            SubmoduleHandle submodule);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe SubmoduleIgnore git_submodule_ignore(
            SubmoduleHandle submodule);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe SubmoduleUpdate git_submodule_update_strategy(
            SubmoduleHandle submodule);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe SubmoduleRecurse git_submodule_fetch_recurse_submodules(
            SubmoduleHandle submodule);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_submodule_reload(
            SubmoduleHandle submodule,
            [MarshalAs(UnmanagedType.Bool)] bool force);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_submodule_status(
            out SubmoduleStatus status,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictFilePathMarshaler))] FilePath name,
            GitSubmoduleIgnore ignore);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_submodule_init(
            SubmoduleHandle submodule,
            [MarshalAs(UnmanagedType.Bool)] bool overwrite);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_tag_annotation_create(
            out GitOid oid,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            ObjectHandle target,
            SignatureHandle signature,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string message);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_tag_create(
            out GitOid oid,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            ObjectHandle target,
            SignatureHandle signature,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string message,
            [MarshalAs(UnmanagedType.Bool)]
            bool force);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_tag_create_lightweight(
            out GitOid oid,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            ObjectHandle target,
            [MarshalAs(UnmanagedType.Bool)]
            bool force);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_tag_delete(
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string tagName);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_tag_list(out GitStrArray array, RepositoryHandle repo);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern unsafe string git_tag_message(ObjectHandle tag);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern unsafe string git_tag_name(ObjectHandle tag);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe SignatureHandle git_tag_tagger(ObjectHandle tag);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_oid* git_tag_target_id(ObjectHandle tag);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe GitObjectType git_tag_target_type(ObjectHandle tag);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_libgit2_init();

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_libgit2_shutdown();

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_openssl_set_locking();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void git_trace_cb(LogLevel level, IntPtr message);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_trace_set(LogLevel level, git_trace_cb trace_cb);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_transfer_progress_callback(ref GitTransferProgress stats, IntPtr payload);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_transport_cb(out IntPtr transport, IntPtr remote, IntPtr payload);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate int git_transport_certificate_check_cb(git_certificate* cert, int valid, IntPtr hostname, IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_transport_register(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string prefix,
            IntPtr transport_cb,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_transport_smart(
            out IntPtr transport,
            IntPtr remote,
            IntPtr definition);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_transport_smart_certificate_check(
            IntPtr transport,
            IntPtr cert,
            int valid,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string hostname);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_transport_smart_credentials(
            out IntPtr cred_out,
            IntPtr transport,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string user,
            int methods);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_transport_unregister(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string prefix);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe uint git_tree_entry_filemode(TreeEntryHandle entry);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe UnownedTreeEntryHandle git_tree_entry_byindex(ObjectHandle tree, UIntPtr idx);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_tree_entry_bypath(
            out TreeEntryHandle tree,
            ObjectHandle root,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string treeentry_path);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_tree_entry_free(nint treeEntry);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe git_oid* git_tree_entry_id(TreeEntryHandle entry);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))]
        internal static extern unsafe string git_tree_entry_name(TreeEntryHandle entry);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe GitObjectType git_tree_entry_type(TreeEntryHandle entry);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe UIntPtr git_tree_entrycount(ObjectHandle tree);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_treebuilder_new(out TreeBuilderHandle builder, RepositoryHandle repo, IntPtr src);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_treebuilder_insert(
            IntPtr entry_out,
            TreeBuilderHandle builder,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string treeentry_name,
            ref GitOid id,
            uint attributes);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_treebuilder_write(out GitOid id, TreeBuilderHandle bld);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_treebuilder_free(nint bld);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_blob_is_binary(ObjectHandle blob);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_cherrypick(RepositoryHandle repo, ObjectHandle commit, GitCherryPickOptions options);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_cherrypick_commit(out IndexHandle index,
            RepositoryHandle repo,
            ObjectHandle cherrypick_commit,
            ObjectHandle our_commit,
            uint mainline,
            ref GitMergeOpts options);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int git_transaction_commit(IntPtr txn);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void git_transaction_free(IntPtr txn);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int url_resolve_callback(
            IntPtr url_resolved,
            IntPtr url,
            int direction,
            IntPtr payload);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe void git_worktree_free(nint worktree);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_worktree_lookup(
            out WorktreeHandle reference,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_worktree_list(
            out GitStrArray array,
            RepositoryHandle repo);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_repository_open_from_worktree(
            out RepositoryHandle repository,
            WorktreeHandle worktree);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_worktree_is_locked(
            GitBuf reason,
            WorktreeHandle worktree);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_worktree_validate(
            WorktreeHandle worktree);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_worktree_lock(
            WorktreeHandle worktree,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string reason);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_worktree_unlock(
            WorktreeHandle worktree);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_worktree_add(
            out WorktreeHandle reference,
            RepositoryHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(StrictUtf8Marshaler))] string path,
            git_worktree_add_options options);

        [DllImport(libgit2, CallingConvention = CallingConvention.Cdecl)]
        internal static extern unsafe int git_worktree_prune(
            WorktreeHandle worktree,
            git_worktree_prune_options options);
    }
}
