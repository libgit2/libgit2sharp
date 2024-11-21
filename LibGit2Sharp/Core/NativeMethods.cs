using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using LibGit2Sharp.Core.Handles;
using LibGit2Sharp.Core.Marshallers;

// Restrict the set of directories where the native library is loaded from to safe directories.
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.ApplicationDirectory | DllImportSearchPath.SafeDirectories)]
[assembly: DisableRuntimeMarshalling]

namespace LibGit2Sharp.Core
{
    internal static partial class NativeMethods
    {
        private const string libgit2 = NativeDllName.Name;

        // An object tied to the lifecycle of the NativeMethods static class.
        // This will handle initialization and shutdown of the underlying
        // native library.
        private static NativeShutdownObject shutdownObject;

        static NativeMethods()
        {
            NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, ResolveDll);
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

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitError* git_error_last();

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_error_set_str(GitErrorCategory error_class, string errorString);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void git_error_set_oom();

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial uint git_blame_get_hunk_count(git_blame* blame);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial git_blame_hunk* git_blame_get_hunk_byindex(git_blame* blame, uint index);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_blame_file(out git_blame* blame, git_repository* repo, string path, git_blame_options options);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_blame_free(git_blame* blame);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_blob_create_from_disk(ref GitOid id, git_repository* repo, string path);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_blob_create_from_workdir(ref GitOid id, git_repository* repo, string relative_path);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_blob_create_from_stream(out IntPtr stream, git_repository* repositoryPtr, string hintpath);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_blob_create_from_stream_commit(ref GitOid oid, IntPtr stream);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_blob_filtered_content(GitBuf buf, git_object* blob, string as_path, [MarshalAs(UnmanagedType.Bool)] bool check_for_binary_data);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial IntPtr git_blob_rawcontent(git_object* blob);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial long git_blob_rawsize(git_object* blob);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_branch_create_from_annotated(out git_reference* ref_out, git_repository* repo, string branch_name, git_annotated_commit* target, [MarshalAs(UnmanagedType.Bool)] bool force);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_branch_delete(git_reference* reference);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int branch_foreach_callback(IntPtr branch_name, GitBranchType branch_type, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void git_branch_iterator_free(IntPtr iterator);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_branch_iterator_new(out IntPtr iter_out, IntPtr repo, GitBranchType branch_type);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_branch_move(out git_reference* ref_out, git_reference* reference, string new_branch_name, [MarshalAs(UnmanagedType.Bool)] bool force);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_branch_next(out IntPtr ref_out, out GitBranchType type_out, IntPtr iter);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_branch_remote_name(GitBuf buf, git_repository* repo, string canonical_branch_name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int commit_signing_callback(IntPtr signature, IntPtr signature_field, IntPtr commit_content, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_rebase_init(out git_rebase* rebase, git_repository* repo, git_annotated_commit* branch, git_annotated_commit* upstream, git_annotated_commit* onto, GitRebaseOptions options);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_rebase_open(out git_rebase* rebase, git_repository* repo, GitRebaseOptions options);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial UIntPtr git_rebase_operation_entrycount(git_rebase* rebase);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial UIntPtr git_rebase_operation_current(git_rebase* rebase);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial git_rebase_operation* git_rebase_operation_byindex(git_rebase* rebase, UIntPtr index);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_rebase_next(out git_rebase_operation* operation, git_rebase* rebase);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_rebase_commit(ref GitOid id, git_rebase* rebase, git_signature* author, git_signature* committer, IntPtr message_encoding, IntPtr message);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_rebase_abort(git_rebase* rebase);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_rebase_finish(git_rebase* repo, git_signature* signature);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_rebase_free(git_rebase* rebase);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_rename(ref GitStrArray problems, git_repository* repo, string old_name, string new_name);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_remote_rename_problem_cb(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalCookie = UniqueId.UniqueIdentifier, MarshalTypeRef = typeof(LaxUtf8NoCleanupMarshaler))] string problematic_refspec,
            IntPtr payload);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_branch_upstream_name(GitBuf buf, git_repository* repo, string referenceName);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void git_buf_dispose(GitBuf buf);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_checkout_tree(git_repository* repo, git_object* treeish, ref GitCheckoutOpts opts);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_checkout_index(git_repository* repo, git_object* treeish, ref GitCheckoutOpts opts);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_clone(out git_repository* repo, string origin_url, string workdir_path, ref GitCloneOptions opts);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial git_signature* git_commit_author(git_object* commit);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial git_signature* git_commit_committer(git_object* commit);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_commit_create_from_ids(out GitOid id, git_repository* repo, string updateRef, git_signature* author, git_signature* committer, string encoding, string message, ref GitOid tree, UIntPtr parentCount, [In] IntPtr[] parents);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_commit_create_buffer(GitBuf res, git_repository* repo, git_signature* author, git_signature* committer, string encoding, string message, git_object* tree, UIntPtr parent_count, IntPtr* parents /* git_commit** originally */);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_commit_create_with_signature(out GitOid id, git_repository* repo, string commit_content, string signature, string signature_field);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_commit_message(git_object* commit);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_commit_summary(git_object* commit);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_commit_message_encoding(git_object* commit);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitOid* git_commit_parent_id(git_object* commit, uint n);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial uint git_commit_parentcount(git_object* commit);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitOid* git_commit_tree_id(git_object* commit);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_commit_extract_signature(GitBuf signature, GitBuf signed_data, git_repository* repo, ref GitOid commit_id, string field);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_config_delete_entry(git_config* cfg, string name);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_config_lock(out IntPtr txn, git_config* config);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_config_delete_multivar(git_config* cfg, string name, string regexp);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_config_set_multivar(git_config* cfg, string name, string regexp, string value);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_config_find_global(GitBuf global_config_path);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_config_find_system(GitBuf system_config_path);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_config_find_xdg(GitBuf xdg_config_path);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_config_find_programdata(GitBuf programdata_config_path);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_config_free(git_config* cfg);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_config_entry_free(GitConfigEntry* entry);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_config_get_entry(out GitConfigEntry* entry, git_config* cfg, string name);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_config_add_file_ondisk(git_config* cfg, string path, uint level, git_repository* repo, [MarshalAs(UnmanagedType.Bool)] bool force);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_config_new(out git_config* cfg);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_config_open_level(out git_config* cfg, git_config* parent, uint level);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_config_parse_bool([MarshalAs(UnmanagedType.Bool)] out bool value, string valueToParse);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_config_parse_int32([MarshalAs(UnmanagedType.I4)] out int value, string valueToParse);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_config_parse_int64([MarshalAs(UnmanagedType.I8)] out long value, string valueToParse);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_config_set_bool(git_config* cfg, string name, [MarshalAs(UnmanagedType.Bool)] bool value);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_config_set_int32(git_config* cfg, string name, int value);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_config_set_int64(git_config* cfg, string name, long value);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_config_set_string(git_config* cfg, string name, string value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int config_foreach_callback(IntPtr entry, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_config_foreach(git_config* cfg, config_foreach_callback callback, IntPtr payload);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_config_iterator_glob_new(out IntPtr iter, IntPtr cfg, string regexp);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_config_next(out IntPtr entry, IntPtr iter);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void git_config_iterator_free(IntPtr iter);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_config_snapshot(out git_config* @out, git_config* config);

        // Ordinarily we would decorate the `url` parameter with the StrictUtf8Marshaler like we do everywhere
        // else, but apparently doing a native->managed callback with the 64-bit version of CLR 2.0 can
        // sometimes vomit when using a custom IMarshaler.  So yeah, don't do that.  If you need the url,
        // call StrictUtf8Marshaler.FromNative manually.  See the discussion here:
        // http://social.msdn.microsoft.com/Forums/en-US/netfx64bit/thread/1eb746c6-d695-4632-8a9e-16c4fa98d481
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_cred_acquire_cb(out IntPtr cred, IntPtr url, IntPtr username_from_url, GitCredentialType allowed_types, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_cred_default_new(out IntPtr cred);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_cred_userpass_plaintext_new(out IntPtr cred, string username, string password);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void git_cred_free(IntPtr cred);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_describe_commit(out git_describe_result* describe, git_object* committish, ref GitDescribeOptions options);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_describe_format(GitBuf buf, git_describe_result* describe, ref GitDescribeFormatOptions options);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_describe_result_free(git_describe_result* describe);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_diff_free(git_diff* diff);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_diff_tree_to_tree(out git_diff* diff, git_repository* repo, git_object* oldTree, git_object* newTree, GitDiffOptions options);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_diff_tree_to_index(out git_diff* diff, git_repository* repo, git_object* oldTree, git_index* index, GitDiffOptions options);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_diff_merge(git_diff* onto, git_diff* from);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_diff_index_to_workdir(out git_diff* diff, git_repository* repo, git_index* index, GitDiffOptions options);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_diff_tree_to_workdir(out git_diff* diff, git_repository* repo, git_object* oldTree, GitDiffOptions options);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate int git_diff_file_cb([In] git_diff_delta* delta, float progress, IntPtr payload);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate int git_diff_hunk_cb([In] git_diff_delta* delta, [In] GitDiffHunk hunk, IntPtr payload);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate int git_diff_line_cb([In] git_diff_delta* delta, [In] GitDiffHunk hunk, [In] GitDiffLine line, IntPtr payload);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate int git_diff_binary_cb([In] git_diff_delta* delta, [In] GitDiffBinary binary, IntPtr payload);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_diff_blobs(git_object* oldBlob, string old_as_path, git_object* newBlob, string new_as_path, GitDiffOptions options, git_diff_file_cb fileCallback, git_diff_binary_cb binaryCallback, git_diff_hunk_cb hunkCallback, git_diff_line_cb lineCallback, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_diff_foreach(git_diff* diff, git_diff_file_cb fileCallback, git_diff_binary_cb binaryCallback, git_diff_hunk_cb hunkCallback, git_diff_line_cb lineCallback, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_diff_find_similar(git_diff* diff, GitDiffFindOptions options);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial UIntPtr git_diff_num_deltas(git_diff* diff);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial git_diff_delta* git_diff_get_delta(git_diff* diff, UIntPtr idx);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_filter_register(string name, IntPtr gitFilter, int priority);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_filter_unregister(string name);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_filter_source_mode(git_filter_source* source);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_features();

        #region git_libgit2_opts

        // Bindings for git_libgit2_opts(int option, ...):
        // Currently only GIT_OPT_GET_SEARCH_PATH and GIT_OPT_SET_SEARCH_PATH are supported,
        // but other overloads could be added using a similar pattern.
        // CallingConvention.Cdecl is used to allow binding the the C varargs signature, and each possible call signature must be enumerated.
        // __argslist was an option, but is an undocumented feature that should likely not be used here.

        // git_libgit2_opts(GIT_OPT_GET_SEARCH_PATH, int level, git_buf *buf)
        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_opts(int option, uint level, GitBuf buf);

        // git_libgit2_opts(GIT_OPT_SET_SEARCH_PATH, int level, const char *path)
        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_opts(int option, uint level, string path);

        // git_libgit2_opts(GIT_OPT_ENABLE_*, int enabled)
        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_opts(int option, int enabled);

        // git_libgit2_opts(GIT_OPT_SET_USER_AGENT, const char *path)
        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_opts(int option, string path);

        // git_libgit2_opts(GIT_OPT_GET_USER_AGENT, git_buf *buf)
        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_opts(int option, GitBuf buf);

        // git_libgit2_opts(GIT_OPT_SET_EXTENSIONS, const char **extensions, size_t len)
        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_opts(int option, IntPtr extensions, UIntPtr len);

        // git_libgit2_opts(GIT_OPT_GET_EXTENSIONS, git_strarray *out)
        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_opts(int option, out GitStrArray extensions);
        #endregion

        #region git_libgit2_opts_osxarm64

        // For RID osx-arm64 the calling convention is different: we need to pad out to 8 arguments before varargs
        // (see discussion at https://github.com/dotnet/runtime/issues/48796)

        // git_libgit2_opts(GIT_OPT_GET_SEARCH_PATH, int level, git_buf *buf)
        [LibraryImport(libgit2, EntryPoint = "git_libgit2_opts")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_opts_osxarm64(int option, IntPtr nop2, IntPtr nop3, IntPtr nop4, IntPtr nop5, IntPtr nop6, IntPtr nop7, IntPtr nop8, uint level, GitBuf buf);

        // git_libgit2_opts(GIT_OPT_SET_SEARCH_PATH, int level, const char *path)
        [LibraryImport(libgit2, EntryPoint = "git_libgit2_opts", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_opts_osxarm64(int option, IntPtr nop2, IntPtr nop3, IntPtr nop4, IntPtr nop5, IntPtr nop6, IntPtr nop7, IntPtr nop8, uint level, string path);

        // git_libgit2_opts(GIT_OPT_ENABLE_*, int enabled)
        [LibraryImport(libgit2, EntryPoint = "git_libgit2_opts")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_opts_osxarm64(int option, IntPtr nop2, IntPtr nop3, IntPtr nop4, IntPtr nop5, IntPtr nop6, IntPtr nop7, IntPtr nop8, int enabled);

        // git_libgit2_opts(GIT_OPT_SET_USER_AGENT, const char *path)
        [LibraryImport(libgit2, EntryPoint = "git_libgit2_opts", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_opts_osxarm64(int option, IntPtr nop2, IntPtr nop3, IntPtr nop4, IntPtr nop5, IntPtr nop6, IntPtr nop7, IntPtr nop8, string path);

        // git_libgit2_opts(GIT_OPT_GET_USER_AGENT, git_buf *buf)
        [LibraryImport(libgit2, EntryPoint = "git_libgit2_opts")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_opts_osxarm64(int option, IntPtr nop2, IntPtr nop3, IntPtr nop4, IntPtr nop5, IntPtr nop6, IntPtr nop7, IntPtr nop8, GitBuf buf);

        // git_libgit2_opts(GIT_OPT_SET_EXTENSIONS, const char **extensions, size_t len)
        [LibraryImport(libgit2, EntryPoint = "git_libgit2_opts")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_opts_osxarm64(int option, IntPtr nop2, IntPtr nop3, IntPtr nop4, IntPtr nop5, IntPtr nop6, IntPtr nop7, IntPtr nop8, IntPtr extensions, UIntPtr len);

        // git_libgit2_opts(GIT_OPT_GET_EXTENSIONS, git_strarray *out)
        [LibraryImport(libgit2, EntryPoint = "git_libgit2_opts")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_opts_osxarm64(int option, IntPtr nop2, IntPtr nop3, IntPtr nop4, IntPtr nop5, IntPtr nop6, IntPtr nop7, IntPtr nop8, out GitStrArray extensions);
        #endregion

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_graph_ahead_behind(out UIntPtr ahead, out UIntPtr behind, git_repository* repo, ref GitOid one, ref GitOid two);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_graph_descendant_of(git_repository* repo, ref GitOid commit, ref GitOid ancestor);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_ignore_add_rule(git_repository* repo, string rules);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_ignore_clear_internal_rules(git_repository* repo);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_ignore_path_is_ignored(out int ignored, git_repository* repo, string path);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_index_add_bypath(git_index* index, string path);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_index_add(git_index* index, git_index_entry* entry);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_index_conflict_get(out git_index_entry* ancestor, out git_index_entry* ours, out git_index_entry* theirs, git_index* index, string path);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_index_conflict_iterator_new(out git_index_conflict_iterator* iterator, git_index* index);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_index_conflict_next(out git_index_entry* ancestor, out git_index_entry* ours, out git_index_entry* theirs, git_index_conflict_iterator* iterator);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_index_conflict_iterator_free(git_index_conflict_iterator* iterator);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial UIntPtr git_index_entrycount(git_index* index);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_index_entry_stage(git_index_entry* indexentry);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_index_free(git_index* index);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial git_index_entry* git_index_get_byindex(git_index* index, UIntPtr n);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial git_index_entry* git_index_get_bypath(git_index* index, string path, int stage);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_index_has_conflicts(git_index* index);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial UIntPtr git_index_name_entrycount(git_index* handle);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial git_index_name_entry* git_index_name_get_byindex(git_index* handle, UIntPtr n);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_index_open(out git_index* index, string indexpath);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_index_read(git_index* index, [MarshalAs(UnmanagedType.Bool)] bool force);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_index_remove_bypath(git_index* index, string path);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial UIntPtr git_index_reuc_entrycount(git_index* handle);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial git_index_reuc_entry* git_index_reuc_get_byindex(git_index* handle, UIntPtr n);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial git_index_reuc_entry* git_index_reuc_get_bypath(git_index* handle, string path);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_index_write(git_index* index);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_index_write_tree(out GitOid treeOid, git_index* index);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_index_write_tree_to(out GitOid treeOid, git_index* index, git_repository* repo);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_index_read_tree(git_index* index, git_object* tree);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_index_clear(git_index* index);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_merge_base_many(out GitOid mergeBase, git_repository* repo, int length, [In] GitOid[] input_array);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_merge_base_octopus(out GitOid mergeBase, git_repository* repo, int length, [In] GitOid[] input_array);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_annotated_commit_from_ref(out git_annotated_commit* annotatedCommit, git_repository* repo, git_reference* reference);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_annotated_commit_from_fetchhead(out git_annotated_commit* annotatedCommit, git_repository* repo, string branch_name, string remote_url, ref GitOid oid);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_annotated_commit_from_revspec(out git_annotated_commit* annotatedCommit, git_repository* repo, string revspec);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_annotated_commit_lookup(out git_annotated_commit* annotatedCommit, git_repository* repo, ref GitOid id);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitOid* git_annotated_commit_id(git_annotated_commit* annotatedCommit);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_merge(git_repository* repo, [In] IntPtr[] their_heads, UIntPtr their_heads_len, ref GitMergeOpts merge_opts, ref GitCheckoutOpts checkout_opts);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_merge_commits(out git_index* index, git_repository* repo, git_object* our_commit, git_object* their_commit, ref GitMergeOpts merge_opts);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_merge_analysis(out GitMergeAnalysis status_out, out GitMergePreference preference_out, git_repository* repo, [In] IntPtr[] their_heads, int their_heads_len);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_annotated_commit_free(git_annotated_commit* commit);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_message_prettify(GitBuf buf, string message, [MarshalAs(UnmanagedType.Bool)] bool strip_comments, sbyte comment_char);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_note_create(out GitOid noteOid, git_repository* repo, string notes_ref, git_signature* author, git_signature* committer, ref GitOid oid, string note, int force);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_note_free(git_note* note);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_note_message(git_note* note);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitOid* git_note_id(git_note* note);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_note_read(out git_note* note, git_repository* repo, string notes_ref, ref GitOid oid);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_note_remove(git_repository* repo, string notes_ref, git_signature* author, git_signature* committer, ref GitOid oid);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_note_default_ref(GitBuf notes_ref, git_repository* repo);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_note_foreach_cb(ref GitOid blob_id, ref GitOid annotated_object_id, IntPtr payload);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_note_foreach(git_repository* repo, string notes_ref, git_note_foreach_cb cb, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_odb_add_backend(git_odb* odb, IntPtr backend, int priority);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial IntPtr git_odb_backend_malloc(IntPtr backend, UIntPtr len);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_odb_exists(git_odb* odb, ref GitOid id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_odb_foreach_cb(IntPtr id, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_odb_foreach(git_odb* odb, git_odb_foreach_cb cb, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_odb_open_wstream(out git_odb_stream* stream, git_odb* odb, long size, GitObjectType type);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_odb_free(git_odb* odb);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_odb_read_header(out UIntPtr len_out, out GitObjectType type, git_odb* odb, ref GitOid id);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_object_free(git_object* obj);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_odb_stream_write(git_odb_stream* Stream, IntPtr Buffer, UIntPtr len);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_odb_stream_finalize_write(out GitOid id, git_odb_stream* stream);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_odb_stream_free(git_odb_stream* stream);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_odb_write(out GitOid id, git_odb* odb, byte* data, UIntPtr len, GitObjectType type);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitOid* git_object_id(git_object* obj);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_object_lookup(out git_object* obj, git_repository* repo, ref GitOid id, GitObjectType type);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_object_peel(out git_object* peeled, git_object* obj, GitObjectType type);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_object_short_id(GitBuf buf, git_object* obj);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitObjectType git_object_type(git_object* obj);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_patch_from_diff(out git_patch* patch, git_diff* diff, UIntPtr idx);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_patch_print(git_patch* patch, git_diff_line_cb print_cb, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_patch_line_stats(out UIntPtr total_context, out UIntPtr total_additions, out UIntPtr total_deletions, git_patch* patch);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_patch_free(git_patch* patch);

        /* Push network progress notification function */
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_push_transfer_progress(uint current, uint total, UIntPtr bytes, IntPtr payload);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_packbuilder_progress(int stage, uint current, uint total, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_packbuilder_free(git_packbuilder* packbuilder);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_packbuilder_insert(git_packbuilder* packbuilder, ref GitOid id, string name);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_packbuilder_insert_commit(git_packbuilder* packbuilder, ref GitOid id);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_packbuilder_insert_recur(git_packbuilder* packbuilder, ref GitOid id, string name);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_packbuilder_insert_tree(git_packbuilder* packbuilder, ref GitOid id);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_packbuilder_new(out git_packbuilder* packbuilder, git_repository* repo);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial UIntPtr git_packbuilder_object_count(git_packbuilder* packbuilder);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial uint git_packbuilder_set_threads(git_packbuilder* packbuilder, uint numThreads);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_packbuilder_write(git_packbuilder* packbuilder, string path, uint mode, IntPtr progressCallback, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial UIntPtr git_packbuilder_written(git_packbuilder* packbuilder);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_reference_create(out git_reference* reference, git_repository* repo, string name, ref GitOid oid, [MarshalAs(UnmanagedType.Bool)] bool force, string log_message);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_reference_symbolic_create(out git_reference* reference, git_repository* repo, string name, string target, [MarshalAs(UnmanagedType.Bool)] bool force, string log_message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int ref_glob_callback(IntPtr reference_name, IntPtr payload);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_reference_foreach_glob(git_repository* repo, string glob, ref_glob_callback callback, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_reference_free(git_reference* reference);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_reference_is_valid_name(string refname);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_reference_list(out GitStrArray array, git_repository* repo);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_reference_lookup(out git_reference* reference, git_repository* repo, string name);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_reference_name(git_reference* reference);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_reference_remove(git_repository* repo, string name);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitOid* git_reference_target(git_reference* reference);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_reference_rename(out git_reference* ref_out, git_reference* reference, string newName, [MarshalAs(UnmanagedType.Bool)] bool force, string log_message);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_reference_set_target(out git_reference* ref_out, git_reference* reference, ref GitOid id, string log_message);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_reference_symbolic_set_target(out git_reference* ref_out, git_reference* reference, string target, string log_message);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_reference_symbolic_target(git_reference* reference);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitReferenceType git_reference_type(git_reference* reference);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_reference_ensure_log(git_repository* repo, string refname);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_reflog_free(git_reflog* reflog);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_reflog_read(out git_reflog* ref_out, git_repository* repo, string name);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial UIntPtr git_reflog_entrycount(git_reflog* reflog);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial git_reflog_entry* git_reflog_entry_byindex(git_reflog* reflog, UIntPtr idx);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitOid* git_reflog_entry_id_old(git_reflog_entry* entry);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitOid* git_reflog_entry_id_new(git_reflog_entry* entry);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial git_signature* git_reflog_entry_committer(git_reflog_entry* entry);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_reflog_entry_message(git_reflog_entry* entry);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_refspec_transform(GitBuf buf, IntPtr refspec, string name);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_refspec_rtransform(GitBuf buf, IntPtr refspec, string name);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static partial string git_refspec_string(IntPtr refSpec);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial RefSpecDirection git_refspec_direction(IntPtr refSpec);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static partial string git_refspec_dst(IntPtr refSpec);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static partial string git_refspec_src(IntPtr refspec);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool git_refspec_force(IntPtr refSpec);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool git_refspec_src_matches(IntPtr refspec, string reference);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool git_refspec_dst_matches(IntPtr refspec, string reference);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_autotag(git_remote* remote);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_connect(git_remote* remote, GitDirection direction, ref GitRemoteCallbacks callbacks, ref GitProxyOptions proxy_options, ref GitStrArray custom_headers);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_create(out git_remote* remote, git_repository* repo, string name, string url);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_create_anonymous(out git_remote* remote, git_repository* repo, string url);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_create_with_fetchspec(out git_remote* remote, git_repository* repo, string name, string url, string refspec);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_delete(git_repository* repo, string name);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_fetch(git_remote* remote, ref GitStrArray refspecs, GitFetchOptions fetch_opts, string log_message);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_remote_free(git_remote* remote);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_get_fetch_refspecs(out GitStrArray array, git_remote* remote);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial git_refspec* git_remote_get_refspec(git_remote* remote, UIntPtr n);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_get_push_refspecs(out GitStrArray array, git_remote* remote);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_push(git_remote* remote, ref GitStrArray refSpecs, GitPushOptions opts);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial UIntPtr git_remote_refspec_count(git_remote* remote);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_set_url(git_repository* repo, string remote, string url);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_add_fetch(git_repository* repo, string remote, string url);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_set_pushurl(git_repository* repo, string remote, string url);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_add_push(git_repository* repo, string remote, string url);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_remote_is_valid_name(string remote_name);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_list(out GitStrArray array, git_repository* repo);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_lookup(out git_remote* remote, git_repository* repo, string name);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_remote_ls(out git_remote_head** heads, out UIntPtr size, git_remote* remote);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_remote_name(git_remote* remote);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_remote_url(git_remote* remote);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_remote_pushurl(git_remote* remote);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_remote_set_autotag(git_repository* repo, string name, TagFetchMode option);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int remote_progress_callback(IntPtr str, int len, IntPtr data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int remote_completion_callback(RemoteCompletionType type, IntPtr data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int remote_update_tips_callback(IntPtr refName, ref GitOid oldId, ref GitOid newId, IntPtr data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int push_negotiation_callback(IntPtr updates, UIntPtr len, IntPtr payload);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int push_update_reference_callback(IntPtr refName, IntPtr status, IntPtr data);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_repository_discover(GitBuf buf, string start_path, [MarshalAs(UnmanagedType.Bool)] bool across_fs, string ceiling_dirs);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_repository_fetchhead_foreach_cb(IntPtr remote_name, IntPtr remote_url, ref GitOid oid, [MarshalAs(UnmanagedType.Bool)] bool is_merge, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_fetchhead_foreach(git_repository* repo, git_repository_fetchhead_foreach_cb cb, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_repository_free(git_repository* repo);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_repository_head_detached(IntPtr repo);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_repository_head_unborn(IntPtr repo);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_ident([MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))] out string name, [MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))] out string email, git_repository* repo);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_index(out git_index* index, git_repository* repo);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_init_ext(out git_repository* repository, string path, GitRepositoryInitOptions options);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_repository_is_bare(IntPtr handle);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_repository_is_shallow(IntPtr repo);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_state_cleanup(git_repository* repo);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_repository_mergehead_foreach_cb(ref GitOid oid, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_mergehead_foreach(git_repository* repo, git_repository_mergehead_foreach_cb cb, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_message(GitBuf buf, git_repository* repository);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_new(out git_repository* repo);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_odb(out git_odb* odb, git_repository* repo);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_open(out git_repository* repository, string path);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_open_ext(out git_repository* repository, string path, RepositoryOpenFlags flags, string ceilingDirs);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_repository_path(git_repository* repository);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_set_config(git_repository* repository, git_config* config);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_set_ident(git_repository* repo, string name, string email);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_set_index(git_repository* repository, git_index* index);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_set_workdir(git_repository* repository, string workdir, [MarshalAs(UnmanagedType.Bool)] bool update_gitlink);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_set_head_detached(git_repository* repo, ref GitOid commitish);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_set_head_detached_from_annotated(git_repository* repo, git_annotated_commit* commit);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_set_head(git_repository* repo, string refname);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_state(git_repository* repository);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_repository_workdir(git_repository* repository);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static partial string git_repository_workdir(IntPtr repository);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_reset(git_repository* repo, git_object* target, ResetMode reset_type, GitCheckoutOpts* opts);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_revert(git_repository* repo, git_object* commit, GitRevertOpts opts);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_revert_commit(out git_index* index, git_repository* repo, git_object* revert_commit, git_object* our_commit, uint mainline, ref GitMergeOpts opts);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_revparse_ext(out git_object* obj, out git_reference* reference, git_repository* repo, string spec);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_revwalk_free(git_revwalk* walker);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_revwalk_hide(git_revwalk* walker, ref GitOid commit_id);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_revwalk_new(out git_revwalk* walker, git_repository* repo);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_revwalk_next(out GitOid id, git_revwalk* walker);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_revwalk_push(git_revwalk* walker, ref GitOid id);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_revwalk_reset(git_revwalk* walker);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_revwalk_sorting(git_revwalk* walk, CommitSortStrategies sort);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_revwalk_simplify_first_parent(git_revwalk* walk);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_signature_free(git_signature* signature);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_signature_new(out git_signature* signature, string name, string email, long time, int offset);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_signature_now(out git_signature* signature, string name, string email);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_signature_dup(out git_signature* dest, git_signature* sig);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_stash_save(out GitOid id, git_repository* repo, git_signature* stasher, string message, StashModifiers flags);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_stash_cb(UIntPtr index, IntPtr message, ref GitOid stash_id, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_stash_foreach(git_repository* repo, git_stash_cb callback, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_stash_drop(git_repository* repo, UIntPtr index);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_stash_apply(git_repository* repo, UIntPtr index, GitStashApplyOpts opts);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_stash_pop(git_repository* repo, UIntPtr index, GitStashApplyOpts opts);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_status_file(out FileStatus statusflags, git_repository* repo, string filepath);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_status_list_new(out git_status_list* git_status_list, git_repository* repo, GitStatusOptions options);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_status_list_entrycount(git_status_list* statusList);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial git_status_entry* git_status_byindex(git_status_list* list, UIntPtr idx);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_status_list_free(git_status_list* statusList);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void git_strarray_free(ref GitStrArray array);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_submodule_lookup(out git_submodule* reference, git_repository* repo, string name);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_submodule_resolve_url(GitBuf buf, git_repository* repo, string url);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_submodule_update(git_submodule* sm, [MarshalAs(UnmanagedType.Bool)] bool init, ref GitSubmoduleUpdateOptions submoduleUpdateOptions);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int submodule_callback(IntPtr sm, IntPtr name, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_submodule_foreach(git_repository* repo, submodule_callback callback, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_submodule_add_to_index(git_submodule* submodule, [MarshalAs(UnmanagedType.Bool)] bool write_index);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_submodule_free(git_submodule* submodule);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_submodule_path(git_submodule* submodule);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_submodule_url(git_submodule* submodule);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitOid* git_submodule_index_id(git_submodule* submodule);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitOid* git_submodule_head_id(git_submodule* submodule);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitOid* git_submodule_wd_id(git_submodule* submodule);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial SubmoduleIgnore git_submodule_ignore(git_submodule* submodule);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial SubmoduleUpdate git_submodule_update_strategy(git_submodule* submodule);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial SubmoduleRecurse git_submodule_fetch_recurse_submodules(git_submodule* submodule);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_submodule_reload(git_submodule* submodule, [MarshalAs(UnmanagedType.Bool)] bool force);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_submodule_status(out SubmoduleStatus status, git_repository* repo, string name, GitSubmoduleIgnore ignore);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_submodule_init(git_submodule* submodule, [MarshalAs(UnmanagedType.Bool)] bool overwrite);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_tag_annotation_create(out GitOid oid, git_repository* repo, string name, git_object* target, git_signature* signature, string message);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_tag_create(out GitOid oid, git_repository* repo, string name, git_object* target, git_signature* signature, string message, [MarshalAs(UnmanagedType.Bool)] bool force);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_tag_create_lightweight(out GitOid oid, git_repository* repo, string name, git_object* target, [MarshalAs(UnmanagedType.Bool)] bool force);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_tag_delete(git_repository* repo, string tagName);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_tag_list(out GitStrArray array, git_repository* repo);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_tag_message(git_object* tag);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_tag_name(git_object* tag);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial git_signature* git_tag_tagger(git_object* tag);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitOid* git_tag_target_id(git_object* tag);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitObjectType git_tag_target_type(git_object* tag);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_init();

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_libgit2_shutdown();

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_openssl_set_locking();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void git_trace_cb(LogLevel level, IntPtr message);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_trace_set(LogLevel level, git_trace_cb trace_cb);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_transfer_progress_callback(ref GitTransferProgress stats, IntPtr payload);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int git_transport_cb(out IntPtr transport, IntPtr remote, IntPtr payload);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate int git_transport_certificate_check_cb(git_certificate* cert, int valid, IntPtr hostname, IntPtr payload);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_transport_register(string prefix, IntPtr transport_cb, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_transport_smart(out IntPtr transport, IntPtr remote, IntPtr definition);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_transport_smart_certificate_check(IntPtr transport, IntPtr cert, int valid, string hostname);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_transport_smart_credentials(out IntPtr cred_out, IntPtr transport, string user, int methods);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_transport_unregister(string prefix);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial uint git_tree_entry_filemode(git_tree_entry* entry);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial git_tree_entry* git_tree_entry_byindex(git_object* tree, UIntPtr idx);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_tree_entry_bypath(out git_tree_entry* tree, git_object* root, string treeentry_path);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_tree_entry_free(git_tree_entry* treeEntry);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitOid* git_tree_entry_id(git_tree_entry* entry);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        [return: MarshalUsing(typeof(NoCleanupUtf8StringMarshaller))]
        internal static unsafe partial string git_tree_entry_name(git_tree_entry* entry);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial GitObjectType git_tree_entry_type(git_tree_entry* entry);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial UIntPtr git_tree_entrycount(git_object* tree);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_treebuilder_new(out git_treebuilder* builder, git_repository* repo, IntPtr src);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_treebuilder_insert(IntPtr entry_out, git_treebuilder* builder, string treeentry_name, ref GitOid id, uint attributes);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_treebuilder_write(out GitOid id, git_treebuilder* bld);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_treebuilder_free(git_treebuilder* bld);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_blob_is_binary(git_object* blob);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_cherrypick(git_repository* repo, git_object* commit, GitCherryPickOptions options);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_cherrypick_commit(out git_index* index, git_repository* repo, git_object* cherrypick_commit, git_object* our_commit, uint mainline, ref GitMergeOpts options);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial int git_transaction_commit(IntPtr txn);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static partial void git_transaction_free(IntPtr txn);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate int url_resolve_callback(IntPtr url_resolved, IntPtr url, int direction, IntPtr payload);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial void git_worktree_free(git_worktree* worktree);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_worktree_lookup(out git_worktree* reference, git_repository* repo, string name);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_worktree_list(out GitStrArray array, git_repository* repo);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_repository_open_from_worktree(out git_repository* repository, git_worktree* worktree);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_worktree_is_locked(GitBuf reason, git_worktree* worktree);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_worktree_validate(git_worktree* worktree);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_worktree_lock(git_worktree* worktree, string reason);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_worktree_unlock(git_worktree* worktree);

        [LibraryImport(libgit2, StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_worktree_add(out git_worktree* reference, git_repository* repo, string name, string path, git_worktree_add_options options);

        [LibraryImport(libgit2)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        internal static unsafe partial int git_worktree_prune(git_worktree* worktree, git_worktree_prune_options options);
    }
}
