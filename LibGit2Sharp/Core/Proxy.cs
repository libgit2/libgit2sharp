using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using LibGit2Sharp.Core.Handles;
using LibGit2Sharp.Handlers;

// ReSharper disable InconsistentNaming
namespace LibGit2Sharp.Core
{
    internal class Proxy
    {
        #region giterr_

        public static void giterr_set_str(GitErrorCategory error_class, Exception exception)
        {
            if (exception is OutOfMemoryException)
            {
                NativeMethods.giterr_set_oom();
            }
            else
            {
                NativeMethods.giterr_set_str(error_class, exception.Message);
            }
        }

        public static void giterr_set_str(GitErrorCategory error_class, String errorString)
        {
            NativeMethods.giterr_set_str(error_class, errorString);
        }

        #endregion

        #region git_blame_

        public static BlameSafeHandle git_blame_file(
            RepositorySafeHandle repo,
            FilePath path,
            GitBlameOptions options)
        {
            using (ThreadAffinity())
            {
                BlameSafeHandle handle;
                int res = NativeMethods.git_blame_file(out handle, repo, path, options);
                Ensure.ZeroResult(res);
                return handle;
            }
        }

        public static GitBlameHunk git_blame_get_hunk_byindex(BlameSafeHandle blame, uint idx)
        {
            return NativeMethods.git_blame_get_hunk_byindex(blame, idx).MarshalAs<GitBlameHunk>(false);
        }

        public static void git_blame_free(IntPtr blame)
        {
            NativeMethods.git_blame_free(blame);
        }

        #endregion

        #region git_blob_

        public static ObjectId git_blob_create_fromchunks(RepositorySafeHandle repo, FilePath hintpath, NativeMethods.source_callback fileCallback)
        {
            using (ThreadAffinity())
            {
                var oid = new GitOid();
                int res = NativeMethods.git_blob_create_fromchunks(ref oid, repo, hintpath, fileCallback, IntPtr.Zero);

                if (res == (int)GitErrorCode.User)
                {
                    throw new EndOfStreamException("The stream ended unexpectedly");
                }

                Ensure.ZeroResult(res);

                return oid;
            }
        }

        public static ObjectId git_blob_create_fromdisk(RepositorySafeHandle repo, FilePath path)
        {
            using (ThreadAffinity())
            {
                var oid = new GitOid();
                int res = NativeMethods.git_blob_create_fromdisk(ref oid, repo, path);
                Ensure.ZeroResult(res);

                return oid;
            }
        }

        public static ObjectId git_blob_create_fromfile(RepositorySafeHandle repo, FilePath path)
        {
            using (ThreadAffinity())
            {
                var oid = new GitOid();
                int res = NativeMethods.git_blob_create_fromworkdir(ref oid, repo, path);
                Ensure.ZeroResult(res);

                return oid;
            }
        }

        public static UnmanagedMemoryStream git_blob_filtered_content_stream(RepositorySafeHandle repo, ObjectId id, FilePath path, bool check_for_binary_data)
        {
            var buf = new GitBuf();
            var handle = new ObjectSafeWrapper(id, repo).ObjectPtr;

            return new RawContentStream(handle, h =>
            {
                Ensure.ZeroResult(NativeMethods.git_blob_filtered_content(buf, h, path, check_for_binary_data));
                return buf.ptr;
            },
            h => (long)buf.size,
            new[] { buf });
        }

        public static UnmanagedMemoryStream git_blob_rawcontent_stream(RepositorySafeHandle repo, ObjectId id, Int64 size)
        {
            var handle = new ObjectSafeWrapper(id, repo).ObjectPtr;
            return new RawContentStream(handle, NativeMethods.git_blob_rawcontent, h => size);
        }

        public static Int64 git_blob_rawsize(GitObjectSafeHandle obj)
        {
            return NativeMethods.git_blob_rawsize(obj);
        }

        public static bool git_blob_is_binary(GitObjectSafeHandle obj)
        {
            int res = NativeMethods.git_blob_is_binary(obj);
            Ensure.BooleanResult(res);

            return (res == 1);
        }

        #endregion

        #region git_branch_

        public static ReferenceSafeHandle git_branch_create_from_annotated(RepositorySafeHandle repo, string branch_name, string targetIdentifier, bool force)
        {
            using (ThreadAffinity())
            {
                ReferenceSafeHandle reference;

                using (var annotatedCommit = git_annotated_commit_from_revspec(repo, targetIdentifier))
                {
                    int res = NativeMethods.git_branch_create_from_annotated(out reference, repo, branch_name, annotatedCommit, force);
                    Ensure.ZeroResult(res);
                }

                return reference;
            }
        }

        public static void git_branch_delete(ReferenceSafeHandle reference)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_branch_delete(reference);
                Ensure.ZeroResult(res);
            }
        }

        public static IEnumerable<Branch> git_branch_iterator(Repository repo, GitBranchType branchType)
        {
            return git_iterator(
                (out BranchIteratorSafeHandle iter_out) =>
                NativeMethods.git_branch_iterator_new(out iter_out, repo.Handle, branchType),
                (BranchIteratorSafeHandle iter, out ReferenceSafeHandle ref_out, out int res) =>
                    {
                        GitBranchType type_out;
                        res = NativeMethods.git_branch_next(out ref_out, out type_out, iter);
                        return new { BranchType = type_out };
                    },
                (handle, payload) =>
                    {
                        var reference = Reference.BuildFromPtr<Reference>(handle, repo);
                        return new Branch(repo, reference, reference.CanonicalName);
                    }
                );
        }

        public static void git_branch_iterator_free(IntPtr iter)
        {
            NativeMethods.git_branch_iterator_free(iter);
        }

        public static ReferenceSafeHandle git_branch_move(ReferenceSafeHandle reference, string new_branch_name, bool force)
        {
            using (ThreadAffinity())
            {
                ReferenceSafeHandle ref_out;
                int res = NativeMethods.git_branch_move(out ref_out, reference, new_branch_name, force);
                Ensure.ZeroResult(res);
                return ref_out;
            }
        }

        public static string git_branch_remote_name(RepositorySafeHandle repo, string canonical_branch_name, bool shouldThrowIfNotFound)
        {
            using (ThreadAffinity())
            using (var buf = new GitBuf())
            {
                int res = NativeMethods.git_branch_remote_name(buf, repo, canonical_branch_name);

                if (!shouldThrowIfNotFound &&
                    (res == (int) GitErrorCode.NotFound || res == (int) GitErrorCode.Ambiguous))
                {
                    return null;
                }

                Ensure.ZeroResult(res);
                return LaxUtf8Marshaler.FromNative(buf.ptr);
            }
        }

        public static string git_branch_upstream_name(RepositorySafeHandle handle, string canonicalReferenceName)
        {
            using (ThreadAffinity())
            using (var buf = new GitBuf())
            {
                int res = NativeMethods.git_branch_upstream_name(buf, handle, canonicalReferenceName);
                if (res == (int) GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.ZeroResult(res);
                return LaxUtf8Marshaler.FromNative(buf.ptr);
            }
        }

        #endregion

        #region git_buf_

        public static void git_buf_free(GitBuf buf)
        {
            NativeMethods.git_buf_free(buf);
        }

        #endregion

        #region git_checkout_

        public static void git_checkout_tree(
            RepositorySafeHandle repo,
            ObjectId treeId,
            ref GitCheckoutOpts opts)
        {
            using (ThreadAffinity())
            using (var osw = new ObjectSafeWrapper(treeId, repo))
            {
                int res = NativeMethods.git_checkout_tree(repo, osw.ObjectPtr, ref opts);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_checkout_index(RepositorySafeHandle repo, GitObjectSafeHandle treeish, ref GitCheckoutOpts opts)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_checkout_index(repo, treeish, ref opts);
                Ensure.ZeroResult(res);
            }
        }

        #endregion

        #region git_cherry_pick_

        internal static void git_cherrypick(RepositorySafeHandle repo, ObjectId commit, GitCherryPickOptions options)
        {
            using (ThreadAffinity())
            using (var nativeCommit = git_object_lookup(repo, commit, GitObjectType.Commit))
            {
                int res = NativeMethods.git_cherrypick(repo, nativeCommit, options);
                Ensure.ZeroResult(res);
            }
        }
        #endregion

        #region git_clone_

        public static RepositorySafeHandle git_clone(
            string url,
            string workdir,
            ref GitCloneOptions opts)
        {
            using (ThreadAffinity())
            {
                RepositorySafeHandle repo;
                int res = NativeMethods.git_clone(out repo, url, workdir, ref opts);
                Ensure.ZeroResult(res);
                return repo;
            }
        }

        #endregion

        #region git_commit_

        public static Signature git_commit_author(GitObjectSafeHandle obj)
        {
            return new Signature(NativeMethods.git_commit_author(obj));
        }

        public static Signature git_commit_committer(GitObjectSafeHandle obj)
        {
            return new Signature(NativeMethods.git_commit_committer(obj));
        }

        public static ObjectId git_commit_create(
            RepositorySafeHandle repo,
            string referenceName,
            Signature author,
            Signature committer,
            string message,
            Tree tree,
            GitOid[] parentIds)
        {
            using (ThreadAffinity())
            using (SignatureSafeHandle authorHandle = author.BuildHandle())
            using (SignatureSafeHandle committerHandle = committer.BuildHandle())
            using (var parentPtrs = new ArrayMarshaler<GitOid>(parentIds))
            {
                GitOid commitOid;

                var treeOid = tree.Id.Oid;

                int res = NativeMethods.git_commit_create_from_ids(
                    out commitOid, repo, referenceName, authorHandle,
                    committerHandle, null, message,
                    ref treeOid, (UIntPtr)parentPtrs.Count, parentPtrs.ToArray());

                Ensure.ZeroResult(res);

                return commitOid;
            }
        }

        public static string git_commit_message(GitObjectSafeHandle obj)
        {
            return NativeMethods.git_commit_message(obj);
        }

        public static string git_commit_summary(GitObjectSafeHandle obj)
        {
            return NativeMethods.git_commit_summary(obj);
        }

        public static string git_commit_message_encoding(GitObjectSafeHandle obj)
        {
            return NativeMethods.git_commit_message_encoding(obj);
        }

        public static ObjectId git_commit_parent_id(GitObjectSafeHandle obj, uint i)
        {
            return NativeMethods.git_commit_parent_id(obj, i).MarshalAsObjectId();
        }

        public static int git_commit_parentcount(RepositorySafeHandle repo, ObjectId id)
        {
            using (var obj = new ObjectSafeWrapper(id, repo))
            {
                return git_commit_parentcount(obj);
            }
        }

        public static int git_commit_parentcount(ObjectSafeWrapper obj)
        {
            return (int)NativeMethods.git_commit_parentcount(obj.ObjectPtr);
        }

        public static ObjectId git_commit_tree_id(GitObjectSafeHandle obj)
        {
            return NativeMethods.git_commit_tree_id(obj).MarshalAsObjectId();
        }

        #endregion

        #region git_config_

        public static void git_config_add_file_ondisk(ConfigurationSafeHandle config, FilePath path, ConfigurationLevel level)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_config_add_file_ondisk(config, path, (uint)level, true);
                Ensure.ZeroResult(res);
            }
        }

        public static bool git_config_delete(ConfigurationSafeHandle config, string name)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_config_delete_entry(config, name);

                if (res == (int)GitErrorCode.NotFound)
                {
                    return false;
                }

                Ensure.ZeroResult(res);
                return true;
            }
        }

        public static FilePath git_config_find_global()
        {
            return ConvertPath(NativeMethods.git_config_find_global);
        }

        public static FilePath git_config_find_system()
        {
            return ConvertPath(NativeMethods.git_config_find_system);
        }

        public static FilePath git_config_find_xdg()
        {
            return ConvertPath(NativeMethods.git_config_find_xdg);
        }

        public static void git_config_free(IntPtr config)
        {
            NativeMethods.git_config_free(config);
        }

        public static void git_config_entry_free(IntPtr entry)
        {
            NativeMethods.git_config_entry_free(entry);
        }

        public static ConfigurationEntry<T> git_config_get_entry<T>(ConfigurationSafeHandle config, string key)
        {
            GitConfigEntryHandle handle = null;

            if (!configurationParser.ContainsKey(typeof(T)))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Generic Argument of type '{0}' is not supported.", typeof(T).FullName));
            }

            GitConfigEntry entry;

            try
            {
                using (ThreadAffinity())
                {
                    var res = NativeMethods.git_config_get_entry(out handle, config, key);
                    if (res == (int)GitErrorCode.NotFound)
                    {
                        return null;
                    }

                    Ensure.ZeroResult(res);

                    entry = handle.MarshalAsGitConfigEntry();
                }
            }
            finally
            {
                handle.SafeDispose();
            }

            return new ConfigurationEntry<T>(LaxUtf8Marshaler.FromNative(entry.namePtr),
                (T)configurationParser[typeof(T)](LaxUtf8Marshaler.FromNative(entry.valuePtr)),
                (ConfigurationLevel)entry.level);
        }

        public static ConfigurationSafeHandle git_config_new()
        {
            using (ThreadAffinity())
            {
                ConfigurationSafeHandle handle;
                int res = NativeMethods.git_config_new(out handle);
                Ensure.ZeroResult(res);

                return handle;
            }
        }

        public static ConfigurationSafeHandle git_config_open_level(ConfigurationSafeHandle parent, ConfigurationLevel level)
        {
            using (ThreadAffinity())
            {
                ConfigurationSafeHandle handle;
                int res = NativeMethods.git_config_open_level(out handle, parent, (uint)level);

                if (res == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.ZeroResult(res);

                return handle;
            }
        }

        public static bool git_config_parse_bool(string value)
        {
            using (ThreadAffinity())
            {
                bool outVal;
                var res = NativeMethods.git_config_parse_bool(out outVal, value);

                Ensure.ZeroResult(res);
                return outVal;
            }
        }

        public static int git_config_parse_int32(string value)
        {
            using (ThreadAffinity())
            {
                int outVal;
                var res = NativeMethods.git_config_parse_int32(out outVal, value);

                Ensure.ZeroResult(res);
                return outVal;
            }
        }

        public static long git_config_parse_int64(string value)
        {
            using (ThreadAffinity())
            {
                long outVal;
                var res = NativeMethods.git_config_parse_int64(out outVal, value);

                Ensure.ZeroResult(res);
                return outVal;
            }
        }

        public static void git_config_set_bool(ConfigurationSafeHandle config, string name, bool value)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_config_set_bool(config, name, value);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_config_set_int32(ConfigurationSafeHandle config, string name, int value)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_config_set_int32(config, name, value);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_config_set_int64(ConfigurationSafeHandle config, string name, long value)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_config_set_int64(config, name, value);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_config_set_string(ConfigurationSafeHandle config, string name, string value)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_config_set_string(config, name, value);
                Ensure.ZeroResult(res);
            }
        }

        public static ICollection<TResult> git_config_foreach<TResult>(
            ConfigurationSafeHandle config,
            Func<IntPtr, TResult> resultSelector)
        {
            return git_foreach(resultSelector, c => NativeMethods.git_config_foreach(config, (e, p) => c(e, p), IntPtr.Zero));
        }

        public static IEnumerable<ConfigurationEntry<string>> git_config_iterator_glob(
            ConfigurationSafeHandle config,
            string regexp,
            Func<IntPtr, ConfigurationEntry<string>> resultSelector)
        {
            return git_iterator(
                (out ConfigurationIteratorSafeHandle iter) =>
                NativeMethods.git_config_iterator_glob_new(out iter, config, regexp),
                (ConfigurationIteratorSafeHandle iter, out SafeHandleBase handle, out int res) =>
                    {
                        handle = null;

                        IntPtr entry;
                        res = NativeMethods.git_config_next(out entry, iter);
                        return new { EntryPtr = entry };
                    },
                (handle, payload) => resultSelector(payload.EntryPtr)
                );
        }

        public static void git_config_iterator_free(IntPtr iter)
        {
            NativeMethods.git_config_iterator_free(iter);
        }

        public static ConfigurationSafeHandle git_config_snapshot(ConfigurationSafeHandle config)
        {
            using (ThreadAffinity())
            {
                ConfigurationSafeHandle handle;
                int res = NativeMethods.git_config_snapshot(out handle, config);
                Ensure.ZeroResult(res);

                return handle;
            }
        }

        #endregion

        #region git_describe_

        public static string git_describe_commit(
            RepositorySafeHandle repo,
            ObjectId committishId,
            DescribeOptions options)
        {
            Ensure.ArgumentPositiveInt32(options.MinimumCommitIdAbbreviatedSize,
                "options.MinimumCommitIdAbbreviatedSize");

            using (ThreadAffinity())
            using (var osw = new ObjectSafeWrapper(committishId, repo))
            {
                GitDescribeOptions opts = new GitDescribeOptions
                {
                    Version = 1,
                    DescribeStrategy = options.Strategy,
                    MaxCandidatesTags = 10,
                    OnlyFollowFirstParent = false,
                    ShowCommitOidAsFallback = options.UseCommitIdAsFallback,
                };

                DescribeResultSafeHandle describeHandle = null;

                try
                {
                    int res = NativeMethods.git_describe_commit(out describeHandle, osw.ObjectPtr, ref opts);
                    Ensure.ZeroResult(res);

                    using (var buf = new GitBuf())
                    {
                        GitDescribeFormatOptions formatOptions = new GitDescribeFormatOptions
                        {
                            Version = 1,
                            MinAbbreviatedSize = (uint)options.MinimumCommitIdAbbreviatedSize,
                            AlwaysUseLongFormat = options.AlwaysRenderLongFormat,
                        };

                        res = NativeMethods.git_describe_format(buf, describeHandle, ref formatOptions);
                        Ensure.ZeroResult(res);

                        describeHandle.Dispose();
                        return LaxUtf8Marshaler.FromNative(buf.ptr);
                    }
                }
                finally
                {
                    if (describeHandle != null)
                    {
                        describeHandle.Dispose();
                    }
                }
            }
        }

        public static void git_describe_free(IntPtr iter)
        {
            NativeMethods.git_describe_result_free(iter);
        }

        #endregion

        #region git_diff_

        public static void git_diff_blobs(
            RepositorySafeHandle repo,
            ObjectId oldBlob,
            ObjectId newBlob,
            GitDiffOptions options,
            NativeMethods.git_diff_file_cb fileCallback,
            NativeMethods.git_diff_hunk_cb hunkCallback,
            NativeMethods.git_diff_line_cb lineCallback)
        {
            using (ThreadAffinity())
            using (var osw1 = new ObjectSafeWrapper(oldBlob, repo, true))
            using (var osw2 = new ObjectSafeWrapper(newBlob, repo, true))
            {
                int res = NativeMethods.git_diff_blobs(
                    osw1.ObjectPtr, null, osw2.ObjectPtr, null,
                    options, fileCallback, hunkCallback, lineCallback, IntPtr.Zero);

                Ensure.ZeroResult(res);
            }
        }

        public static void git_diff_foreach(
            DiffSafeHandle diff,
            NativeMethods.git_diff_file_cb fileCallback,
            NativeMethods.git_diff_hunk_cb hunkCallback,
            NativeMethods.git_diff_line_cb lineCallback)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_diff_foreach(diff, fileCallback, hunkCallback, lineCallback, IntPtr.Zero);
                Ensure.ZeroResult(res);
            }
        }

        public static DiffSafeHandle git_diff_tree_to_index(
            RepositorySafeHandle repo,
            IndexSafeHandle index,
            ObjectId oldTree,
            GitDiffOptions options)
        {
            using (ThreadAffinity())
            using (var osw = new ObjectSafeWrapper(oldTree, repo, true))
            {
                DiffSafeHandle diff;
                int res = NativeMethods.git_diff_tree_to_index(out diff, repo, osw.ObjectPtr, index, options);
                Ensure.ZeroResult(res);

                return diff;
            }
        }

        public static void git_diff_free(IntPtr diff)
        {
            NativeMethods.git_diff_free(diff);
        }

        public static void git_diff_merge(DiffSafeHandle onto, DiffSafeHandle from)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_diff_merge(onto, from);
                Ensure.ZeroResult(res);
            }
        }

        public static DiffSafeHandle git_diff_tree_to_tree(
            RepositorySafeHandle repo,
            ObjectId oldTree,
            ObjectId newTree,
            GitDiffOptions options)
        {
            using (ThreadAffinity())
            using (var osw1 = new ObjectSafeWrapper(oldTree, repo, true))
            using (var osw2 = new ObjectSafeWrapper(newTree, repo, true))
            {
                DiffSafeHandle diff;
                int res = NativeMethods.git_diff_tree_to_tree(out diff, repo, osw1.ObjectPtr, osw2.ObjectPtr, options);
                Ensure.ZeroResult(res);

                return diff;
            }
        }

        public static DiffSafeHandle git_diff_index_to_workdir(
            RepositorySafeHandle repo,
            IndexSafeHandle index,
            GitDiffOptions options)
        {
            using (ThreadAffinity())
            {
                DiffSafeHandle diff;
                int res = NativeMethods.git_diff_index_to_workdir(out diff, repo, index, options);
                Ensure.ZeroResult(res);

                return diff;
            }
        }

        public static DiffSafeHandle git_diff_tree_to_workdir(
           RepositorySafeHandle repo,
           ObjectId oldTree,
           GitDiffOptions options)
        {
            using (ThreadAffinity())
            using (var osw = new ObjectSafeWrapper(oldTree, repo, true))
            {
                DiffSafeHandle diff;
                int res = NativeMethods.git_diff_tree_to_workdir(out diff, repo, osw.ObjectPtr, options);
                Ensure.ZeroResult(res);

                return diff;
            }
        }

        public static void git_diff_find_similar(DiffSafeHandle diff, GitDiffFindOptions options)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_diff_find_similar(diff, options);
                Ensure.ZeroResult(res);
            }
        }

        public static int git_diff_num_deltas(DiffSafeHandle diff)
        {
            return (int)NativeMethods.git_diff_num_deltas(diff);
        }

        public static GitDiffDelta git_diff_get_delta(DiffSafeHandle diff, int idx)
        {
            return NativeMethods.git_diff_get_delta(diff, (UIntPtr) idx).MarshalAs<GitDiffDelta>(false);
        }

        #endregion

        #region git_graph_

        public static Tuple<int?, int?> git_graph_ahead_behind(RepositorySafeHandle repo, Commit first, Commit second)
        {
            if (first == null || second == null)
            {
                return new Tuple<int?, int?>(null, null);
            }

            GitOid oid1 = first.Id.Oid;
            GitOid oid2 = second.Id.Oid;

            using (ThreadAffinity())
            {
                UIntPtr ahead;
                UIntPtr behind;

                int res = NativeMethods.git_graph_ahead_behind(out ahead, out behind, repo, ref oid1, ref oid2);

                Ensure.ZeroResult(res);

                return new Tuple<int?, int?>((int)ahead, (int)behind);
            }
        }

        public static bool git_graph_descendant_of(RepositorySafeHandle repo, ObjectId commitId, ObjectId ancestorId)
        {
            GitOid oid1 = commitId.Oid;
            GitOid oid2 = ancestorId.Oid;

            using (ThreadAffinity())
            {
                int res = NativeMethods.git_graph_descendant_of(repo, ref oid1, ref oid2);

                Ensure.BooleanResult(res);

                return (res == 1);
            }
        }

        #endregion

        #region git_ignore_

        public static void git_ignore_add_rule(RepositorySafeHandle repo, string rules)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_ignore_add_rule(repo, rules);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_ignore_clear_internal_rules(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_ignore_clear_internal_rules(repo);
                Ensure.ZeroResult(res);
            }
        }

        public static bool git_ignore_path_is_ignored(RepositorySafeHandle repo, string path)
        {
            using (ThreadAffinity())
            {
                int ignored;
                int res = NativeMethods.git_ignore_path_is_ignored(out ignored, repo, path);
                Ensure.ZeroResult(res);

                return (ignored != 0);
            }
        }

        #endregion

        #region git_index_

        public static void git_index_add(IndexSafeHandle index, GitIndexEntry entry)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_index_add(index, entry);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_index_add_bypath(IndexSafeHandle index, FilePath path)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_index_add_bypath(index, path);
                Ensure.ZeroResult(res);
            }
        }

        public static Conflict git_index_conflict_get(
            IndexSafeHandle index,
            Repository repo,
            FilePath path)
        {
            IndexEntrySafeHandle ancestor, ours, theirs;

            int res = NativeMethods.git_index_conflict_get(
                out ancestor, out ours, out theirs, index, path);

            if (res == (int)GitErrorCode.NotFound)
            {
                return null;
            }

            Ensure.ZeroResult(res);

            return new Conflict(
                IndexEntry.BuildFromPtr(ancestor),
                IndexEntry.BuildFromPtr(ours),
                IndexEntry.BuildFromPtr(theirs));
        }

        public static int git_index_entrycount(IndexSafeHandle index)
        {
            UIntPtr count = NativeMethods.git_index_entrycount(index);
            if ((long)count > int.MaxValue)
            {
                throw new LibGit2SharpException("Index entry count exceeds size of int");
            }
            return (int)count;
        }

        public static StageLevel git_index_entry_stage(IndexEntrySafeHandle index)
        {
            return (StageLevel)NativeMethods.git_index_entry_stage(index);
        }

        public static void git_index_free(IntPtr index)
        {
            NativeMethods.git_index_free(index);
        }

        public static IndexEntrySafeHandle git_index_get_byindex(IndexSafeHandle index, UIntPtr n)
        {
            return NativeMethods.git_index_get_byindex(index, n);
        }

        public static IndexEntrySafeHandle git_index_get_bypath(IndexSafeHandle index, FilePath path, int stage)
        {
            IndexEntrySafeHandle handle = NativeMethods.git_index_get_bypath(index, path, stage);

            return handle.IsZero ? null : handle;
        }

        public static bool git_index_has_conflicts(IndexSafeHandle index)
        {
            int res = NativeMethods.git_index_has_conflicts(index);
            Ensure.BooleanResult(res);

            return res != 0;
        }

        public static int git_index_name_entrycount(IndexSafeHandle index)
        {
            uint count = NativeMethods.git_index_name_entrycount(index);
            if ((long)count > int.MaxValue)
            {
                throw new LibGit2SharpException("Index name entry count exceeds size of int");
            }
            return (int)count;
        }

        public static IndexNameEntrySafeHandle git_index_name_get_byindex(IndexSafeHandle index, UIntPtr n)
        {
            return NativeMethods.git_index_name_get_byindex(index, n);
        }

        public static IndexSafeHandle git_index_open(FilePath indexpath)
        {
            using (ThreadAffinity())
            {
                IndexSafeHandle handle;
                int res = NativeMethods.git_index_open(out handle, indexpath);
                Ensure.ZeroResult(res);

                return handle;
            }
        }

        public static void git_index_read(IndexSafeHandle index)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_index_read(index, false);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_index_remove_bypath(IndexSafeHandle index, FilePath path)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_index_remove_bypath(index, path);
                Ensure.ZeroResult(res);
            }
        }

        public static int git_index_reuc_entrycount(IndexSafeHandle index)
        {
            uint count = NativeMethods.git_index_reuc_entrycount(index);
            if ((long)count > int.MaxValue)
            {
                throw new LibGit2SharpException("Index REUC entry count exceeds size of int");
            }
            return (int)count;
        }

        public static IndexReucEntrySafeHandle git_index_reuc_get_byindex(IndexSafeHandle index, UIntPtr n)
        {
            return NativeMethods.git_index_reuc_get_byindex(index, n);
        }

        public static IndexReucEntrySafeHandle git_index_reuc_get_bypath(IndexSafeHandle index, string path)
        {
            return NativeMethods.git_index_reuc_get_bypath(index, path);
        }

        public static void git_index_write(IndexSafeHandle index)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_index_write(index);
                Ensure.ZeroResult(res);
            }
        }

        public static ObjectId git_tree_create_fromindex(Index index)
        {
            using (ThreadAffinity())
            {
                GitOid treeOid;
                int res = NativeMethods.git_index_write_tree(out treeOid, index.Handle);
                Ensure.ZeroResult(res);

                return treeOid;
            }
        }

        public static void git_index_read_fromtree(Index index, GitObjectSafeHandle tree)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_index_read_tree(index.Handle, tree);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_index_clear(Index index)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_index_clear(index.Handle);
                Ensure.ZeroResult(res);
            }
        }

        #endregion

        #region git_merge_

        public static IndexSafeHandle git_merge_trees(RepositorySafeHandle repo, GitObjectSafeHandle ancestorTree, GitObjectSafeHandle ourTree, GitObjectSafeHandle theirTree)
        {
            using (ThreadAffinity())
            {
                IndexSafeHandle index;
                GitMergeOpts opts = new GitMergeOpts { Version = 1 };
                int res = NativeMethods.git_merge_trees(out index, repo, ancestorTree, ourTree, theirTree, ref opts);
                Ensure.ZeroResult(res);

                return index;
            }
        }

        public static ObjectId git_merge_base_many(RepositorySafeHandle repo, GitOid[] commitIds)
        {
            using (ThreadAffinity())
            {
                GitOid ret;
                int res = NativeMethods.git_merge_base_many(out ret, repo, commitIds.Length, commitIds);

                if (res == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.ZeroResult(res);

                return ret;
            }
        }

        public static ObjectId git_merge_base_octopus(RepositorySafeHandle repo, GitOid[] commitIds)
        {
            using (ThreadAffinity())
            {
                GitOid ret;
                int res = NativeMethods.git_merge_base_octopus(out ret, repo, commitIds.Length, commitIds);

                if (res == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.ZeroResult(res);

                return ret;
            }
        }

        public static GitAnnotatedCommitHandle git_annotated_commit_from_fetchhead(RepositorySafeHandle repo, string branchName, string remoteUrl, GitOid oid)
        {
            using (ThreadAffinity())
            {
                GitAnnotatedCommitHandle merge_head;

                int res = NativeMethods.git_annotated_commit_from_fetchhead(out merge_head, repo, branchName, remoteUrl, ref oid);

                Ensure.ZeroResult(res);

                return merge_head;
            }
        }

        public static GitAnnotatedCommitHandle git_annotated_commit_lookup(RepositorySafeHandle repo, GitOid oid)
        {
            using (ThreadAffinity())
            {
                GitAnnotatedCommitHandle their_head;

                int res = NativeMethods.git_annotated_commit_lookup(out their_head, repo, ref oid);

                Ensure.ZeroResult(res);

                return their_head;
            }
        }

        public static GitAnnotatedCommitHandle git_annotated_commit_from_ref(RepositorySafeHandle repo, ReferenceSafeHandle reference)
        {
            using (ThreadAffinity())
            {
                GitAnnotatedCommitHandle their_head;

                int res = NativeMethods.git_annotated_commit_from_ref(out their_head, repo, reference);

                Ensure.ZeroResult(res);

                return their_head;
            }
        }

        public static GitAnnotatedCommitHandle git_annotated_commit_from_revspec(RepositorySafeHandle repo, string revspec)
        {
            using (ThreadAffinity())
            {
                GitAnnotatedCommitHandle their_head;

                int res = NativeMethods.git_annotated_commit_from_revspec(out their_head, repo, revspec);

                Ensure.ZeroResult(res);

                return their_head;
            }
        }

        public static ObjectId git_annotated_commit_id(GitAnnotatedCommitHandle mergeHead)
        {
            return NativeMethods.git_annotated_commit_id(mergeHead).MarshalAsObjectId();
        }

        public static void git_merge(RepositorySafeHandle repo, GitAnnotatedCommitHandle[] heads, GitMergeOpts mergeOptions, GitCheckoutOpts checkoutOptions)
        {
            using (ThreadAffinity())
            {
                IntPtr[] their_heads = heads.Select(head => head.DangerousGetHandle()).ToArray();

                int res = NativeMethods.git_merge(
                    repo,
                    their_heads,
                    (UIntPtr)their_heads.Length,
                    ref mergeOptions,
                    ref checkoutOptions);

                Ensure.ZeroResult(res);
            }
        }

        public static void git_merge_analysis(
            RepositorySafeHandle repo,
            GitAnnotatedCommitHandle[] heads,
            out GitMergeAnalysis analysis_out,
            out GitMergePreference preference_out)
        {
            using (ThreadAffinity())
            {
                IntPtr[] their_heads = heads.Select(head => head.DangerousGetHandle()).ToArray();

                int res = NativeMethods.git_merge_analysis(
                    out analysis_out,
                    out preference_out,
                    repo,
                    their_heads,
                    their_heads.Length);

                Ensure.ZeroResult(res);
            }
        }

        public static void git_annotated_commit_free(IntPtr handle)
        {
            NativeMethods.git_annotated_commit_free(handle);
        }

        #endregion

        #region git_message_

        public static string git_message_prettify(string message, char? commentChar)
        {
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            int comment = commentChar.GetValueOrDefault();
            if (comment > sbyte.MaxValue)
            {
                throw new InvalidOperationException("Only single byte characters are allowed as commentary characters in a message (eg. '#').");
            }

            using (ThreadAffinity())
            using (var buf = new GitBuf())
            {
                int res = NativeMethods.git_message_prettify(buf, message, false, (sbyte)comment);
                Ensure.Int32Result(res);

                return LaxUtf8Marshaler.FromNative(buf.ptr) ?? string.Empty;
            }
        }

        #endregion

        #region git_note_

        public static ObjectId git_note_create(
            RepositorySafeHandle repo,
            string notes_ref,
            Signature author,
            Signature committer,
            ObjectId targetId,
            string note,
            bool force)
        {
            using (ThreadAffinity())
            using (SignatureSafeHandle authorHandle = author.BuildHandle())
            using (SignatureSafeHandle committerHandle = committer.BuildHandle())
            {
                GitOid noteOid;
                GitOid oid = targetId.Oid;

                int res = NativeMethods.git_note_create(out noteOid, repo, notes_ref, authorHandle, committerHandle, ref oid, note, force ? 1 : 0);
                Ensure.ZeroResult(res);

                return noteOid;
            }
        }

        public static string git_note_default_ref(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                string notes_ref;
                int res = NativeMethods.git_note_default_ref(out notes_ref, repo);
                Ensure.ZeroResult(res);

                return notes_ref;
            }
        }

        public static ICollection<TResult> git_note_foreach<TResult>(RepositorySafeHandle repo, string notes_ref, Func<GitOid, GitOid, TResult> resultSelector)
        {
            return git_foreach(resultSelector, c => NativeMethods.git_note_foreach(repo, notes_ref,
                (ref GitOid x, ref GitOid y, IntPtr p) => c(x, y, p), IntPtr.Zero));
        }

        public static void git_note_free(IntPtr note)
        {
            NativeMethods.git_note_free(note);
        }

        public static string git_note_message(NoteSafeHandle note)
        {
            return NativeMethods.git_note_message(note);
        }

        public static ObjectId git_note_id(NoteSafeHandle note)
        {
            return NativeMethods.git_note_id(note).MarshalAsObjectId();
        }

        public static NoteSafeHandle git_note_read(RepositorySafeHandle repo, string notes_ref, ObjectId id)
        {
            using (ThreadAffinity())
            {
                GitOid oid = id.Oid;
                NoteSafeHandle note;

                int res = NativeMethods.git_note_read(out note, repo, notes_ref, ref oid);

                if (res == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.ZeroResult(res);

                return note;
            }
        }

        public static void git_note_remove(RepositorySafeHandle repo, string notes_ref, Signature author, Signature committer, ObjectId targetId)
        {
            using (ThreadAffinity())
            using (SignatureSafeHandle authorHandle = author.BuildHandle())
            using (SignatureSafeHandle committerHandle = committer.BuildHandle())
            {
                GitOid oid = targetId.Oid;

                int res = NativeMethods.git_note_remove(repo, notes_ref, authorHandle, committerHandle, ref oid);

                if (res == (int)GitErrorCode.NotFound)
                {
                    return;
                }

                Ensure.ZeroResult(res);
            }
        }

        #endregion

        #region git_object_

        public static ObjectId git_object_id(GitObjectSafeHandle obj)
        {
            return NativeMethods.git_object_id(obj).MarshalAsObjectId();
        }

        public static void git_object_free(IntPtr obj)
        {
            NativeMethods.git_object_free(obj);
        }

        public static GitObjectSafeHandle git_object_lookup(RepositorySafeHandle repo, ObjectId id, GitObjectType type)
        {
            using (ThreadAffinity())
            {
                GitObjectSafeHandle handle;
                GitOid oid = id.Oid;

                int res = NativeMethods.git_object_lookup(out handle, repo, ref oid, type);
                switch (res)
                {
                    case (int)GitErrorCode.NotFound:
                        return null;

                    default:
                        Ensure.ZeroResult(res);
                        break;
                }

                return handle;
            }
        }

        public static GitObjectSafeHandle git_object_peel(RepositorySafeHandle repo, ObjectId id, GitObjectType type, bool throwsIfCanNotPeel)
        {
            using (ThreadAffinity())
            {
                GitObjectSafeHandle peeled;
                int res;

                using (var obj = new ObjectSafeWrapper(id, repo))
                {
                    res = NativeMethods.git_object_peel(out peeled, obj.ObjectPtr, type);
                }

                if (!throwsIfCanNotPeel &&
                    (res == (int)GitErrorCode.NotFound || res == (int)GitErrorCode.Ambiguous || res == (int)GitErrorCode.InvalidSpecification || res == (int)GitErrorCode.Peel))
                {
                    return null;
                }

                Ensure.ZeroResult(res);
                return peeled;
            }
        }

        public static string git_object_short_id(RepositorySafeHandle repo, ObjectId id)
        {
            using (ThreadAffinity())
            using (var obj = new ObjectSafeWrapper(id, repo))
            using (var buf = new GitBuf())
            {
                int res = NativeMethods.git_object_short_id(buf, obj.ObjectPtr);
                Ensure.Int32Result(res);

                return LaxUtf8Marshaler.FromNative(buf.ptr);
            }
        }

        public static GitObjectType git_object_type(GitObjectSafeHandle obj)
        {
            return NativeMethods.git_object_type(obj);
        }

        #endregion

        #region git_odb_

        public static void git_odb_add_backend(ObjectDatabaseSafeHandle odb, IntPtr backend, int priority)
        {
            Ensure.ZeroResult(NativeMethods.git_odb_add_backend(odb, backend, priority));
        }

        public static IntPtr git_odb_backend_malloc(IntPtr backend, UIntPtr len)
        {
            IntPtr toReturn = NativeMethods.git_odb_backend_malloc(backend, len);

            if (IntPtr.Zero == toReturn)
            {
                throw new LibGit2SharpException(String.Format(CultureInfo.InvariantCulture,
                                                              "Unable to allocate {0} bytes; out of memory",
                                                              len),
                                                GitErrorCode.Error, GitErrorCategory.NoMemory);
            }

            return toReturn;
        }

        public static bool git_odb_exists(ObjectDatabaseSafeHandle odb, ObjectId id)
        {
            using (ThreadAffinity())
            {
                GitOid oid = id.Oid;

                int res = NativeMethods.git_odb_exists(odb, ref oid);
                Ensure.BooleanResult(res);

                return (res == 1);
            }
        }

        public static GitObjectMetadata git_odb_read_header(ObjectDatabaseSafeHandle odb, ObjectId id)
        {
            using (ThreadAffinity())
            {
                GitOid oid = id.Oid;
                UIntPtr length;
                GitObjectType objectType;

                int res = NativeMethods.git_odb_read_header(out length, out objectType, odb, ref oid);
                Ensure.ZeroResult(res);

                return new GitObjectMetadata((long)length, objectType);
            }
        }

        public static ICollection<TResult> git_odb_foreach<TResult>(
            ObjectDatabaseSafeHandle odb,
            Func<IntPtr, TResult> resultSelector)
        {
            return git_foreach(
                resultSelector,
                c => NativeMethods.git_odb_foreach(
                    odb,
                    (x, p) => c(x, p),
                    IntPtr.Zero));
        }

        public static OdbStreamSafeHandle git_odb_open_wstream(ObjectDatabaseSafeHandle odb, UIntPtr size, GitObjectType type)
        {
            using (ThreadAffinity())
            {
                OdbStreamSafeHandle stream;
                int res = NativeMethods.git_odb_open_wstream(out stream, odb, size, type);
                Ensure.ZeroResult(res);

                return stream;
            }
        }

        public static void git_odb_free(IntPtr odb)
        {
            NativeMethods.git_odb_free(odb);
        }

        public static void git_odb_stream_write(OdbStreamSafeHandle stream, byte[] data, int len)
        {
            using (ThreadAffinity())
            {
                int res;
                unsafe
                {
                    fixed (byte *p = data)
                    {
                        res = NativeMethods.git_odb_stream_write(stream, (IntPtr) p, (UIntPtr) len);
                    }
                }

                Ensure.ZeroResult(res);
            }
        }

        public static ObjectId git_odb_stream_finalize_write(OdbStreamSafeHandle stream)
        {
            using (ThreadAffinity())
            {
                GitOid id;
                int res = NativeMethods.git_odb_stream_finalize_write(out id, stream);
                Ensure.ZeroResult(res);

                return id;
            }
        }

        public static void git_odb_stream_free(IntPtr stream)
        {
            NativeMethods.git_odb_stream_free(stream);
        }

        #endregion

        #region git_patch_

        public static void git_patch_free(IntPtr patch)
        {
            NativeMethods.git_patch_free(patch);
        }

        public static PatchSafeHandle git_patch_from_diff(DiffSafeHandle diff, int idx)
        {
            using (ThreadAffinity())
            {
                PatchSafeHandle handle;
                int res = NativeMethods.git_patch_from_diff(out handle, diff, (UIntPtr) idx);
                Ensure.ZeroResult(res);
                return handle;
            }
        }

        public static void git_patch_print(PatchSafeHandle patch, NativeMethods.git_diff_line_cb printCallback)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_patch_print(patch, printCallback, IntPtr.Zero);
                Ensure.ZeroResult(res);
            }
        }

        public static Tuple<int, int> git_patch_line_stats(PatchSafeHandle patch)
        {
            using (ThreadAffinity())
            {
                UIntPtr ctx, add, del;
                int res = NativeMethods.git_patch_line_stats(out ctx, out add, out del, patch);
                Ensure.ZeroResult(res);
                return new Tuple<int, int>((int)add, (int)del);
            }
        }

        #endregion

        #region git_reference_

        public static ReferenceSafeHandle git_reference_create(RepositorySafeHandle repo, string name, ObjectId targetId, bool allowOverwrite,
            string logMessage)
        {
            using (ThreadAffinity())
            {
                GitOid oid = targetId.Oid;
                ReferenceSafeHandle handle;

                int res = NativeMethods.git_reference_create(out handle, repo, name, ref oid, allowOverwrite, logMessage);
                Ensure.ZeroResult(res);

                return handle;
            }
        }

        public static ReferenceSafeHandle git_reference_symbolic_create(RepositorySafeHandle repo, string name, string target, bool allowOverwrite,
            string logMessage)
        {
            using (ThreadAffinity())
            {
                ReferenceSafeHandle handle;
                int res = NativeMethods.git_reference_symbolic_create(out handle, repo, name, target, allowOverwrite, logMessage);
                Ensure.ZeroResult(res);

                return handle;
            }
        }

        public static ICollection<TResult> git_reference_foreach_glob<TResult>(
            RepositorySafeHandle repo,
            string glob,
            Func<IntPtr, TResult> resultSelector)
        {
            return git_foreach(resultSelector, c => NativeMethods.git_reference_foreach_glob(repo, glob, (x, p) => c(x, p), IntPtr.Zero));
        }

        public static void git_reference_free(IntPtr reference)
        {
            NativeMethods.git_reference_free(reference);
        }

        public static bool git_reference_is_valid_name(string refname)
        {
            int res = NativeMethods.git_reference_is_valid_name(refname);
            Ensure.BooleanResult(res);

            return (res == 1);
        }

        public static IList<string> git_reference_list(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                var array = new GitStrArrayNative();

                try
                {
                    int res = NativeMethods.git_reference_list(out array.Array, repo);
                    Ensure.ZeroResult(res);

                    return array.ReadStrings();
                }
                finally
                {
                    array.Dispose();
                }
            }
        }

        public static ReferenceSafeHandle git_reference_lookup(RepositorySafeHandle repo, string name, bool shouldThrowIfNotFound)
        {
            using (ThreadAffinity())
            {
                ReferenceSafeHandle handle;
                int res = NativeMethods.git_reference_lookup(out handle, repo, name);

                if (!shouldThrowIfNotFound && res == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.ZeroResult(res);

                return handle;
            }
        }

        public static string git_reference_name(ReferenceSafeHandle reference)
        {
            return NativeMethods.git_reference_name(reference);
        }

        public static void git_reference_remove(RepositorySafeHandle repo, string name)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_reference_remove(repo, name);
                Ensure.ZeroResult(res);
            }
        }

        public static ObjectId git_reference_target(ReferenceSafeHandle reference)
        {
            return NativeMethods.git_reference_target(reference).MarshalAsObjectId();
        }

        public static ReferenceSafeHandle git_reference_rename(ReferenceSafeHandle reference, string newName, bool allowOverwrite,
            string logMessage)
        {
            using (ThreadAffinity())
            {
                ReferenceSafeHandle ref_out;

                int res = NativeMethods.git_reference_rename(out ref_out, reference, newName, allowOverwrite, logMessage);
                Ensure.ZeroResult(res);

                return ref_out;
            }
        }

        public static ReferenceSafeHandle git_reference_set_target(ReferenceSafeHandle reference, ObjectId id, string logMessage)
        {
            using (ThreadAffinity())
            {
                GitOid oid = id.Oid;
                ReferenceSafeHandle ref_out;

                int res = NativeMethods.git_reference_set_target(out ref_out, reference, ref oid, logMessage);
                Ensure.ZeroResult(res);

                return ref_out;
            }
        }

        public static ReferenceSafeHandle git_reference_symbolic_set_target(ReferenceSafeHandle reference, string target, string logMessage)
        {
            using (ThreadAffinity())
            {
                ReferenceSafeHandle ref_out;

                int res = NativeMethods.git_reference_symbolic_set_target(out ref_out, reference, target, logMessage);
                Ensure.ZeroResult(res);

                return ref_out;
            }
        }

        public static string git_reference_symbolic_target(ReferenceSafeHandle reference)
        {
            return NativeMethods.git_reference_symbolic_target(reference);
        }

        public static GitReferenceType git_reference_type(ReferenceSafeHandle reference)
        {
            return NativeMethods.git_reference_type(reference);
        }

        public static void git_reference_ensure_log(RepositorySafeHandle repo, string refname)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_reference_ensure_log(repo, refname);
                Ensure.ZeroResult(res);
            }
        }

        #endregion

        #region git_reflog_

        public static void git_reflog_free(IntPtr reflog)
        {
            NativeMethods.git_reflog_free(reflog);
        }

        public static ReflogSafeHandle git_reflog_read(RepositorySafeHandle repo, string canonicalName)
        {
            using (ThreadAffinity())
            {
                ReflogSafeHandle reflog_out;

                int res = NativeMethods.git_reflog_read(out reflog_out, repo, canonicalName);
                Ensure.ZeroResult(res);

                return reflog_out;
            }
        }

        public static int git_reflog_entrycount(ReflogSafeHandle reflog)
        {
            return (int)NativeMethods.git_reflog_entrycount(reflog);
        }

        public static ReflogEntrySafeHandle git_reflog_entry_byindex(ReflogSafeHandle reflog, int idx)
        {
            return NativeMethods.git_reflog_entry_byindex(reflog, (UIntPtr)idx);
        }

        public static ObjectId git_reflog_entry_id_old(SafeHandle entry)
        {
            return NativeMethods.git_reflog_entry_id_old(entry).MarshalAsObjectId();
        }

        public static ObjectId git_reflog_entry_id_new(SafeHandle entry)
        {
            return NativeMethods.git_reflog_entry_id_new(entry).MarshalAsObjectId();
        }

        public static Signature git_reflog_entry_committer(SafeHandle entry)
        {
            return new Signature(NativeMethods.git_reflog_entry_committer(entry));
        }

        public static string git_reflog_entry_message(SafeHandle entry)
        {
            return NativeMethods.git_reflog_entry_message(entry);
        }

        #endregion

        #region git_refspec

        public static string git_refspec_rtransform(GitRefSpecHandle refSpecPtr, string name)
        {
            using (ThreadAffinity())
            using (var buf = new GitBuf())
            {
                int res = NativeMethods.git_refspec_rtransform(buf, refSpecPtr, name);
                Ensure.ZeroResult(res);

                return LaxUtf8Marshaler.FromNative(buf.ptr) ?? string.Empty;
            }
        }

        public static string git_refspec_string(GitRefSpecHandle refSpec)
        {
            return NativeMethods.git_refspec_string(refSpec);
        }

        public static string git_refspec_src(GitRefSpecHandle refSpec)
        {
            return NativeMethods.git_refspec_src(refSpec);
        }

        public static string git_refspec_dst(GitRefSpecHandle refSpec)
        {
            return NativeMethods.git_refspec_dst(refSpec);
        }

        public static RefSpecDirection git_refspec_direction(GitRefSpecHandle refSpec)
        {
            return NativeMethods.git_refspec_direction(refSpec);
        }

        public static bool git_refspec_force(GitRefSpecHandle refSpec)
        {
            return NativeMethods.git_refspec_force(refSpec);
        }

        #endregion

        #region git_remote_

        public static TagFetchMode git_remote_autotag(RemoteSafeHandle remote)
        {
            return (TagFetchMode) NativeMethods.git_remote_autotag(remote);
        }

        public static RemoteSafeHandle git_remote_create(RepositorySafeHandle repo, string name, string url)
        {
            using (ThreadAffinity())
            {
                RemoteSafeHandle handle;
                int res = NativeMethods.git_remote_create(out handle, repo, name, url);
                Ensure.ZeroResult(res);

                return handle;
            }
        }

        public static RemoteSafeHandle git_remote_create_with_fetchspec(RepositorySafeHandle repo, string name, string url, string refspec)
        {
            using (ThreadAffinity())
            {
                RemoteSafeHandle handle;
                int res = NativeMethods.git_remote_create_with_fetchspec(out handle, repo, name, url, refspec);
                Ensure.ZeroResult(res);

                return handle;
            }
        }

        public static RemoteSafeHandle git_remote_create_anonymous(RepositorySafeHandle repo, string url, string refspec)
        {
            using (ThreadAffinity())
            {
                RemoteSafeHandle handle;
                int res = NativeMethods.git_remote_create_anonymous(out handle, repo, url, refspec);
                Ensure.ZeroResult(res);

                return handle;
            }
        }

        public static void git_remote_connect(RemoteSafeHandle remote, GitDirection direction)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_remote_connect(remote, direction);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_remote_delete(RepositorySafeHandle repo, string name)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_remote_delete(repo, name);

                if (res == (int)GitErrorCode.NotFound)
                {
                    return;
                }

                Ensure.ZeroResult(res);
            }
        }

        public static void git_remote_disconnect(RemoteSafeHandle remote)
        {
            using (ThreadAffinity())
            {
                NativeMethods.git_remote_disconnect(remote);
            }
        }

        public static GitRefSpecHandle git_remote_get_refspec(RemoteSafeHandle remote, int n)
        {
            return NativeMethods.git_remote_get_refspec(remote, (UIntPtr)n);
        }

        public static int git_remote_refspec_count(RemoteSafeHandle remote)
        {
            return (int)NativeMethods.git_remote_refspec_count(remote);
        }

        public static IList<string> git_remote_get_fetch_refspecs(RemoteSafeHandle remote)
        {
            using (ThreadAffinity())
            {
                var array = new GitStrArrayNative();

                try
                {
                    int res = NativeMethods.git_remote_get_fetch_refspecs(out array.Array, remote);
                    Ensure.ZeroResult(res);

                    return array.ReadStrings();
                }
                finally
                {
                    array.Dispose();
                }
            }
        }

        public static IList<string> git_remote_get_push_refspecs(RemoteSafeHandle remote)
        {
            using (ThreadAffinity())
            {
                var array = new GitStrArrayNative();

                try
                {
                    int res = NativeMethods.git_remote_get_push_refspecs(out array.Array, remote);
                    Ensure.ZeroResult(res);

                    return array.ReadStrings();
                }
                finally
                {
                    array.Dispose();
                }
            }
        }

        public static void git_remote_push(RemoteSafeHandle remote, IEnumerable<string> refSpecs, GitPushOptions opts)
        {
            using (ThreadAffinity())
            {
                var array = new GitStrArrayManaged();

                try
                {
                    array = GitStrArrayManaged.BuildFrom(refSpecs.ToArray());

                    int res = NativeMethods.git_remote_push(remote, ref array.Array, opts);
                    Ensure.ZeroResult(res);
                }
                finally
                {
                    array.Dispose();
                }
            }
        }

        public static void git_remote_set_fetch_refspecs(RemoteSafeHandle remote, IEnumerable<string> refSpecs)
        {
            using (ThreadAffinity())
            {
                var array = new GitStrArrayManaged();

                try
                {
                    array = GitStrArrayManaged.BuildFrom(refSpecs.ToArray());

                    int res = NativeMethods.git_remote_set_fetch_refspecs(remote, ref array.Array);
                    Ensure.ZeroResult(res);
                }
                finally
                {
                    array.Dispose();
                }
            }
        }

        public static void git_remote_set_push_refspecs(RemoteSafeHandle remote, IEnumerable<string> refSpecs)
        {
            using (ThreadAffinity())
            {
                var array = new GitStrArrayManaged();

                try
                {
                    array = GitStrArrayManaged.BuildFrom(refSpecs.ToArray());

                    int res = NativeMethods.git_remote_set_push_refspecs(remote, ref array.Array);
                    Ensure.ZeroResult(res);
                }
                finally
                {
                    array.Dispose();
                }
            }
        }

        public static void git_remote_set_url(RemoteSafeHandle remote, string url)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_remote_set_url(remote, url);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_remote_set_pushurl(RemoteSafeHandle remote, string url)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_remote_set_pushurl(remote, url);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_remote_fetch(RemoteSafeHandle remote, string logMessage)
        {
            using (ThreadAffinity())
            {
                var array = new GitStrArrayNative();

                try
                {
                    int res = NativeMethods.git_remote_fetch(remote, ref array.Array, logMessage);
                    Ensure.ZeroResult(res);
                }
                finally
                {
                    array.Dispose();
                }
            }
        }

        public static void git_remote_free(IntPtr remote)
        {
            NativeMethods.git_remote_free(remote);
        }

        public static bool git_remote_is_valid_name(string refname)
        {
            int res = NativeMethods.git_remote_is_valid_name(refname);
            Ensure.BooleanResult(res);

            return (res == 1);
        }

        public static IList<string> git_remote_list(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                var array = new GitStrArrayNative();

                try
                {
                    int res = NativeMethods.git_remote_list(out array.Array, repo);
                    Ensure.ZeroResult(res);

                    return array.ReadStrings();
                }
                finally
                {
                    array.Dispose();
                }
            }
        }

        public static IEnumerable<DirectReference> git_remote_ls(Repository repository, RemoteSafeHandle remote)
        {
            IntPtr heads;
            UIntPtr count;

            using (ThreadAffinity())
            {
                int res = NativeMethods.git_remote_ls(out heads, out count, remote);
                Ensure.ZeroResult(res);
            }

            var intCount = (int)count.ToUInt32();

            if (intCount < 0)
            {
                throw new OverflowException();
            }

            var refs = new List<DirectReference>();
            IntPtr currentHead = heads;

            for (int i = 0; i < intCount; i++)
            {
                var remoteHead = Marshal.ReadIntPtr(currentHead).MarshalAs<GitRemoteHead>();

                // The name pointer should never be null - if it is,
                // this indicates a bug somewhere (libgit2, server, etc).
                if (remoteHead.NamePtr == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Not expecting null value for reference name.");
                }

                string name = LaxUtf8Marshaler.FromNative(remoteHead.NamePtr);
                refs.Add(new DirectReference(name, repository, remoteHead.Oid));

                currentHead = IntPtr.Add(currentHead, IntPtr.Size);
            }

            return refs;
        }

        public static RemoteSafeHandle git_remote_lookup(RepositorySafeHandle repo, string name, bool throwsIfNotFound)
        {
            using (ThreadAffinity())
            {
                RemoteSafeHandle handle;
                int res = NativeMethods.git_remote_lookup(out handle, repo, name);

                if (res == (int)GitErrorCode.NotFound && !throwsIfNotFound)
                {
                    return null;
                }

                Ensure.ZeroResult(res);
                return handle;
            }
        }

        public static string git_remote_name(RemoteSafeHandle remote)
        {
            return NativeMethods.git_remote_name(remote);
        }

        public static void git_remote_rename(RepositorySafeHandle repo, string name, string new_name, RemoteRenameFailureHandler callback)
        {
            using (ThreadAffinity())
            {
                if (callback == null)
                {
                    callback = problem => {};
                }

                var array = new GitStrArrayNative();

                try
                {
                    int res = NativeMethods.git_remote_rename(
                        ref array.Array,
                        repo,
                        name,
                        new_name);

                    if (res == (int)GitErrorCode.NotFound)
                    {
                        throw new NotFoundException(
                            string.Format("Remote '{0}' does not exist and cannot be renamed.", name));
                    }

                    Ensure.ZeroResult(res);

                    foreach (var item in array.ReadStrings())
                    {
                        callback(item);
                    }
                }
                finally
                {
                    array.Dispose();
                }
            }
        }

        public static void git_remote_save(RemoteSafeHandle remote)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_remote_save(remote);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_remote_set_autotag(RemoteSafeHandle remote, TagFetchMode value)
        {
            NativeMethods.git_remote_set_autotag(remote, value);
        }

        public static void git_remote_set_callbacks(RemoteSafeHandle remote, ref GitRemoteCallbacks callbacks)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_remote_set_callbacks(remote, ref callbacks);
                Ensure.ZeroResult(res);
            }
        }

        public static string git_remote_url(RemoteSafeHandle remote)
        {
            return NativeMethods.git_remote_url(remote);
        }

        public static string git_remote_pushurl(RemoteSafeHandle remote)
        {
            return NativeMethods.git_remote_pushurl(remote);
        }

        #endregion

        #region git_repository_

        public static FilePath git_repository_discover(FilePath start_path)
        {
            return ConvertPath(buf => NativeMethods.git_repository_discover(buf, start_path, false, null));
        }

        public static bool git_repository_head_detached(RepositorySafeHandle repo)
        {
            return RepositoryStateChecker(repo, NativeMethods.git_repository_head_detached);
        }

        public static ICollection<TResult> git_repository_fetchhead_foreach<TResult>(
            RepositorySafeHandle repo,
            Func<string, string, GitOid, bool, TResult> resultSelector)
        {
            return git_foreach(
                resultSelector,
                c => NativeMethods.git_repository_fetchhead_foreach(
                    repo,
                    (IntPtr w, IntPtr x, ref GitOid y, bool z, IntPtr p)
                        => c(LaxUtf8Marshaler.FromNative(w), LaxUtf8Marshaler.FromNative(x), y, z, p), IntPtr.Zero),
                    GitErrorCode.NotFound);
        }

        public static void git_repository_free(IntPtr repo)
        {
            NativeMethods.git_repository_free(repo);
        }

        public static bool git_repository_head_unborn(RepositorySafeHandle repo)
        {
            return RepositoryStateChecker(repo, NativeMethods.git_repository_head_unborn);
        }

        public static IndexSafeHandle git_repository_index(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                IndexSafeHandle handle;
                int res = NativeMethods.git_repository_index(out handle, repo);
                Ensure.ZeroResult(res);

                return handle;
            }
        }

        public static RepositorySafeHandle git_repository_init_ext(
            FilePath workdirPath,
            FilePath gitdirPath,
            bool isBare)
        {
            using (ThreadAffinity())
            using (var opts = GitRepositoryInitOptions.BuildFrom(workdirPath, isBare))
            {
                RepositorySafeHandle repo;
                int res = NativeMethods.git_repository_init_ext(out repo, gitdirPath, opts);
                Ensure.ZeroResult(res);

                return repo;
            }
        }

        public static bool git_repository_is_bare(RepositorySafeHandle repo)
        {
            return RepositoryStateChecker(repo, NativeMethods.git_repository_is_bare);
        }

        public static bool git_repository_is_shallow(RepositorySafeHandle repo)
        {
            return RepositoryStateChecker(repo, NativeMethods.git_repository_is_shallow);
        }

        public static void git_repository_state_cleanup(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_repository_state_cleanup(repo);
                Ensure.ZeroResult(res);
            }
        }

        public static ICollection<TResult> git_repository_mergehead_foreach<TResult>(
            RepositorySafeHandle repo,
            Func<GitOid, TResult> resultSelector)
        {
            return git_foreach(
                resultSelector,
                c => NativeMethods.git_repository_mergehead_foreach(
                    repo, (ref GitOid x, IntPtr p) => c(x, p), IntPtr.Zero),
                GitErrorCode.NotFound);
        }

        public static string git_repository_message(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            using (var buf = new GitBuf())
            {
                int res = NativeMethods.git_repository_message(buf, repo);
                if (res == (int)GitErrorCode.NotFound)
                {
                    return null;
                }
                Ensure.ZeroResult(res);

                return LaxUtf8Marshaler.FromNative(buf.ptr);
            }
        }

        public static ObjectDatabaseSafeHandle git_repository_odb(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                ObjectDatabaseSafeHandle handle;
                int res = NativeMethods.git_repository_odb(out handle, repo);
                Ensure.ZeroResult(res);

                return handle;
            }
        }

        public static RepositorySafeHandle git_repository_open(string path)
        {
            using (ThreadAffinity())
            {
                RepositorySafeHandle repo;
                int res = NativeMethods.git_repository_open(out repo, path);

                if (res == (int)GitErrorCode.NotFound)
                {
                    throw new RepositoryNotFoundException(String.Format(CultureInfo.InvariantCulture, "Path '{0}' doesn't point at a valid Git repository or workdir.", path));
                }

                Ensure.ZeroResult(res);

                return repo;
            }
        }

        public static void git_repository_open_ext(string path, RepositoryOpenFlags flags, string ceilingDirs)
        {
            using (ThreadAffinity())
            {
                int res;

                using (var repo = new NullRepositorySafeHandle())
                {
                    res = NativeMethods.git_repository_open_ext(repo, path, flags, ceilingDirs);
                }

                if (res == (int)GitErrorCode.NotFound)
                {
                    throw new RepositoryNotFoundException(String.Format(CultureInfo.InvariantCulture, "Path '{0}' doesn't point at a valid Git repository or workdir.", path));
                }

                Ensure.ZeroResult(res);
            }
        }

        public static FilePath git_repository_path(RepositorySafeHandle repo)
        {
            return NativeMethods.git_repository_path(repo);
        }

        public static void git_repository_set_config(RepositorySafeHandle repo, ConfigurationSafeHandle config)
        {
            NativeMethods.git_repository_set_config(repo, config);
        }

        public static void git_repository_set_ident(RepositorySafeHandle repo, string name, string email)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_repository_set_ident(repo, name, email);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_repository_set_index(RepositorySafeHandle repo, IndexSafeHandle index)
        {
            NativeMethods.git_repository_set_index(repo, index);
        }

        public static void git_repository_set_workdir(RepositorySafeHandle repo, FilePath workdir)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_repository_set_workdir(repo, workdir, false);
                Ensure.ZeroResult(res);
            }
        }

        public static CurrentOperation git_repository_state(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_repository_state(repo);
                Ensure.Int32Result(res);
                return (CurrentOperation)res;
            }
        }

        public static FilePath git_repository_workdir(RepositorySafeHandle repo)
        {
            return NativeMethods.git_repository_workdir(repo);
        }

        public static void git_repository_set_head_detached(RepositorySafeHandle repo, ObjectId commitish)
        {
            using (ThreadAffinity())
            {
                GitOid oid = commitish.Oid;
                int res = NativeMethods.git_repository_set_head_detached(repo, ref oid);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_repository_set_head_detached_from_annotated(RepositorySafeHandle repo, GitAnnotatedCommitHandle commit)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_repository_set_head_detached_from_annotated(repo, commit);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_repository_set_head(RepositorySafeHandle repo, string refname)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_repository_set_head(repo, refname);
                Ensure.ZeroResult(res);
            }
        }

        #endregion

        #region git_reset_

        public static void git_reset(
            RepositorySafeHandle repo,
            ObjectId committishId,
            ResetMode resetKind,
            ref GitCheckoutOpts checkoutOptions)
        {
            using (ThreadAffinity())
            using (var osw = new ObjectSafeWrapper(committishId, repo))
            {
                int res = NativeMethods.git_reset(repo, osw.ObjectPtr, resetKind, ref checkoutOptions);
                Ensure.ZeroResult(res);
            }
        }

        #endregion

        #region git_revert_

        public static void git_revert(
            RepositorySafeHandle repo,
            ObjectId commit,
            GitRevertOpts opts)
        {
            using (ThreadAffinity())
            using (var nativeCommit = git_object_lookup(repo, commit, GitObjectType.Commit))
            {
                int res = NativeMethods.git_revert(repo, nativeCommit, opts);
                Ensure.ZeroResult(res);
            }
        }

        #endregion

        #region git_revparse_

        public static Tuple<GitObjectSafeHandle, ReferenceSafeHandle> git_revparse_ext(RepositorySafeHandle repo, string objectish)
        {
            using (ThreadAffinity())
            {
                GitObjectSafeHandle obj;
                ReferenceSafeHandle reference;
                int res = NativeMethods.git_revparse_ext(out obj, out reference, repo, objectish);

                switch (res)
                {
                    case (int)GitErrorCode.NotFound:
                        return null;

                    case (int)GitErrorCode.Ambiguous:
                        throw new AmbiguousSpecificationException(string.Format(CultureInfo.InvariantCulture, "Provided abbreviated ObjectId '{0}' is too short.", objectish));

                    default:
                        Ensure.ZeroResult(res);
                        break;
                }

                return new Tuple<GitObjectSafeHandle, ReferenceSafeHandle>(obj, reference);
            }
        }

        public static GitObjectSafeHandle git_revparse_single(RepositorySafeHandle repo, string objectish)
        {
            var handles = git_revparse_ext(repo, objectish);

            if (handles == null)
            {
                return null;
            }

            handles.Item2.Dispose();

            return handles.Item1;
        }

        #endregion

        #region git_revwalk_

        public static void git_revwalk_free(IntPtr walker)
        {
            NativeMethods.git_revwalk_free(walker);
        }

        public static void git_revwalk_hide(RevWalkerSafeHandle walker, ObjectId commit_id)
        {
            using (ThreadAffinity())
            {
                GitOid oid = commit_id.Oid;
                int res = NativeMethods.git_revwalk_hide(walker, ref oid);
                Ensure.ZeroResult(res);
            }
        }

        public static RevWalkerSafeHandle git_revwalk_new(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                RevWalkerSafeHandle handle;
                int res = NativeMethods.git_revwalk_new(out handle, repo);
                Ensure.ZeroResult(res);

                return handle;
            }
        }

        public static ObjectId git_revwalk_next(RevWalkerSafeHandle walker)
        {
            using (ThreadAffinity())
            {
                GitOid ret;
                int res = NativeMethods.git_revwalk_next(out ret, walker);

                if (res == (int)GitErrorCode.IterOver)
                {
                    return null;
                }

                Ensure.ZeroResult(res);

                return ret;
            }
        }

        public static void git_revwalk_push(RevWalkerSafeHandle walker, ObjectId id)
        {
            using (ThreadAffinity())
            {
                GitOid oid = id.Oid;
                int res = NativeMethods.git_revwalk_push(walker, ref oid);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_revwalk_reset(RevWalkerSafeHandle walker)
        {
            NativeMethods.git_revwalk_reset(walker);
        }

        public static void git_revwalk_sorting(RevWalkerSafeHandle walker, CommitSortStrategies options)
        {
            NativeMethods.git_revwalk_sorting(walker, options);
        }

        public static void git_revwalk_simplify_first_parent(RevWalkerSafeHandle walker)
        {
            NativeMethods.git_revwalk_simplify_first_parent(walker);
        }

        #endregion

        #region git_signature_

        public static void git_signature_free(IntPtr signature)
        {
            NativeMethods.git_signature_free(signature);
        }

        public static SignatureSafeHandle git_signature_new(string name, string email, DateTimeOffset when)
        {
            using (ThreadAffinity())
            {
                SignatureSafeHandle handle;
                int res = NativeMethods.git_signature_new(out handle, name, email, when.ToSecondsSinceEpoch(),
                                                          (int)when.Offset.TotalMinutes);
                Ensure.ZeroResult(res);

                return handle;
            }
        }

        public static IntPtr git_signature_dup(IntPtr sig)
        {
            using (ThreadAffinity())
            {
                IntPtr handle;
                int res = NativeMethods.git_signature_dup(out handle, sig);
                Ensure.ZeroResult(res);
                return handle;
            }
        }

        #endregion

        #region git_stash_

        public static ObjectId git_stash_save(
            RepositorySafeHandle repo,
            Signature stasher,
            string prettifiedMessage,
            StashModifiers options)
        {
            using (ThreadAffinity())
            using (SignatureSafeHandle sigHandle = stasher.BuildHandle())
            {
                GitOid stashOid;

                int res = NativeMethods.git_stash_save(out stashOid, repo, sigHandle, prettifiedMessage, options);

                if (res == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.Int32Result(res);

                return new ObjectId(stashOid);
            }
        }

        public static ICollection<TResult> git_stash_foreach<TResult>(
            RepositorySafeHandle repo,
            Func<int, IntPtr, GitOid, TResult> resultSelector)
        {
            return git_foreach(
                resultSelector,
                c => NativeMethods.git_stash_foreach(
                    repo, (UIntPtr i, IntPtr m, ref GitOid x, IntPtr p) => c((int)i, m, x, p), IntPtr.Zero),
                GitErrorCode.NotFound);
        }

        public static void git_stash_drop(RepositorySafeHandle repo, int index)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_stash_drop(repo, (UIntPtr) index);
                Ensure.BooleanResult(res);
            }
        }

        #endregion

        #region git_status_

        public static FileStatus git_status_file(RepositorySafeHandle repo, FilePath path)
        {
            using (ThreadAffinity())
            {
                FileStatus status;
                int res = NativeMethods.git_status_file(out status, repo, path);

                switch (res)
                {
                    case (int)GitErrorCode.NotFound:
                        return FileStatus.Nonexistent;

                    case (int)GitErrorCode.Ambiguous:
                        throw new AmbiguousSpecificationException(string.Format(CultureInfo.InvariantCulture, "More than one file matches the pathspec '{0}'. You can either force a literal path evaluation (GIT_STATUS_OPT_DISABLE_PATHSPEC_MATCH), or use git_status_foreach().", path));

                    default:
                        Ensure.ZeroResult(res);
                        break;
                }

                return status;
            }
        }

        public static StatusListSafeHandle git_status_list_new(RepositorySafeHandle repo, GitStatusOptions options)
        {
            using (ThreadAffinity())
            {
                StatusListSafeHandle handle;
                int res = NativeMethods.git_status_list_new(out handle, repo, options);
                Ensure.ZeroResult(res);
                return handle;
            }
        }

        public static int git_status_list_entrycount(StatusListSafeHandle list)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_status_list_entrycount(list);
                Ensure.Int32Result(res);
                return res;
            }
        }

        public static StatusEntrySafeHandle git_status_byindex(StatusListSafeHandle list, long idx)
        {
            return NativeMethods.git_status_byindex(list, (UIntPtr)idx);
        }

        public static void git_status_list_free(IntPtr statusList)
        {
            NativeMethods.git_status_list_free(statusList);
        }

        #endregion

        #region git_submodule_

        /// <summary>
        /// Returns a handle to the corresponding submodule,
        /// or an invalid handle if a submodule is not found.
        /// </summary>
        public static SubmoduleSafeHandle git_submodule_lookup(RepositorySafeHandle repo, FilePath name)
        {
            using (ThreadAffinity())
            {
                SubmoduleSafeHandle reference;
                var res = NativeMethods.git_submodule_lookup(out reference, repo, name);

                switch (res)
                {
                    case (int)GitErrorCode.NotFound:
                    case (int)GitErrorCode.Exists:
                    case (int)GitErrorCode.OrphanedHead:
                        return null;

                    default:
                        Ensure.ZeroResult(res);
                        return reference;
                }
            }
        }

        public static string git_submodule_resolve_url(RepositorySafeHandle repo, string url)
        {
            using (ThreadAffinity())
            using (var buf = new GitBuf())
            {
                int res = NativeMethods.git_submodule_resolve_url(buf, repo, url);

                Ensure.ZeroResult(res);
                return LaxUtf8Marshaler.FromNative(buf.ptr);
            }
        }

        public static ICollection<TResult> git_submodule_foreach<TResult>(RepositorySafeHandle repo, Func<IntPtr, IntPtr, TResult> resultSelector)
        {
            return git_foreach(resultSelector, c => NativeMethods.git_submodule_foreach(repo, (x, y, p) => c(x, y, p), IntPtr.Zero));
        }

        public static void git_submodule_add_to_index(SubmoduleSafeHandle submodule, bool write_index)
        {
            using (ThreadAffinity())
            {
                var res = NativeMethods.git_submodule_add_to_index(submodule, write_index);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_submodule_save(SubmoduleSafeHandle submodule)
        {
            using (ThreadAffinity())
            {
                var res = NativeMethods.git_submodule_save(submodule);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_submodule_update(SubmoduleSafeHandle submodule, bool init, ref GitSubmoduleOptions options)
        {
            using (ThreadAffinity())
            {
                var res = NativeMethods.git_submodule_update(submodule, init, ref options);
                Ensure.ZeroResult(res);
            }
        }

        public static void git_submodule_free(IntPtr submodule)
        {
            NativeMethods.git_submodule_free(submodule);
        }

        public static string git_submodule_path(SubmoduleSafeHandle submodule)
        {
            return NativeMethods.git_submodule_path(submodule);
        }

        public static string git_submodule_url(SubmoduleSafeHandle submodule)
        {
            return NativeMethods.git_submodule_url(submodule);
        }

        public static ObjectId git_submodule_index_id(SubmoduleSafeHandle submodule)
        {
            return NativeMethods.git_submodule_index_id(submodule).MarshalAsObjectId();
        }

        public static ObjectId git_submodule_head_id(SubmoduleSafeHandle submodule)
        {
            return NativeMethods.git_submodule_head_id(submodule).MarshalAsObjectId();
        }

        public static ObjectId git_submodule_wd_id(SubmoduleSafeHandle submodule)
        {
            return NativeMethods.git_submodule_wd_id(submodule).MarshalAsObjectId();
        }

        public static SubmoduleIgnore git_submodule_ignore(SubmoduleSafeHandle submodule)
        {
            return NativeMethods.git_submodule_ignore(submodule);
        }

        public static SubmoduleUpdate git_submodule_update_strategy(SubmoduleSafeHandle submodule)
        {
            return NativeMethods.git_submodule_update_strategy(submodule);
        }

        public static SubmoduleRecurse git_submodule_fetch_recurse_submodules(SubmoduleSafeHandle submodule)
        {
            return NativeMethods.git_submodule_fetch_recurse_submodules(submodule);
        }

        public static void git_submodule_reload(SubmoduleSafeHandle submodule)
        {
            using (ThreadAffinity())
            {
                var res = NativeMethods.git_submodule_reload(submodule, false);
                Ensure.ZeroResult(res);
            }
        }

        public static SubmoduleStatus git_submodule_status(SubmoduleSafeHandle submodule)
        {
            using (ThreadAffinity())
            {
                SubmoduleStatus status;
                var res = NativeMethods.git_submodule_status(out status, submodule);
                Ensure.ZeroResult(res);
                return status;
            }
        }

        public static void git_submodule_init(SubmoduleSafeHandle submodule, bool overwrite)
        {
            using (ThreadAffinity())
            {
                var res = NativeMethods.git_submodule_init(submodule, overwrite);
                Ensure.ZeroResult(res);
            }
        }

        #endregion

        #region git_tag_

        public static ObjectId git_tag_annotation_create(
            RepositorySafeHandle repo,
            string name,
            GitObject target,
            Signature tagger,
            string message)
        {
            using (ThreadAffinity())
            using (var objectPtr = new ObjectSafeWrapper(target.Id, repo))
            using (SignatureSafeHandle sigHandle = tagger.BuildHandle())
            {
                GitOid oid;
                int res = NativeMethods.git_tag_annotation_create(out oid, repo, name, objectPtr.ObjectPtr, sigHandle, message);
                Ensure.ZeroResult(res);

                return oid;
            }
        }

        public static ObjectId git_tag_create(
            RepositorySafeHandle repo,
            string name,
            GitObject target,
            Signature tagger,
            string message,
            bool allowOverwrite)
        {
            using (ThreadAffinity())
            using (var objectPtr = new ObjectSafeWrapper(target.Id, repo))
            using (SignatureSafeHandle sigHandle = tagger.BuildHandle())
            {
                GitOid oid;
                int res = NativeMethods.git_tag_create(out oid, repo, name, objectPtr.ObjectPtr, sigHandle, message, allowOverwrite);
                Ensure.ZeroResult(res);

                return oid;
            }
        }

        public static ObjectId git_tag_create_lightweight(RepositorySafeHandle repo, string name, GitObject target, bool allowOverwrite)
        {
            using (ThreadAffinity())
            using (var objectPtr = new ObjectSafeWrapper(target.Id, repo))
            {
                GitOid oid;
                int res = NativeMethods.git_tag_create_lightweight(out oid, repo, name, objectPtr.ObjectPtr, allowOverwrite);
                Ensure.ZeroResult(res);

                return oid;
            }
        }

        public static void git_tag_delete(RepositorySafeHandle repo, string name)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_tag_delete(repo, name);
                Ensure.ZeroResult(res);
            }
        }

        public static IList<string> git_tag_list(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                var array = new GitStrArrayNative();

                try
                {
                    int res = NativeMethods.git_tag_list(out array.Array, repo);
                    Ensure.ZeroResult(res);

                    return array.ReadStrings();
                }
                finally
                {
                    array.Dispose();
                }
            }
        }

        public static string git_tag_message(GitObjectSafeHandle tag)
        {
            return NativeMethods.git_tag_message(tag);
        }

        public static string git_tag_name(GitObjectSafeHandle tag)
        {
            return NativeMethods.git_tag_name(tag);
        }

        public static Signature git_tag_tagger(GitObjectSafeHandle tag)
        {
            IntPtr taggerHandle = NativeMethods.git_tag_tagger(tag);

            // Not all tags have a tagger signature - we need to handle
            // this case.
            Signature tagger = null;
            if (taggerHandle != IntPtr.Zero)
            {
                tagger = new Signature(taggerHandle);
            }

            return tagger;
        }

        public static ObjectId git_tag_target_id(GitObjectSafeHandle tag)
        {
            return NativeMethods.git_tag_target_id(tag).MarshalAsObjectId();
        }

        public static GitObjectType git_tag_target_type(GitObjectSafeHandle tag)
        {
            return NativeMethods.git_tag_target_type(tag);
        }

        #endregion

        #region git_trace_

        /// <summary>
        /// Install/Enable logging inside of LibGit2 to send messages back to LibGit2Sharp.
        ///
        /// Since the given callback will be passed into and retained by C code,
        /// it is very important that you pass an actual delegate here (and don't
        /// let the compiler create/cast a temporary one for you).  Furthermore, you
        /// must hold a reference to this delegate until you turn off logging.
        ///
        /// This callback is unlike other callbacks because logging persists in the
        /// process until disabled; in contrast, most callbacks are only defined for
        /// the duration of the down-call.
        /// </summary>
        public static void git_trace_set(LogLevel level, NativeMethods.git_trace_cb callback)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_trace_set(level, callback);
                Ensure.ZeroResult(res);
            }
        }

        #endregion

        #region git_transport_

        public static void git_transport_register(String prefix, IntPtr transport_cb, IntPtr param)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_transport_register(prefix, transport_cb, param);

                if (res == (int)GitErrorCode.Exists)
                {
                    throw new EntryExistsException(String.Format("A custom transport for '{0}' is already registered", prefix));
                }

                Ensure.ZeroResult(res);
            }
        }

        public static void git_transport_unregister(String prefix)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_transport_unregister(prefix);

                if (res == (int)GitErrorCode.NotFound)
                {
                    throw new NotFoundException("The given transport was not found");
                }

                Ensure.ZeroResult(res);
            }
        }

        #endregion

        #region git_tree_

        public static Mode git_tree_entry_attributes(SafeHandle entry)
        {
            return (Mode)NativeMethods.git_tree_entry_filemode(entry);
        }

        public static TreeEntrySafeHandle git_tree_entry_byindex(GitObjectSafeHandle tree, long idx)
        {
            return NativeMethods.git_tree_entry_byindex(tree, (UIntPtr)idx);
        }

        public static TreeEntrySafeHandle_Owned git_tree_entry_bypath(RepositorySafeHandle repo, ObjectId id, FilePath treeentry_path)
        {
            using (ThreadAffinity())
            using (var obj = new ObjectSafeWrapper(id, repo))
            {
                TreeEntrySafeHandle_Owned treeEntryPtr;
                int res = NativeMethods.git_tree_entry_bypath(out treeEntryPtr, obj.ObjectPtr, treeentry_path);

                if (res == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.ZeroResult(res);

                return treeEntryPtr;
            }
        }

        public static void git_tree_entry_free(IntPtr treeEntry)
        {
            NativeMethods.git_tree_entry_free(treeEntry);
        }

        public static ObjectId git_tree_entry_id(SafeHandle entry)
        {
            return NativeMethods.git_tree_entry_id(entry).MarshalAsObjectId();
        }

        public static string git_tree_entry_name(SafeHandle entry)
        {
            return NativeMethods.git_tree_entry_name(entry);
        }

        public static GitObjectType git_tree_entry_type(SafeHandle entry)
        {
            return NativeMethods.git_tree_entry_type(entry);
        }

        public static int git_tree_entrycount(GitObjectSafeHandle tree)
        {
            return (int)NativeMethods.git_tree_entrycount(tree);
        }

        #endregion

        #region git_treebuilder_

        public static TreeBuilderSafeHandle git_treebuilder_new(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                TreeBuilderSafeHandle builder;
                int res = NativeMethods.git_treebuilder_new(out builder, repo, IntPtr.Zero);
                Ensure.ZeroResult(res);

                return builder;
            }
        }

        public static void git_treebuilder_free(IntPtr bld)
        {
            NativeMethods.git_treebuilder_free(bld);
        }

        public static void git_treebuilder_insert(TreeBuilderSafeHandle builder, string treeentry_name, TreeEntryDefinition treeEntryDefinition)
        {
            using (ThreadAffinity())
            {
                GitOid oid = treeEntryDefinition.TargetId.Oid;
                int res = NativeMethods.git_treebuilder_insert(IntPtr.Zero, builder, treeentry_name, ref oid, (uint)treeEntryDefinition.Mode);
                Ensure.ZeroResult(res);
            }
        }

        public static ObjectId git_treebuilder_write(TreeBuilderSafeHandle bld)
        {
            using (ThreadAffinity())
            {
                GitOid oid;
                int res = NativeMethods.git_treebuilder_write(out oid, bld);
                Ensure.ZeroResult(res);

                return oid;
            }
        }

        #endregion

        #region git_libgit2_

        /// <summary>
        /// Returns the features with which libgit2 was compiled.
        /// </summary>
        public static BuiltInFeatures git_libgit2_features()
        {
            return (BuiltInFeatures)NativeMethods.git_libgit2_features();
        }

        #endregion

        private static ICollection<TResult> git_foreach<T, TResult>(
            Func<T, TResult> resultSelector,
            Func<Func<T, IntPtr, int>, int> iterator,
            params GitErrorCode[] ignoredErrorCodes)
        {
            using (ThreadAffinity())
            {
                var result = new List<TResult>();
                var res = iterator((x, payload) =>
                                       {
                                           result.Add(resultSelector(x));
                                           return 0;
                                       });

                if (ignoredErrorCodes != null && ignoredErrorCodes.Contains((GitErrorCode)res))
                {
                    return new TResult[0];
                }

                Ensure.ZeroResult(res);
                return result;
            }
        }

        private static ICollection<TResult> git_foreach<T1, T2, TResult>(
            Func<T1, T2, TResult> resultSelector,
            Func<Func<T1, T2, IntPtr, int>, int> iterator,
            params GitErrorCode[] ignoredErrorCodes)
        {
            using (ThreadAffinity())
            {
                var result = new List<TResult>();
                var res = iterator((x, y, payload) =>
                                       {
                                           result.Add(resultSelector(x, y));
                                           return 0;
                                       });

                if (ignoredErrorCodes != null && ignoredErrorCodes.Contains((GitErrorCode)res))
                {
                    return new TResult[0];
                }

                Ensure.ZeroResult(res);
                return result;
            }
        }

        private static ICollection<TResult> git_foreach<T1, T2, T3, TResult>(
            Func<T1, T2, T3, TResult> resultSelector,
            Func<Func<T1, T2, T3, IntPtr, int>, int> iterator,
            params GitErrorCode[] ignoredErrorCodes)
        {
            using (ThreadAffinity())
            {
                var result = new List<TResult>();
                var res = iterator((w, x, y, payload) =>
                {
                    result.Add(resultSelector(w, x, y));
                    return 0;
                });

                if (ignoredErrorCodes != null && ignoredErrorCodes.Contains((GitErrorCode)res))
                {
                    return new TResult[0];
                }

                Ensure.ZeroResult(res);
                return result;
            }
        }

        public delegate TResult Func<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

        private static ICollection<TResult> git_foreach<T1, T2, T3, T4, TResult>(
            Func<T1, T2, T3, T4, TResult> resultSelector,
            Func<Func<T1, T2, T3, T4, IntPtr, int>, int> iterator,
            params GitErrorCode[] ignoredErrorCodes)
        {
            using (ThreadAffinity())
            {
                var result = new List<TResult>();
                var res = iterator((w, x, y, z, payload) =>
                {
                    result.Add(resultSelector(w, x, y, z));
                    return 0;
                });

                if (ignoredErrorCodes != null && ignoredErrorCodes.Contains((GitErrorCode)res))
                {
                    return new TResult[0];
                }

                Ensure.ZeroResult(res);
                return result;
            }
        }

        private delegate int IteratorNew<THandle>(out THandle iter);

        private delegate TPayload IteratorNext<in TIterator, THandle, out TPayload>(TIterator iter, out THandle next, out int res);

        private static THandle git_iterator_new<THandle>(IteratorNew<THandle> newFunc)
            where THandle : SafeHandleBase
        {
            THandle iter;
            Ensure.ZeroResult(newFunc(out iter));
            return iter;
        }

        private static IEnumerable<TResult> git_iterator_next<TIterator, THandle, TPayload, TResult>(
            TIterator iter,
            IteratorNext<TIterator, THandle, TPayload> nextFunc,
            Func<THandle, TPayload, TResult> resultSelector)
            where THandle : SafeHandleBase
        {
            while (true)
            {
                var next = default(THandle);
                try
                {
                    int res;
                    var payload = nextFunc(iter, out next, out res);

                    if (res == (int)GitErrorCode.IterOver)
                    {
                        yield break;
                    }

                    Ensure.ZeroResult(res);
                    yield return resultSelector(next, payload);
                }
                finally
                {
                    next.SafeDispose();
                }
            }
        }

        private static IEnumerable<TResult> git_iterator<TIterator, THandle, TPayload, TResult>(
            IteratorNew<TIterator> newFunc,
            IteratorNext<TIterator, THandle, TPayload> nextFunc,
            Func<THandle, TPayload, TResult> resultSelector
            )
            where TIterator : SafeHandleBase
            where THandle : SafeHandleBase
        {
            using (ThreadAffinity())
            {
                using (var iter = git_iterator_new(newFunc))
                {
                    foreach (var next in git_iterator_next(iter, nextFunc, resultSelector))
                    {
                        yield return next;
                    }
                }
            }
        }

        private static bool RepositoryStateChecker(RepositorySafeHandle repo, Func<RepositorySafeHandle, int> checker)
        {
            using (ThreadAffinity())
            {
                int res = checker(repo);
                Ensure.BooleanResult(res);

                return (res == 1);
            }
        }

        private static FilePath ConvertPath(Func<GitBuf, int> pathRetriever)
        {
            using (ThreadAffinity())
            using (var buf = new GitBuf())
            {
                int result = pathRetriever(buf);

                if (result == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.ZeroResult(result);
                return LaxFilePathMarshaler.FromNative(buf.ptr);
            }
        }

        private static Func<IDisposable> ThreadAffinity = WithoutThreadAffinity;

        internal static void EnableThreadAffinity()
        {
            ThreadAffinity = WithThreadAffinity;
        }

        private static IDisposable WithoutThreadAffinity()
        {
            return null;
        }

        private static IDisposable WithThreadAffinity()
        {
            return new DisposableThreadAffinityWrapper();
        }

        private class DisposableThreadAffinityWrapper : IDisposable
        {
            public DisposableThreadAffinityWrapper()
            {
                Thread.BeginThreadAffinity();
            }

            public void Dispose()
            {
                Thread.EndThreadAffinity();
            }
        }

        private static readonly IDictionary<Type, Func<string, object>> configurationParser = new Dictionary<Type, Func<string, object>>
        {
            { typeof(int), value => git_config_parse_int32(value) },
            { typeof(long), value => git_config_parse_int64(value) },
            { typeof(bool), value => git_config_parse_bool(value) },
            { typeof(string), value => value },
        };

        /// <summary>
        /// Helper method for consistent conversion of return value on
        /// Callbacks that support cancellation from bool to native type.
        /// True indicates that function should continue, false indicates
        /// user wants to cancel.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        internal static int ConvertResultToCancelFlag(bool result)
        {
            return result ? 0 : (int)GitErrorCode.User;
        }
    }
}
// ReSharper restore InconsistentNaming
