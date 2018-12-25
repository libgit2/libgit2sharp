using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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
                NativeMethods.giterr_set_str(error_class, ErrorMessageFromException(exception));
            }
        }

        public static void giterr_set_str(GitErrorCategory error_class, String errorString)
        {
            NativeMethods.giterr_set_str(error_class, errorString);
        }

        /// <summary>
        /// This method will take an exception and try to generate an error message
        /// that captures the important messages of the error.
        /// The formatting is a bit subjective.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static string ErrorMessageFromException(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            BuildErrorMessageFromException(sb, 0, ex);
            return sb.ToString();
        }

        private static void BuildErrorMessageFromException(StringBuilder sb, int level, Exception ex)
        {
            string indent = new string(' ', level * 4);
            sb.AppendFormat("{0}{1}", indent, ex.Message);

            if (ex is AggregateException)
            {
                AggregateException aggregateException = ((AggregateException)ex).Flatten();

                if (aggregateException.InnerExceptions.Count == 1)
                {
                    sb.AppendLine();
                    sb.AppendLine();

                    sb.AppendFormat("{0}Contained Exception:{1}", indent, Environment.NewLine);
                    BuildErrorMessageFromException(sb, level + 1, aggregateException.InnerException);
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine();

                    sb.AppendFormat("{0}Contained Exceptions:{1}", indent, Environment.NewLine);
                    for (int i = 0; i < aggregateException.InnerExceptions.Count; i++)
                    {
                        if (i != 0)
                        {
                            sb.AppendLine();
                            sb.AppendLine();
                        }

                        BuildErrorMessageFromException(sb, level + 1, aggregateException.InnerExceptions[i]);
                    }
                }
            }
            else if (ex.InnerException != null)
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendFormat("{0}Inner Exception:{1}", indent, Environment.NewLine);
                BuildErrorMessageFromException(sb, level + 1, ex.InnerException);
            }
        }

        #endregion

        #region git_blame_

        public static unsafe BlameHandle git_blame_file(
            RepositoryHandle repo,
            string path,
            git_blame_options options)
        {
            git_blame* ptr;
            int res = NativeMethods.git_blame_file(out ptr, repo, path, options);
            Ensure.ZeroResult(res);
            return new BlameHandle(ptr, true);
        }

        public static unsafe git_blame_hunk* git_blame_get_hunk_byindex(BlameHandle blame, uint idx)
        {
            return NativeMethods.git_blame_get_hunk_byindex(blame, idx);
        }

        #endregion

        #region git_blob_

        public static unsafe IntPtr git_blob_create_fromstream(RepositoryHandle repo, string hintpath)
        {
            IntPtr writestream_ptr;

            Ensure.ZeroResult(NativeMethods.git_blob_create_fromstream(out writestream_ptr, repo, hintpath));
            return writestream_ptr;
        }

        public static unsafe ObjectId git_blob_create_fromstream_commit(IntPtr writestream_ptr)
        {
            var oid = new GitOid();
            Ensure.ZeroResult(NativeMethods.git_blob_create_fromstream_commit(ref oid, writestream_ptr));
            return oid;
        }

        public static unsafe ObjectId git_blob_create_fromdisk(RepositoryHandle repo, FilePath path)
        {
            var oid = new GitOid();
            int res = NativeMethods.git_blob_create_fromdisk(ref oid, repo, path);
            Ensure.ZeroResult(res);

            return oid;
        }

        public static unsafe ObjectId git_blob_create_fromfile(RepositoryHandle repo, FilePath path)
        {
            var oid = new GitOid();
            int res = NativeMethods.git_blob_create_fromworkdir(ref oid, repo, path);
            Ensure.ZeroResult(res);

            return oid;
        }

        public static unsafe UnmanagedMemoryStream git_blob_filtered_content_stream(RepositoryHandle repo, ObjectId id, string path, bool check_for_binary_data)
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

        public static unsafe UnmanagedMemoryStream git_blob_rawcontent_stream(RepositoryHandle repo, ObjectId id, Int64 size)
        {
            var handle = new ObjectSafeWrapper(id, repo).ObjectPtr;
            return new RawContentStream(handle, h => NativeMethods.git_blob_rawcontent(h), h => size);
        }

        public static unsafe long git_blob_rawsize(ObjectHandle obj)
        {
            return NativeMethods.git_blob_rawsize(obj);
        }

        public static unsafe bool git_blob_is_binary(ObjectHandle obj)
        {
            int res = NativeMethods.git_blob_is_binary(obj);
            Ensure.BooleanResult(res);

            return (res == 1);
        }

        #endregion

        #region git_branch_

        public static unsafe ReferenceHandle git_branch_create_from_annotated(RepositoryHandle repo, string branch_name, string targetIdentifier, bool force)
        {
            git_reference* reference;

            using (var annotatedCommit = git_annotated_commit_from_revspec(repo, targetIdentifier))
            {
                int res = NativeMethods.git_branch_create_from_annotated(out reference, repo, branch_name, annotatedCommit, force);
                Ensure.ZeroResult(res);
            }

            return new ReferenceHandle(reference, true);
        }

        public static unsafe void git_branch_delete(ReferenceHandle reference)
        {
            int res = NativeMethods.git_branch_delete(reference);
            Ensure.ZeroResult(res);
        }

        public static IEnumerable<Branch> git_branch_iterator(Repository repo, GitBranchType branchType)
        {
            IntPtr iter;
            var res = NativeMethods.git_branch_iterator_new(out iter, repo.Handle.AsIntPtr(), branchType);
            Ensure.ZeroResult(res);

            try
            {
                while (true)
                {
                    IntPtr refPtr = IntPtr.Zero;
                    GitBranchType _branchType;
                    res = NativeMethods.git_branch_next(out refPtr, out _branchType, iter);
                    if (res == (int)GitErrorCode.IterOver)
                    {
                        yield break;
                    }
                    Ensure.ZeroResult(res);

                    Reference reference;
                    using (var refHandle = new ReferenceHandle(refPtr, true))
                    {
                        reference = Reference.BuildFromPtr<Reference>(refHandle, repo);
                    }
                    yield return new Branch(repo, reference, reference.CanonicalName);
                }
            }
            finally
            {
                NativeMethods.git_branch_iterator_free(iter);
            }
        }

        public static void git_branch_iterator_free(IntPtr iter)
        {
            NativeMethods.git_branch_iterator_free(iter);
        }

        public static unsafe ReferenceHandle git_branch_move(ReferenceHandle reference, string new_branch_name, bool force)
        {
            git_reference* ref_out;
            int res = NativeMethods.git_branch_move(out ref_out, reference, new_branch_name, force);
            Ensure.ZeroResult(res);
            return new ReferenceHandle(ref_out, true);
        }

        public static unsafe string git_branch_remote_name(RepositoryHandle repo, string canonical_branch_name, bool shouldThrowIfNotFound)
        {
            using (var buf = new GitBuf())
            {
                int res = NativeMethods.git_branch_remote_name(buf, repo, canonical_branch_name);

                if (!shouldThrowIfNotFound &&
                    (res == (int)GitErrorCode.NotFound || res == (int)GitErrorCode.Ambiguous))
                {
                    return null;
                }

                Ensure.ZeroResult(res);
                return LaxUtf8Marshaler.FromNative(buf.ptr);
            }
        }

        public static unsafe string git_branch_upstream_name(RepositoryHandle handle, string canonicalReferenceName)
        {
            using (var buf = new GitBuf())
            {
                int res = NativeMethods.git_branch_upstream_name(buf, handle, canonicalReferenceName);
                if (res == (int)GitErrorCode.NotFound)
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

        public static unsafe void git_checkout_tree(
            RepositoryHandle repo,
            ObjectId treeId,
            ref GitCheckoutOpts opts)
        {
            using (var osw = new ObjectSafeWrapper(treeId, repo))
            {
                int res = NativeMethods.git_checkout_tree(repo, osw.ObjectPtr, ref opts);
                Ensure.ZeroResult(res);
            }
        }

        public static unsafe void git_checkout_index(RepositoryHandle repo, ObjectHandle treeish, ref GitCheckoutOpts opts)
        {
            int res = NativeMethods.git_checkout_index(repo, treeish, ref opts);
            Ensure.ZeroResult(res);
        }

        #endregion

        #region git_cherry_pick_

        internal static unsafe void git_cherrypick(RepositoryHandle repo, ObjectId commit, GitCherryPickOptions options)
        {
            using (var nativeCommit = git_object_lookup(repo, commit, GitObjectType.Commit))
            {
                int res = NativeMethods.git_cherrypick(repo, nativeCommit, options);
                Ensure.ZeroResult(res);
            }
        }

        internal static unsafe IndexHandle git_cherrypick_commit(RepositoryHandle repo, ObjectHandle cherrypickCommit, ObjectHandle ourCommit, uint mainline, GitMergeOpts opts, out bool earlyStop)
        {
            git_index* index;
            int res = NativeMethods.git_cherrypick_commit(out index, repo, cherrypickCommit, ourCommit, mainline, ref opts);
            if (res == (int)GitErrorCode.MergeConflict)
            {
                earlyStop = true;
            }
            else
            {
                earlyStop = false;
                Ensure.ZeroResult(res);
            }
            return new IndexHandle(index, true);
        }
        #endregion

        #region git_clone_

        public static unsafe RepositoryHandle git_clone(
            string url,
            string workdir,
            ref GitCloneOptions opts)
        {
            git_repository *repo;
            int res = NativeMethods.git_clone(out repo, url, workdir, ref opts);
            Ensure.ZeroResult(res);
            return new RepositoryHandle(repo, true);
        }

        #endregion

        #region git_commit_

        public static unsafe Signature git_commit_author(ObjectHandle obj)
        {
            return new Signature(NativeMethods.git_commit_author(obj));
        }

        public static unsafe Signature git_commit_committer(ObjectHandle obj)
        {
            return new Signature(NativeMethods.git_commit_committer(obj));
        }

        public static unsafe ObjectId git_commit_create(
            RepositoryHandle repo,
            string referenceName,
            Signature author,
            Signature committer,
            string message,
            Tree tree,
            GitOid[] parentIds)
        {
            using (SignatureHandle authorHandle = author.BuildHandle())
            using (SignatureHandle committerHandle = committer.BuildHandle())
            using (var parentPtrs = new ArrayMarshaler<GitOid>(parentIds))
            {
                GitOid commitOid;

                var treeOid = tree.Id.Oid;

                int res = NativeMethods.git_commit_create_from_ids(out commitOid,
                                                                   repo,
                                                                   referenceName,
                                                                   authorHandle,
                                                                   committerHandle,
                                                                   null,
                                                                   message,
                                                                   ref treeOid,
                                                                   (UIntPtr)parentPtrs.Count,
                                                                   parentPtrs.ToArray());

                Ensure.ZeroResult(res);

                return commitOid;
            }
        }

        public static unsafe string git_commit_create_buffer(
            RepositoryHandle repo,
            Signature author,
            Signature committer,
            string message,
            Tree tree,
            Commit[] parents)
        {
            using (SignatureHandle authorHandle = author.BuildHandle())
            using (SignatureHandle committerHandle = committer.BuildHandle())
            using (var treeHandle = Proxy.git_object_lookup(tree.repo.Handle, tree.Id, GitObjectType.Tree))
            using (var buf = new GitBuf())
            {
                ObjectHandle[] handles = new ObjectHandle[0];
                try
                {
                    handles = parents.Select(c => Proxy.git_object_lookup(c.repo.Handle, c.Id, GitObjectType.Commit)).ToArray();
                    var ptrs = handles.Select(p => p.AsIntPtr()).ToArray();
                    int res;
                    fixed(IntPtr* objs = ptrs)
                    {
                        res = NativeMethods.git_commit_create_buffer(buf,
                            repo,
                            authorHandle,
                            committerHandle,
                            null,
                            message,
                            treeHandle,
                            new UIntPtr((ulong)parents.LongCount()),
                            objs);
                    }
                    Ensure.ZeroResult(res);
                }
                finally
                {
                    foreach (var handle in handles)
                    {
                        handle.Dispose();
                    }
                }

                return LaxUtf8Marshaler.FromNative(buf.ptr);
            }
        }

        public static unsafe ObjectId git_commit_create_with_signature(RepositoryHandle repo, string commitContent,
            string signature, string field)
        {
            GitOid id;
            int res = NativeMethods.git_commit_create_with_signature(out id, repo, commitContent, signature, field);
            Ensure.ZeroResult(res);

            return id;
        }

        public static unsafe string git_commit_message(ObjectHandle obj)
        {
            return NativeMethods.git_commit_message(obj);
        }

        public static unsafe string git_commit_summary(ObjectHandle obj)
        {
            return NativeMethods.git_commit_summary(obj);
        }

        public static unsafe string git_commit_message_encoding(ObjectHandle obj)
        {
            return NativeMethods.git_commit_message_encoding(obj);
        }

        public static unsafe ObjectId git_commit_parent_id(ObjectHandle obj, uint i)
        {
            return ObjectId.BuildFromPtr(NativeMethods.git_commit_parent_id(obj, i));
        }

        public static int git_commit_parentcount(RepositoryHandle repo, ObjectId id)
        {
            using (var obj = new ObjectSafeWrapper(id, repo))
            {
                return git_commit_parentcount(obj);
            }
        }

        public static unsafe int git_commit_parentcount(ObjectSafeWrapper obj)
        {
            return (int)NativeMethods.git_commit_parentcount(obj.ObjectPtr);
        }

        public static unsafe ObjectId git_commit_tree_id(ObjectHandle obj)
        {
            return ObjectId.BuildFromPtr(NativeMethods.git_commit_tree_id(obj));
        }

        public static unsafe SignatureInfo git_commit_extract_signature(RepositoryHandle repo, ObjectId id, string field)
        {
            using (var signature = new GitBuf())
            using (var signedData = new GitBuf())
            {
                var oid = id.Oid;
                Ensure.ZeroResult(NativeMethods.git_commit_extract_signature(signature, signedData, repo, ref oid, field));

                return new SignatureInfo()
                {
                    Signature = LaxUtf8Marshaler.FromNative(signature.ptr, signature.size.ConvertToInt()),
                    SignedData = LaxUtf8Marshaler.FromNative(signedData.ptr, signedData.size.ConvertToInt()),
                };
            }
        }

        #endregion

        #region git_config_

        public static unsafe void git_config_add_file_ondisk(ConfigurationHandle config, FilePath path, ConfigurationLevel level, RepositoryHandle repo)
        {
            // RepositoryHandle does implicit cast voodoo that is not null-safe, thus this explicit check
            git_repository* repoHandle = (repo != null) ? (git_repository*)repo : null;
            int res = NativeMethods.git_config_add_file_ondisk(config, path, (uint)level, repoHandle, true);
            Ensure.ZeroResult(res);
        }

        public static unsafe bool git_config_delete(ConfigurationHandle config, string name)
        {
            int res = NativeMethods.git_config_delete_entry(config, name);

            if (res == (int)GitErrorCode.NotFound)
            {
                return false;
            }

            Ensure.ZeroResult(res);
            return true;
        }

        const string anyValue = ".*";

        public static unsafe bool git_config_delete_multivar(ConfigurationHandle config, string name)
        {
            int res = NativeMethods.git_config_delete_multivar(config, name, anyValue);

            if (res == (int)GitErrorCode.NotFound)
            {
                return false;
            }

            Ensure.ZeroResult(res);
            return true;
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

        public static FilePath git_config_find_programdata()
        {
            return ConvertPath(NativeMethods.git_config_find_programdata);
        }

        public static unsafe void git_config_free(git_config *config)
        {
            NativeMethods.git_config_free(config);
        }

        public static unsafe ConfigurationEntry<T> git_config_get_entry<T>(ConfigurationHandle config, string key)
        {
            if (!configurationParser.ContainsKey(typeof(T)))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Generic Argument of type '{0}' is not supported.", typeof(T).FullName));
            }

            GitConfigEntry* entry = null;
            try
            {
                var res = NativeMethods.git_config_get_entry(out entry, config, key);
                if (res == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.ZeroResult(res);
                return new ConfigurationEntry<T>(LaxUtf8Marshaler.FromNative(entry->namePtr),
                    (T)configurationParser[typeof(T)](LaxUtf8Marshaler.FromNative(entry->valuePtr)),
                    (ConfigurationLevel)entry->level);
            }
            finally
            {
                NativeMethods.git_config_entry_free(entry);
            }
        }

        public static unsafe ConfigurationHandle git_config_new()
        {
            git_config* handle;
            int res = NativeMethods.git_config_new(out handle);
            Ensure.ZeroResult(res);

            return new ConfigurationHandle(handle, true);
        }

        public static unsafe ConfigurationHandle git_config_open_level(ConfigurationHandle parent, ConfigurationLevel level)
        {
            git_config* handle;
            int res = NativeMethods.git_config_open_level(out handle, parent, (uint)level);

            if (res == (int)GitErrorCode.NotFound)
            {
                return null;
            }

            Ensure.ZeroResult(res);

            return new ConfigurationHandle(handle, true);
        }

        public static bool git_config_parse_bool(string value)
        {
            bool outVal;
            var res = NativeMethods.git_config_parse_bool(out outVal, value);

            Ensure.ZeroResult(res);
            return outVal;
        }

        public static int git_config_parse_int32(string value)
        {
            int outVal;
            var res = NativeMethods.git_config_parse_int32(out outVal, value);

            Ensure.ZeroResult(res);
            return outVal;
        }

        public static long git_config_parse_int64(string value)
        {
            long outVal;
            var res = NativeMethods.git_config_parse_int64(out outVal, value);

            Ensure.ZeroResult(res);
            return outVal;
        }

        public static unsafe void git_config_set_bool(ConfigurationHandle config, string name, bool value)
        {
            int res = NativeMethods.git_config_set_bool(config, name, value);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_config_set_int32(ConfigurationHandle config, string name, int value)
        {
            int res = NativeMethods.git_config_set_int32(config, name, value);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_config_set_int64(ConfigurationHandle config, string name, long value)
        {
            int res = NativeMethods.git_config_set_int64(config, name, value);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_config_set_string(ConfigurationHandle config, string name, string value)
        {
            int res = NativeMethods.git_config_set_string(config, name, value);
            Ensure.ZeroResult(res);
        }

        public static unsafe ICollection<TResult> git_config_foreach<TResult>(
            ConfigurationHandle config,
            Func<IntPtr, TResult> resultSelector)
        {
            return git_foreach(resultSelector, c => NativeMethods.git_config_foreach(config, (e, p) => c(e, p), IntPtr.Zero));
        }

        public static IEnumerable<ConfigurationEntry<string>> git_config_iterator_glob(
            ConfigurationHandle config,
            string regexp)
        {
            IntPtr iter;
            var res = NativeMethods.git_config_iterator_glob_new(out iter, config.AsIntPtr(), regexp);
            Ensure.ZeroResult(res);
            try
            {
                while (true)
                {
                    IntPtr entry;
                    res = NativeMethods.git_config_next(out entry, iter);
                    if (res == (int)GitErrorCode.IterOver)
                    {
                        yield break;
                    }
                    Ensure.ZeroResult(res);

                    yield return Configuration.BuildConfigEntry(entry);
                }
            }
            finally
            {
                NativeMethods.git_config_iterator_free(iter);
            }
        }

        public static unsafe ConfigurationHandle git_config_snapshot(ConfigurationHandle config)
        {
            git_config* handle;
            int res = NativeMethods.git_config_snapshot(out handle, config);
            Ensure.ZeroResult(res);

            return new ConfigurationHandle(handle, true);
        }

        public static unsafe IntPtr git_config_lock(git_config* config)
        {
            IntPtr txn;
            int res = NativeMethods.git_config_lock(out txn, config);
            Ensure.ZeroResult(res);

            return txn;
        }

        #endregion

        #region git_cred_

        public static void git_cred_free(IntPtr cred)
        {
            NativeMethods.git_cred_free(cred);
        }

        #endregion

        #region git_describe_

        public static unsafe string git_describe_commit(
            RepositoryHandle repo,
            ObjectId committishId,
            DescribeOptions options)
        {
            Ensure.ArgumentPositiveInt32(options.MinimumCommitIdAbbreviatedSize, "options.MinimumCommitIdAbbreviatedSize");

            using (var osw = new ObjectSafeWrapper(committishId, repo))
            {
                GitDescribeOptions opts = new GitDescribeOptions
                {
                    Version = 1,
                    DescribeStrategy = options.Strategy,
                    MaxCandidatesTags = 10,
                    OnlyFollowFirstParent = options.OnlyFollowFirstParent,
                    ShowCommitOidAsFallback = options.UseCommitIdAsFallback,
                };

                DescribeResultHandle describeHandle = null;

                try
                {
                    git_describe_result* result;
                    int res = NativeMethods.git_describe_commit(out result, osw.ObjectPtr, ref opts);
                    Ensure.ZeroResult(res);
                    describeHandle = new DescribeResultHandle(result, true);

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

        #endregion

        #region git_diff_

        public static unsafe void git_diff_blobs(
            RepositoryHandle repo,
            ObjectId oldBlob,
            ObjectId newBlob,
            GitDiffOptions options,
            NativeMethods.git_diff_file_cb fileCallback,
            NativeMethods.git_diff_hunk_cb hunkCallback,
            NativeMethods.git_diff_line_cb lineCallback)
        {
            using (var osw1 = new ObjectSafeWrapper(oldBlob, repo, true))
            using (var osw2 = new ObjectSafeWrapper(newBlob, repo, true))
            {
                int res = NativeMethods.git_diff_blobs(osw1.ObjectPtr,
                                                       null,
                                                       osw2.ObjectPtr,
                                                       null,
                                                       options,
                                                       fileCallback,
                                                       null,
                                                       hunkCallback,
                                                       lineCallback,
                                                       IntPtr.Zero);

                Ensure.ZeroResult(res);
            }
        }

        public static unsafe void git_diff_foreach(
            git_diff* diff,
            NativeMethods.git_diff_file_cb fileCallback,
            NativeMethods.git_diff_hunk_cb hunkCallback,
            NativeMethods.git_diff_line_cb lineCallback)
        {
            int res = NativeMethods.git_diff_foreach(diff, fileCallback, null, hunkCallback, lineCallback, IntPtr.Zero);
            Ensure.ZeroResult(res);
        }

        public static unsafe DiffHandle git_diff_tree_to_index(
            RepositoryHandle repo,
            IndexHandle index,
            ObjectId oldTree,
            GitDiffOptions options)
        {
            using (var osw = new ObjectSafeWrapper(oldTree, repo, true))
            {
                git_diff* diff;
                int res = NativeMethods.git_diff_tree_to_index(out diff, repo, osw.ObjectPtr, index, options);
                Ensure.ZeroResult(res);

                return new DiffHandle(diff, true);
            }
        }

        public static unsafe void git_diff_merge(DiffHandle onto, DiffHandle from)
        {
            int res = NativeMethods.git_diff_merge(onto, from);
            Ensure.ZeroResult(res);
        }

        public static unsafe DiffHandle git_diff_tree_to_tree(
            RepositoryHandle repo,
            ObjectId oldTree,
            ObjectId newTree,
            GitDiffOptions options)
        {
            using (var osw1 = new ObjectSafeWrapper(oldTree, repo, true))
            using (var osw2 = new ObjectSafeWrapper(newTree, repo, true))
            {
                git_diff* diff;
                int res = NativeMethods.git_diff_tree_to_tree(out diff, repo, osw1.ObjectPtr, osw2.ObjectPtr, options);
                Ensure.ZeroResult(res);

                return new DiffHandle(diff, true);
            }
        }

        public static unsafe DiffHandle git_diff_index_to_workdir(
            RepositoryHandle repo,
            IndexHandle index,
            GitDiffOptions options)
        {
            git_diff* diff;
            int res = NativeMethods.git_diff_index_to_workdir(out diff, repo, index, options);
            Ensure.ZeroResult(res);

            return new DiffHandle(diff, true);
        }

        public static unsafe DiffHandle git_diff_tree_to_workdir(
           RepositoryHandle repo,
           ObjectId oldTree,
           GitDiffOptions options)
        {
            using (var osw = new ObjectSafeWrapper(oldTree, repo, true))
            {
                git_diff* diff;
                int res = NativeMethods.git_diff_tree_to_workdir(out diff, repo, osw.ObjectPtr, options);
                Ensure.ZeroResult(res);

                return new DiffHandle(diff, true);
            }
        }

        public static unsafe void git_diff_find_similar(DiffHandle diff, GitDiffFindOptions options)
        {
            int res = NativeMethods.git_diff_find_similar(diff, options);
            Ensure.ZeroResult(res);
        }

        public static unsafe int git_diff_num_deltas(DiffHandle diff)
        {
            return (int)NativeMethods.git_diff_num_deltas(diff);
        }

        public static unsafe git_diff_delta* git_diff_get_delta(DiffHandle diff, int idx)
        {
            return NativeMethods.git_diff_get_delta(diff, (UIntPtr)idx);
        }

        #endregion

        #region git_filter_

        public static void git_filter_register(string name, IntPtr filterPtr, int priority)
        {
            int res = NativeMethods.git_filter_register(name, filterPtr, priority);
            if (res == (int)GitErrorCode.Exists)
            {
                throw new EntryExistsException("A filter with the name '{0}' is already registered", name);
            }
            Ensure.ZeroResult(res);
        }

        public static void git_filter_unregister(string name)
        {
            int res = NativeMethods.git_filter_unregister(name);
            Ensure.ZeroResult(res);
        }

        public static unsafe FilterMode git_filter_source_mode(git_filter_source* filterSource)
        {
            var res = NativeMethods.git_filter_source_mode(filterSource);
            return (FilterMode)res;
        }

        #endregion

        #region git_graph_

        public static unsafe Tuple<int?, int?> git_graph_ahead_behind(RepositoryHandle repo, Commit first, Commit second)
        {
            if (first == null || second == null)
            {
                return new Tuple<int?, int?>(null, null);
            }

            GitOid oid1 = first.Id.Oid;
            GitOid oid2 = second.Id.Oid;

            UIntPtr ahead;
            UIntPtr behind;

            int res = NativeMethods.git_graph_ahead_behind(out ahead, out behind, repo, ref oid1, ref oid2);

            Ensure.ZeroResult(res);

            return new Tuple<int?, int?>((int)ahead, (int)behind);
        }

        public static unsafe bool git_graph_descendant_of(RepositoryHandle repo, ObjectId commitId, ObjectId ancestorId)
        {
            GitOid oid1 = commitId.Oid;
            GitOid oid2 = ancestorId.Oid;
            int res = NativeMethods.git_graph_descendant_of(repo, ref oid1, ref oid2);

            Ensure.BooleanResult(res);

            return (res == 1);
        }

        #endregion

        #region git_ignore_

        public static unsafe void git_ignore_add_rule(RepositoryHandle repo, string rules)
        {
            int res = NativeMethods.git_ignore_add_rule(repo, rules);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_ignore_clear_internal_rules(RepositoryHandle repo)
        {
            int res = NativeMethods.git_ignore_clear_internal_rules(repo);
            Ensure.ZeroResult(res);
        }

        public static unsafe bool git_ignore_path_is_ignored(RepositoryHandle repo, string path)
        {
            int ignored;
            int res = NativeMethods.git_ignore_path_is_ignored(out ignored, repo, path);
            Ensure.ZeroResult(res);

            return (ignored != 0);
        }

        #endregion

        #region git_index_

        public static unsafe void git_index_add(IndexHandle index, git_index_entry* entry)
        {
            int res = NativeMethods.git_index_add(index, entry);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_index_add_bypath(IndexHandle index, FilePath path)
        {
            int res = NativeMethods.git_index_add_bypath(index, path);
            Ensure.ZeroResult(res);
        }

        public static unsafe Conflict git_index_conflict_get(
            IndexHandle index,
            string path)
        {
            git_index_entry* ancestor, ours, theirs;

            int res = NativeMethods.git_index_conflict_get(out ancestor,
                                                           out ours,
                                                           out theirs,
                                                           index,
                                                           path);

            if (res == (int)GitErrorCode.NotFound)
            {
                return null;
            }

            Ensure.ZeroResult(res);

            return new Conflict(IndexEntry.BuildFromPtr(ancestor),
                                IndexEntry.BuildFromPtr(ours),
                                IndexEntry.BuildFromPtr(theirs));
        }

        public static unsafe ConflictIteratorHandle git_index_conflict_iterator_new(IndexHandle index)
        {
            git_index_conflict_iterator* iter;
            int res = NativeMethods.git_index_conflict_iterator_new(out iter, index);
            Ensure.ZeroResult(res);

            return new ConflictIteratorHandle(iter, true);
        }

        public static unsafe Conflict git_index_conflict_next(ConflictIteratorHandle iterator)
        {
            git_index_entry* ancestor, ours, theirs;

            int res = NativeMethods.git_index_conflict_next(out ancestor, out ours, out theirs, iterator);

            if (res == (int)GitErrorCode.IterOver)
            {
                return null;
            }

            Ensure.ZeroResult(res);

            return new Conflict(IndexEntry.BuildFromPtr(ancestor),
                                IndexEntry.BuildFromPtr(ours),
                                IndexEntry.BuildFromPtr(theirs));
        }

        public static unsafe int git_index_entrycount(IndexHandle index)
        {
            return NativeMethods.git_index_entrycount(index)
                .ConvertToInt();
        }

        public static unsafe StageLevel git_index_entry_stage(git_index_entry* entry)
        {
            return (StageLevel)NativeMethods.git_index_entry_stage(entry);
        }

        public static unsafe git_index_entry* git_index_get_byindex(IndexHandle index, UIntPtr n)
        {
            return NativeMethods.git_index_get_byindex(index, n);
        }

        public static unsafe git_index_entry* git_index_get_bypath(IndexHandle index, string path, int stage)
        {
            return NativeMethods.git_index_get_bypath(index, path, stage);
        }

        public static unsafe bool git_index_has_conflicts(IndexHandle index)
        {
            int res = NativeMethods.git_index_has_conflicts(index);
            Ensure.BooleanResult(res);

            return res != 0;
        }

        public static unsafe int git_index_name_entrycount(IndexHandle index)
        {
            return NativeMethods.git_index_name_entrycount(index)
                .ConvertToInt();
        }

        public static unsafe git_index_name_entry* git_index_name_get_byindex(IndexHandle index, UIntPtr n)
        {
            return NativeMethods.git_index_name_get_byindex(index, n);
        }

        public static unsafe IndexHandle git_index_open(FilePath indexpath)
        {
            git_index* handle;
            int res = NativeMethods.git_index_open(out handle, indexpath);
            Ensure.ZeroResult(res);

            return new IndexHandle(handle, true);
        }

        public static unsafe void git_index_read(IndexHandle index)
        {
            int res = NativeMethods.git_index_read(index, false);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_index_remove_bypath(IndexHandle index, string path)
        {
            int res = NativeMethods.git_index_remove_bypath(index, path);
            Ensure.ZeroResult(res);
        }

        public static unsafe int git_index_reuc_entrycount(IndexHandle index)
        {
            return NativeMethods.git_index_reuc_entrycount(index)
                .ConvertToInt();
        }

        public static unsafe git_index_reuc_entry* git_index_reuc_get_byindex(IndexHandle index, UIntPtr n)
        {
            return NativeMethods.git_index_reuc_get_byindex(index, n);
        }

        public static unsafe git_index_reuc_entry* git_index_reuc_get_bypath(IndexHandle index, string path)
        {
            return NativeMethods.git_index_reuc_get_bypath(index, path);
        }

        public static unsafe void git_index_write(IndexHandle index)
        {
            int res = NativeMethods.git_index_write(index);
            Ensure.ZeroResult(res);
        }

        public static unsafe ObjectId git_index_write_tree(IndexHandle index)
        {
            GitOid treeOid;
            int res = NativeMethods.git_index_write_tree(out treeOid, index);
            Ensure.ZeroResult(res);

            return treeOid;
        }

        public static unsafe ObjectId git_index_write_tree_to(IndexHandle index, RepositoryHandle repo)
        {
            GitOid treeOid;
            int res = NativeMethods.git_index_write_tree_to(out treeOid, index, repo);
            Ensure.ZeroResult(res);

            return treeOid;
        }

        public static unsafe void git_index_read_fromtree(Index index, ObjectHandle tree)
        {
            int res = NativeMethods.git_index_read_tree(index.Handle, tree);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_index_clear(Index index)
        {
            int res = NativeMethods.git_index_clear(index.Handle);
            Ensure.ZeroResult(res);
        }

        #endregion

        #region git_merge_

        public static unsafe IndexHandle git_merge_commits(RepositoryHandle repo, ObjectHandle ourCommit, ObjectHandle theirCommit, GitMergeOpts opts, out bool earlyStop)
        {
            git_index* index;
            int res = NativeMethods.git_merge_commits(out index, repo, ourCommit, theirCommit, ref opts);
            if (res == (int)GitErrorCode.MergeConflict)
            {
                earlyStop = true;
            }
            else
            {
                earlyStop = false;
                Ensure.ZeroResult(res);
            }

            return new IndexHandle(index, true);
        }

        public static unsafe ObjectId git_merge_base_many(RepositoryHandle repo, GitOid[] commitIds)
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

        public static unsafe ObjectId git_merge_base_octopus(RepositoryHandle repo, GitOid[] commitIds)
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

        public static unsafe AnnotatedCommitHandle git_annotated_commit_from_fetchhead(RepositoryHandle repo, string branchName, string remoteUrl, GitOid oid)
        {
            git_annotated_commit* commit;

            int res = NativeMethods.git_annotated_commit_from_fetchhead(out commit, repo, branchName, remoteUrl, ref oid);

            Ensure.ZeroResult(res);

            return new AnnotatedCommitHandle(commit, true);
        }

        public static unsafe AnnotatedCommitHandle git_annotated_commit_lookup(RepositoryHandle repo, GitOid oid)
        {
            git_annotated_commit* commit;

            int res = NativeMethods.git_annotated_commit_lookup(out commit, repo, ref oid);

            Ensure.ZeroResult(res);

            return new AnnotatedCommitHandle(commit, true);
        }

        public static unsafe AnnotatedCommitHandle git_annotated_commit_from_ref(RepositoryHandle repo, ReferenceHandle reference)
        {
            git_annotated_commit* commit;

            int res = NativeMethods.git_annotated_commit_from_ref(out commit, repo, reference);

            Ensure.ZeroResult(res);

            return new AnnotatedCommitHandle(commit, true);
        }

        public static unsafe AnnotatedCommitHandle git_annotated_commit_from_revspec(RepositoryHandle repo, string revspec)
        {
            git_annotated_commit* commit;

            int res = NativeMethods.git_annotated_commit_from_revspec(out commit, repo, revspec);

            Ensure.ZeroResult(res);

            return new AnnotatedCommitHandle(commit, true);
        }

        public static unsafe ObjectId git_annotated_commit_id(AnnotatedCommitHandle mergeHead)
        {
            return ObjectId.BuildFromPtr(NativeMethods.git_annotated_commit_id(mergeHead));
        }

        public static unsafe void git_merge(RepositoryHandle repo, AnnotatedCommitHandle[] heads, GitMergeOpts mergeOptions, GitCheckoutOpts checkoutOptions, out bool earlyStop)
        {
            IntPtr[] their_heads = heads.Select(head => head.AsIntPtr()).ToArray();

            int res = NativeMethods.git_merge(repo,
                                              their_heads,
                                              (UIntPtr)their_heads.Length,
                                              ref mergeOptions,
                                              ref checkoutOptions);

            if (res == (int)GitErrorCode.MergeConflict)
            {
                earlyStop = true;
            }
            else
            {
                earlyStop = false;
                Ensure.ZeroResult(res);
            }
        }

        public static unsafe void git_merge_analysis(
            RepositoryHandle repo,
            AnnotatedCommitHandle[] heads,
            out GitMergeAnalysis analysis_out,
            out GitMergePreference preference_out)
        {
            IntPtr[] their_heads = heads.Select(head => head.AsIntPtr()).ToArray();

            int res = NativeMethods.git_merge_analysis(out analysis_out,
                                                       out preference_out,
                                                       repo,
                                                       their_heads,
                                                       their_heads.Length);

            Ensure.ZeroResult(res);
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

            using (var buf = new GitBuf())
            {
                int res = NativeMethods.git_message_prettify(buf, message, true, (sbyte)comment);
                Ensure.Int32Result(res);

                return LaxUtf8Marshaler.FromNative(buf.ptr) ?? string.Empty;
            }
        }

        #endregion

        #region git_note_

        public static unsafe ObjectId git_note_create(
            RepositoryHandle repo,
            string notes_ref,
            Signature author,
            Signature committer,
            ObjectId targetId,
            string note,
            bool force)
        {
            using (SignatureHandle authorHandle = author.BuildHandle())
            using (SignatureHandle committerHandle = committer.BuildHandle())
            {
                GitOid noteOid;
                GitOid oid = targetId.Oid;

                int res = NativeMethods.git_note_create(out noteOid, repo, notes_ref, authorHandle, committerHandle, ref oid, note, force ? 1 : 0);
                Ensure.ZeroResult(res);

                return noteOid;
            }
        }

        public static unsafe string git_note_default_ref(RepositoryHandle repo)
        {
            using (var buf = new GitBuf())
            {
                int res = NativeMethods.git_note_default_ref(buf, repo);
                Ensure.ZeroResult(res);

                return LaxUtf8Marshaler.FromNative(buf.ptr);
            }
        }

        public static unsafe ICollection<TResult> git_note_foreach<TResult>(RepositoryHandle repo, string notes_ref, Func<GitOid, GitOid, TResult> resultSelector)
        {
            return git_foreach(resultSelector, c => NativeMethods.git_note_foreach(repo,
                                                                                   notes_ref,
                                                                                   (ref GitOid x, ref GitOid y, IntPtr p) => c(x, y, p),
                                                                                   IntPtr.Zero), GitErrorCode.NotFound);
        }

        public static unsafe string git_note_message(NoteHandle note)
        {
            return NativeMethods.git_note_message(note);
        }

        public static unsafe ObjectId git_note_id(NoteHandle note)
        {
            return ObjectId.BuildFromPtr(NativeMethods.git_note_id(note));
        }

        public static unsafe NoteHandle git_note_read(RepositoryHandle repo, string notes_ref, ObjectId id)
        {
            GitOid oid = id.Oid;
            git_note* note;

            int res = NativeMethods.git_note_read(out note, repo, notes_ref, ref oid);

            if (res == (int)GitErrorCode.NotFound)
            {
                return null;
            }

            Ensure.ZeroResult(res);

            return new NoteHandle(note, true);
        }

        public static unsafe void git_note_remove(RepositoryHandle repo, string notes_ref, Signature author, Signature committer, ObjectId targetId)
        {
            using (SignatureHandle authorHandle = author.BuildHandle())
            using (SignatureHandle committerHandle = committer.BuildHandle())
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

        public static unsafe ObjectId git_object_id(ObjectHandle obj)
        {
            return ObjectId.BuildFromPtr(NativeMethods.git_object_id(obj));
        }

        public static unsafe ObjectHandle git_object_lookup(RepositoryHandle repo, ObjectId id, GitObjectType type)
        {
            git_object* handle;
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

            return new ObjectHandle(handle, true);
        }

        public static unsafe ObjectHandle git_object_peel(RepositoryHandle repo, ObjectId id, GitObjectType type, bool throwsIfCanNotPeel)
        {
            git_object* peeled;
            int res;

            using (var obj = new ObjectSafeWrapper(id, repo))
            {
                res = NativeMethods.git_object_peel(out peeled, obj.ObjectPtr, type);
            }

            if (!throwsIfCanNotPeel &&
                (res == (int)GitErrorCode.NotFound || res == (int)GitErrorCode.Ambiguous ||
                 res == (int)GitErrorCode.InvalidSpecification || res == (int)GitErrorCode.Peel))
            {
                return null;
            }

            Ensure.ZeroResult(res);
            return new ObjectHandle(peeled, true);
        }

        public static unsafe string git_object_short_id(RepositoryHandle repo, ObjectId id)
        {
            using (var obj = new ObjectSafeWrapper(id, repo))
            using (var buf = new GitBuf())
            {
                int res = NativeMethods.git_object_short_id(buf, obj.ObjectPtr);
                Ensure.Int32Result(res);

                return LaxUtf8Marshaler.FromNative(buf.ptr);
            }
        }

        public static unsafe GitObjectType git_object_type(ObjectHandle obj)
        {
            return NativeMethods.git_object_type(obj);
        }

        #endregion

        #region git_odb_

        public static unsafe void git_odb_add_backend(ObjectDatabaseHandle odb, IntPtr backend, int priority)
        {
            Ensure.ZeroResult(NativeMethods.git_odb_add_backend(odb, backend, priority));
        }

        public static IntPtr git_odb_backend_malloc(IntPtr backend, UIntPtr len)
        {
            IntPtr toReturn = NativeMethods.git_odb_backend_malloc(backend, len);

            if (IntPtr.Zero == toReturn)
            {
                throw new LibGit2SharpException("Unable to allocate {0} bytes; out of memory",
                                                len,
                                                GitErrorCode.Error,
                                                GitErrorCategory.NoMemory);
            }

            return toReturn;
        }

        public static unsafe bool git_odb_exists(ObjectDatabaseHandle odb, ObjectId id)
        {
            GitOid oid = id.Oid;

            int res = NativeMethods.git_odb_exists(odb, ref oid);
            Ensure.BooleanResult(res);

            return (res == 1);
        }

        public static unsafe GitObjectMetadata git_odb_read_header(ObjectDatabaseHandle odb, ObjectId id)
        {
            GitOid oid = id.Oid;
            UIntPtr length;
            GitObjectType objectType;

            int res = NativeMethods.git_odb_read_header(out length, out objectType, odb, ref oid);
            Ensure.ZeroResult(res);

            return new GitObjectMetadata((long)length, objectType);
        }

        public static unsafe ICollection<ObjectId> git_odb_foreach(ObjectDatabaseHandle odb)
        {
            var list = new List<ObjectId>();

            NativeMethods.git_odb_foreach(odb, (p, _data) =>
                {
                    list.Add(ObjectId.BuildFromPtr(p));
                    return 0;
                }, IntPtr.Zero);

            return list;
        }

        public static unsafe OdbStreamHandle git_odb_open_wstream(ObjectDatabaseHandle odb, long size, GitObjectType type)
        {
            git_odb_stream* stream;
            int res = NativeMethods.git_odb_open_wstream(out stream, odb, size, type);
            Ensure.ZeroResult(res);

            return new OdbStreamHandle(stream, true);
        }

        public static void git_odb_stream_write(OdbStreamHandle stream, byte[] data, int len)
        {
            int res;
            unsafe
            {
                fixed (byte* p = data)
                {
                    res = NativeMethods.git_odb_stream_write(stream, (IntPtr)p, (UIntPtr)len);
                }
            }

            Ensure.ZeroResult(res);
        }

        public static unsafe ObjectId git_odb_stream_finalize_write(OdbStreamHandle stream)
        {
            GitOid id;
            int res = NativeMethods.git_odb_stream_finalize_write(out id, stream);
            Ensure.ZeroResult(res);

            return id;
        }

        public static unsafe ObjectId git_odb_write(ObjectDatabaseHandle odb, byte[] data, ObjectType type)
        {
            GitOid id;
            int res;
            fixed(byte* p = data)
            {
                res = NativeMethods.git_odb_write(out id, odb, p, new UIntPtr((ulong)data.LongLength), type.ToGitObjectType());
            }
            Ensure.ZeroResult(res);

            return id;
        }

#endregion

#region git_patch_

        public static unsafe PatchHandle git_patch_from_diff(DiffHandle diff, int idx)
        {
            git_patch* handle;
            int res = NativeMethods.git_patch_from_diff(out handle, diff, (UIntPtr)idx);
            Ensure.ZeroResult(res);
            return new PatchHandle(handle, true);
        }

        public static unsafe void git_patch_print(PatchHandle patch, NativeMethods.git_diff_line_cb printCallback)
        {
            int res = NativeMethods.git_patch_print(patch, printCallback, IntPtr.Zero);
            Ensure.ZeroResult(res);
        }

        public static unsafe Tuple<int, int> git_patch_line_stats(PatchHandle patch)
        {
            UIntPtr ctx, add, del;
            int res = NativeMethods.git_patch_line_stats(out ctx, out add, out del, patch);
            Ensure.ZeroResult(res);
            return new Tuple<int, int>((int)add, (int)del);
        }

#endregion

#region git_packbuilder_

        public static unsafe PackBuilderHandle git_packbuilder_new(RepositoryHandle repo)
        {
            git_packbuilder* handle;

            int res = NativeMethods.git_packbuilder_new(out handle, repo);
            Ensure.ZeroResult(res);

            return new PackBuilderHandle(handle, true);
        }

        public static unsafe void git_packbuilder_insert(PackBuilderHandle packbuilder, ObjectId targetId, string name)
        {
            GitOid oid = targetId.Oid;

            int res = NativeMethods.git_packbuilder_insert(packbuilder, ref oid, name);
            Ensure.ZeroResult(res);
        }

        internal static unsafe void git_packbuilder_insert_commit(PackBuilderHandle packbuilder, ObjectId targetId)
        {
            GitOid oid = targetId.Oid;

            int res = NativeMethods.git_packbuilder_insert_commit(packbuilder, ref oid);
            Ensure.ZeroResult(res);
        }

        internal static unsafe void git_packbuilder_insert_tree(PackBuilderHandle packbuilder, ObjectId targetId)
        {
            GitOid oid = targetId.Oid;

            int res = NativeMethods.git_packbuilder_insert_tree(packbuilder, ref oid);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_packbuilder_insert_recur(PackBuilderHandle packbuilder, ObjectId targetId, string name)
        {
            GitOid oid = targetId.Oid;

            int res = NativeMethods.git_packbuilder_insert_recur(packbuilder, ref oid, name);
            Ensure.ZeroResult(res);
        }

        public static unsafe uint git_packbuilder_set_threads(PackBuilderHandle packbuilder, uint numThreads)
        {
            return NativeMethods.git_packbuilder_set_threads(packbuilder, numThreads);
        }

        public static unsafe void git_packbuilder_write(PackBuilderHandle packbuilder, FilePath path)
        {
            int res = NativeMethods.git_packbuilder_write(packbuilder, path, 0, IntPtr.Zero, IntPtr.Zero);
            Ensure.ZeroResult(res);
        }

        public static unsafe UIntPtr git_packbuilder_object_count(PackBuilderHandle packbuilder)
        {
            return NativeMethods.git_packbuilder_object_count(packbuilder);
        }

        public static unsafe UIntPtr git_packbuilder_written(PackBuilderHandle packbuilder)
        {
            return NativeMethods.git_packbuilder_written(packbuilder);
        }
#endregion

#region git_rebase

        public static unsafe RebaseHandle git_rebase_init(
            RepositoryHandle repo,
            AnnotatedCommitHandle branch,
            AnnotatedCommitHandle upstream,
            AnnotatedCommitHandle onto,
            GitRebaseOptions options)
        {
            git_rebase* rebase = null;

            int result = NativeMethods.git_rebase_init(out rebase, repo, branch, upstream, onto, options);
            Ensure.ZeroResult(result);

            return new RebaseHandle(rebase, true);
        }

        public static unsafe RebaseHandle git_rebase_open(RepositoryHandle repo, GitRebaseOptions options)
        {
            git_rebase* rebase = null;

            int result = NativeMethods.git_rebase_open(out rebase, repo, options);
            Ensure.ZeroResult(result);

            return new RebaseHandle(rebase, true);
        }

        public static unsafe long git_rebase_operation_entrycount(RebaseHandle rebase)
        {
            return NativeMethods.git_rebase_operation_entrycount(rebase).ConvertToLong();
        }

        public static unsafe long git_rebase_operation_current(RebaseHandle rebase)
        {
            UIntPtr result = NativeMethods.git_rebase_operation_current(rebase);

            if (result == GIT_REBASE_NO_OPERATION)
            {
                return RebaseNoOperation;
            }
            else
            {
                return result.ConvertToLong();
            }
        }

        /// <summary>
        /// The value from the native layer indicating that no rebase operation is in progress.
        /// </summary>
        private static UIntPtr GIT_REBASE_NO_OPERATION
        {
            get
            {
                return UIntPtr.Size == 4 ? new UIntPtr(uint.MaxValue) : new UIntPtr(ulong.MaxValue);
            }
        }

        public const long RebaseNoOperation = -1;

        public static unsafe git_rebase_operation* git_rebase_operation_byindex(
            RebaseHandle rebase,
            long index)
        {
            Debug.Assert(index >= 0);
            return NativeMethods.git_rebase_operation_byindex(rebase, ((UIntPtr)index));
        }

        /// <summary>
        /// Returns null when finished.
        /// </summary>
        /// <param name="rebase"></param>
        /// <returns></returns>
        public static unsafe git_rebase_operation* git_rebase_next(RebaseHandle rebase)
        {
            git_rebase_operation* ptr;
            int result = NativeMethods.git_rebase_next(out ptr, rebase);
            if (result == (int)GitErrorCode.IterOver)
            {
                return null;
            }
            Ensure.ZeroResult(result);

            return ptr;
        }

        public static unsafe GitRebaseCommitResult git_rebase_commit(
            RebaseHandle rebase,
            Identity author,
            Identity committer)
        {
            Ensure.ArgumentNotNull(rebase, "rebase");
            Ensure.ArgumentNotNull(committer, "committer");

            using (SignatureHandle committerHandle = committer.BuildNowSignatureHandle())
            using (SignatureHandle authorHandle = author.SafeBuildNowSignatureHandle())
            {
                GitRebaseCommitResult commitResult = new GitRebaseCommitResult();

                int result = NativeMethods.git_rebase_commit(ref commitResult.CommitId, rebase, authorHandle, committerHandle, IntPtr.Zero, IntPtr.Zero);

                if (result == (int)GitErrorCode.Applied)
                {
                    commitResult.CommitId = GitOid.Empty;
                    commitResult.WasPatchAlreadyApplied = true;
                }
                else
                {
                    Ensure.ZeroResult(result);
                }

                return commitResult;
            }
        }

        /// <summary>
        /// Struct to report the result of calling git_rebase_commit.
        /// </summary>
        public struct GitRebaseCommitResult
        {
            /// <summary>
            /// The ID of the commit that was generated, if any
            /// </summary>
            public GitOid CommitId;

            /// <summary>
            /// bool to indicate if the patch was already applied.
            /// If Patch was already applied, then CommitId will be empty (all zeros).
            /// </summary>
            public bool WasPatchAlreadyApplied;
        }

        public static unsafe void git_rebase_abort(
            RebaseHandle rebase)
        {
            Ensure.ArgumentNotNull(rebase, "rebase");

            int result = NativeMethods.git_rebase_abort(rebase);
            Ensure.ZeroResult(result);
        }

        public static unsafe void git_rebase_finish(
            RebaseHandle rebase,
            Identity committer)
        {
            Ensure.ArgumentNotNull(rebase, "rebase");
            Ensure.ArgumentNotNull(committer, "committer");

            using (var signatureHandle = committer.BuildNowSignatureHandle())
            {
                int result = NativeMethods.git_rebase_finish(rebase, signatureHandle);
                Ensure.ZeroResult(result);
            }
        }

#endregion

#region git_reference_

        public static unsafe ReferenceHandle git_reference_create(
            RepositoryHandle repo,
            string name,
            ObjectId targetId,
            bool allowOverwrite,
            string logMessage)
        {
            GitOid oid = targetId.Oid;
            git_reference* handle;

            int res = NativeMethods.git_reference_create(out handle, repo, name, ref oid, allowOverwrite, logMessage);
            Ensure.ZeroResult(res);

            return new ReferenceHandle(handle, true);
        }

        public static unsafe ReferenceHandle git_reference_symbolic_create(
            RepositoryHandle repo,
            string name,
            string target,
            bool allowOverwrite,
            string logMessage)
        {
            git_reference* handle;
            int res = NativeMethods.git_reference_symbolic_create(out handle, repo, name, target, allowOverwrite,
                logMessage);
            Ensure.ZeroResult(res);

            return new ReferenceHandle(handle, true);
        }

        public static unsafe ICollection<TResult> git_reference_foreach_glob<TResult>(
            RepositoryHandle repo,
            string glob,
            Func<IntPtr, TResult> resultSelector)
        {
            return git_foreach(resultSelector, c => NativeMethods.git_reference_foreach_glob(repo, glob, (x, p) => c(x, p), IntPtr.Zero));
        }

        public static bool git_reference_is_valid_name(string refname)
        {
            int res = NativeMethods.git_reference_is_valid_name(refname);
            Ensure.BooleanResult(res);

            return (res == 1);
        }

        public static unsafe IList<string> git_reference_list(RepositoryHandle repo)
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

        public static unsafe ReferenceHandle git_reference_lookup(RepositoryHandle repo, string name, bool shouldThrowIfNotFound)
        {
            git_reference* handle;
            int res = NativeMethods.git_reference_lookup(out handle, repo, name);

            if (!shouldThrowIfNotFound && res == (int)GitErrorCode.NotFound)
            {
                return null;
            }

            Ensure.ZeroResult(res);

            return new ReferenceHandle(handle, true);
        }

        public static unsafe string git_reference_name(git_reference* reference)
        {
            return NativeMethods.git_reference_name(reference);
        }

        public static unsafe void git_reference_remove(RepositoryHandle repo, string name)
        {
            int res = NativeMethods.git_reference_remove(repo, name);
            Ensure.ZeroResult(res);
        }

        public static unsafe ObjectId git_reference_target(git_reference* reference)
        {
            return ObjectId.BuildFromPtr(NativeMethods.git_reference_target(reference));
        }

        public static unsafe ReferenceHandle git_reference_rename(
            ReferenceHandle reference,
            string newName,
            bool allowOverwrite,
            string logMessage)
        {
            git_reference* ref_out;

            int res = NativeMethods.git_reference_rename(out ref_out, reference, newName, allowOverwrite, logMessage);
            Ensure.ZeroResult(res);

            return new ReferenceHandle(ref_out, true);
        }

        public static unsafe ReferenceHandle git_reference_set_target(ReferenceHandle reference, ObjectId id, string logMessage)
        {
            GitOid oid = id.Oid;
            git_reference* ref_out;

            int res = NativeMethods.git_reference_set_target(out ref_out, reference, ref oid, logMessage);
            Ensure.ZeroResult(res);

            return new ReferenceHandle(ref_out, true);
        }

        public static unsafe ReferenceHandle git_reference_symbolic_set_target(ReferenceHandle reference, string target, string logMessage)
        {
            git_reference* ref_out;

            int res = NativeMethods.git_reference_symbolic_set_target(out ref_out, reference, target, logMessage);
            Ensure.ZeroResult(res);

            return new ReferenceHandle(ref_out, true);
        }

        public static unsafe string git_reference_symbolic_target(git_reference* reference)
        {
            return NativeMethods.git_reference_symbolic_target(reference);
        }

        public static unsafe GitReferenceType git_reference_type(git_reference* reference)
        {
            return NativeMethods.git_reference_type(reference);
        }

        public static unsafe void git_reference_ensure_log(RepositoryHandle repo, string refname)
        {
            int res = NativeMethods.git_reference_ensure_log(repo, refname);
            Ensure.ZeroResult(res);
        }

#endregion

#region git_reflog_

        public static unsafe ReflogHandle git_reflog_read(RepositoryHandle repo, string canonicalName)
        {
            git_reflog* reflog_out;

            int res = NativeMethods.git_reflog_read(out reflog_out, repo, canonicalName);
            Ensure.ZeroResult(res);

            return new ReflogHandle(reflog_out, true);
        }

        public static unsafe int git_reflog_entrycount(ReflogHandle reflog)
        {
            return (int)NativeMethods.git_reflog_entrycount(reflog);
        }

        public static unsafe git_reflog_entry* git_reflog_entry_byindex(ReflogHandle reflog, int idx)
        {
            return NativeMethods.git_reflog_entry_byindex(reflog, (UIntPtr)idx);
        }

        public static unsafe ObjectId git_reflog_entry_id_old(git_reflog_entry* entry)
        {
            return ObjectId.BuildFromPtr(NativeMethods.git_reflog_entry_id_old(entry));
        }

        public static unsafe ObjectId git_reflog_entry_id_new(git_reflog_entry* entry)
        {
            return ObjectId.BuildFromPtr(NativeMethods.git_reflog_entry_id_new(entry));
        }

        public static unsafe Signature git_reflog_entry_committer(git_reflog_entry* entry)
        {
            return new Signature(NativeMethods.git_reflog_entry_committer(entry));
        }

        public static unsafe string git_reflog_entry_message(git_reflog_entry* entry)
        {
            return NativeMethods.git_reflog_entry_message(entry);
        }

#endregion

#region git_refspec

        public static unsafe string git_refspec_transform(IntPtr refSpecPtr, string name)
        {
            using (var buf = new GitBuf())
            {
                int res = NativeMethods.git_refspec_transform(buf, refSpecPtr, name);
                Ensure.ZeroResult(res);

                return LaxUtf8Marshaler.FromNative(buf.ptr) ?? string.Empty;
            }
        }

        public static unsafe string git_refspec_rtransform(IntPtr refSpecPtr, string name)
        {
            using (var buf = new GitBuf())
            {
                int res = NativeMethods.git_refspec_rtransform(buf, refSpecPtr, name);
                Ensure.ZeroResult(res);

                return LaxUtf8Marshaler.FromNative(buf.ptr) ?? string.Empty;
            }
        }

        public static unsafe string git_refspec_string(IntPtr refspec)
        {
            return NativeMethods.git_refspec_string(refspec);
        }

        public static unsafe string git_refspec_src(IntPtr refSpec)
        {
            return NativeMethods.git_refspec_src(refSpec);
        }

        public static unsafe string git_refspec_dst(IntPtr refSpec)
        {
            return NativeMethods.git_refspec_dst(refSpec);
        }

        public static unsafe RefSpecDirection git_refspec_direction(IntPtr refSpec)
        {
            return NativeMethods.git_refspec_direction(refSpec);
        }

        public static unsafe bool git_refspec_force(IntPtr refSpec)
        {
            return NativeMethods.git_refspec_force(refSpec);
        }

        public static bool git_refspec_src_matches(IntPtr refspec, string reference)
        {
            return NativeMethods.git_refspec_src_matches(refspec, reference);
        }

        public static bool git_refspec_dst_matches(IntPtr refspec, string reference)
        {
            return NativeMethods.git_refspec_dst_matches(refspec, reference);
        }

#endregion

#region git_remote_

        public static unsafe TagFetchMode git_remote_autotag(RemoteHandle remote)
        {
            return (TagFetchMode)NativeMethods.git_remote_autotag(remote);
        }

        public static unsafe RemoteHandle git_remote_create(RepositoryHandle repo, string name, string url)
        {
            git_remote* handle;
            int res = NativeMethods.git_remote_create(out handle, repo, name, url);
            Ensure.ZeroResult(res);

            return new RemoteHandle(handle, true);
        }

        public static unsafe RemoteHandle git_remote_create_with_fetchspec(RepositoryHandle repo, string name, string url, string refspec)
        {
            git_remote* handle;
            int res = NativeMethods.git_remote_create_with_fetchspec(out handle, repo, name, url, refspec);
            Ensure.ZeroResult(res);

            return new RemoteHandle(handle, true);
        }

        public static unsafe RemoteHandle git_remote_create_anonymous(RepositoryHandle repo, string url)
        {
            git_remote* handle;
            int res = NativeMethods.git_remote_create_anonymous(out handle, repo, url);
            Ensure.ZeroResult(res);

            return new RemoteHandle(handle, true);
        }

        public static unsafe void git_remote_connect(RemoteHandle remote, GitDirection direction, ref GitRemoteCallbacks remoteCallbacks, ref GitProxyOptions proxyOptions)
        {
            GitStrArrayManaged customHeaders = new GitStrArrayManaged();

            try
            {
                int res = NativeMethods.git_remote_connect(remote, direction, ref remoteCallbacks, ref proxyOptions, ref customHeaders.Array);
                Ensure.ZeroResult(res);
            }
            catch (Exception)
            {
                customHeaders.Dispose();
            }
        }

        public static unsafe void git_remote_delete(RepositoryHandle repo, string name)
        {
            int res = NativeMethods.git_remote_delete(repo, name);

            if (res == (int)GitErrorCode.NotFound)
            {
                return;
            }

            Ensure.ZeroResult(res);
        }

        public static unsafe git_refspec* git_remote_get_refspec(RemoteHandle remote, int n)
        {
            return NativeMethods.git_remote_get_refspec(remote, (UIntPtr)n);
        }

        public static unsafe int git_remote_refspec_count(RemoteHandle remote)
        {
            return (int)NativeMethods.git_remote_refspec_count(remote);
        }

        public static unsafe IList<string> git_remote_get_fetch_refspecs(RemoteHandle remote)
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

        public static unsafe IList<string> git_remote_get_push_refspecs(RemoteHandle remote)
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

        public static unsafe void git_remote_push(RemoteHandle remote, IEnumerable<string> refSpecs, GitPushOptions opts)
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

        public static unsafe void git_remote_set_url(RepositoryHandle repo, string remote, string url)
        {
            int res = NativeMethods.git_remote_set_url(repo, remote, url);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_remote_add_fetch(RepositoryHandle repo, string remote, string url)
        {
            int res = NativeMethods.git_remote_add_fetch(repo, remote, url);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_remote_set_pushurl(RepositoryHandle repo, string remote, string url)
        {
            int res = NativeMethods.git_remote_set_pushurl(repo, remote, url);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_remote_add_push(RepositoryHandle repo, string remote, string url)
        {
            int res = NativeMethods.git_remote_add_push(repo, remote, url);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_remote_fetch(
            RemoteHandle remote, IEnumerable<string> refSpecs,
            GitFetchOptions fetchOptions, string logMessage)
        {
            var array = new GitStrArrayManaged();

            try
            {
                array = GitStrArrayManaged.BuildFrom(refSpecs.ToArray());

                int res = NativeMethods.git_remote_fetch(remote, ref array.Array, fetchOptions, logMessage);
                Ensure.ZeroResult(res);
            }
            finally
            {
                array.Dispose();
            }
        }

        public static bool git_remote_is_valid_name(string refname)
        {
            int res = NativeMethods.git_remote_is_valid_name(refname);
            Ensure.BooleanResult(res);

            return (res == 1);
        }

        public static unsafe IList<string> git_remote_list(RepositoryHandle repo)
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

        public static unsafe IEnumerable<Reference> git_remote_ls(Repository repository, RemoteHandle remote)
        {
            git_remote_head** heads;
            UIntPtr count;

            int res = NativeMethods.git_remote_ls(out heads, out count, remote);
            Ensure.ZeroResult(res);

            var intCount = checked(count.ToUInt32());
            var directRefs = new Dictionary<string, Reference>();
            var symRefs = new Dictionary<string, string>();

            for (int i = 0; i < intCount; i++)
            {
                git_remote_head* currentHead = heads[i];
                string name = LaxUtf8Marshaler.FromNative(currentHead->Name);
                string symRefTarget = LaxUtf8Marshaler.FromNative(currentHead->SymrefTarget);

                // The name pointer should never be null - if it is,
                // this indicates a bug somewhere (libgit2, server, etc).
                if (string.IsNullOrEmpty(name))
                {
                    throw new InvalidOperationException("Not expecting null value for reference name.");
                }

                if (!string.IsNullOrEmpty(symRefTarget))
                {
                    symRefs.Add(name, symRefTarget);
                }
                else
                {
                    directRefs.Add(name, new DirectReference(name, repository, new ObjectId(currentHead->Oid.Id)));
                }
            }

            for (int i = 0; i < symRefs.Count; i++)
            {
                var symRef = symRefs.ElementAt(i);

                if (!directRefs.ContainsKey(symRef.Value))
                {
                    throw new InvalidOperationException("Symbolic reference target not found in direct reference results.");
                }

                directRefs.Add(symRef.Key, new SymbolicReference(repository, symRef.Key, symRef.Value, directRefs[symRef.Value]));
            }

            var refs = directRefs.Values.ToList();
            refs.Sort((r1, r2) => String.CompareOrdinal(r1.CanonicalName, r2.CanonicalName));

            return refs;
        }

        public static unsafe RemoteHandle git_remote_lookup(RepositoryHandle repo, string name, bool throwsIfNotFound)
        {
            git_remote* handle;
            int res = NativeMethods.git_remote_lookup(out handle, repo, name);

            if (res == (int)GitErrorCode.NotFound && !throwsIfNotFound)
            {
                return null;
            }

            Ensure.ZeroResult(res);
            return new RemoteHandle(handle, true);
        }

        public static unsafe string git_remote_name(RemoteHandle remote)
        {
            return NativeMethods.git_remote_name(remote);
        }

        public static unsafe void git_remote_rename(RepositoryHandle repo, string name, string new_name, RemoteRenameFailureHandler callback)
        {
            if (callback == null)
            {
                callback = problem => { };
            }

            var array = new GitStrArrayNative();

            try
            {
                int res = NativeMethods.git_remote_rename(ref array.Array,
                                                          repo,
                                                          name,
                                                          new_name);

                if (res == (int)GitErrorCode.NotFound)
                {
                    throw new NotFoundException("Remote '{0}' does not exist and cannot be renamed.", name);
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

        public static unsafe void git_remote_set_autotag(RepositoryHandle repo, string remote, TagFetchMode value)
        {
            NativeMethods.git_remote_set_autotag(repo, remote, value);
        }

        public static unsafe string git_remote_url(RemoteHandle remote)
        {
            return NativeMethods.git_remote_url(remote);
        }

        public static unsafe string git_remote_pushurl(RemoteHandle remote)
        {
            return NativeMethods.git_remote_pushurl(remote);
        }

#endregion

#region git_repository_

        public static FilePath git_repository_discover(FilePath start_path)
        {
            return ConvertPath(buf => NativeMethods.git_repository_discover(buf, start_path, false, null));
        }

        public static unsafe bool git_repository_head_detached(RepositoryHandle repo)
        {
            return RepositoryStateChecker(repo, NativeMethods.git_repository_head_detached);
        }

        public static unsafe ICollection<TResult> git_repository_fetchhead_foreach<TResult>(
            RepositoryHandle repo,
            Func<string, string, GitOid, bool, TResult> resultSelector)
        {
            return git_foreach(resultSelector,
                               c => NativeMethods.git_repository_fetchhead_foreach(repo,
                                                                                   (IntPtr w, IntPtr x, ref GitOid y, bool z, IntPtr p)
                                                                                       => c(LaxUtf8Marshaler.FromNative(w), LaxUtf8Marshaler.FromNative(x), y, z, p),
                                                                                   IntPtr.Zero),
                               GitErrorCode.NotFound);
        }

        public static bool git_repository_head_unborn(RepositoryHandle repo)
        {
            return RepositoryStateChecker(repo, NativeMethods.git_repository_head_unborn);
        }

        public static unsafe IndexHandle git_repository_index(RepositoryHandle repo)
        {
            git_index* handle;
            int res = NativeMethods.git_repository_index(out handle, repo);
            Ensure.ZeroResult(res);

            return new IndexHandle(handle, true);
        }

        public static unsafe RepositoryHandle git_repository_init_ext(
            FilePath workdirPath,
            FilePath gitdirPath,
            bool isBare)
        {
            using (var opts = GitRepositoryInitOptions.BuildFrom(workdirPath, isBare))
            {
                git_repository* repo;
                int res = NativeMethods.git_repository_init_ext(out repo, gitdirPath, opts);
                Ensure.ZeroResult(res);

                return new RepositoryHandle(repo, true);
            }
        }

        public static unsafe bool git_repository_is_bare(RepositoryHandle repo)
        {
            return RepositoryStateChecker(repo, NativeMethods.git_repository_is_bare);
        }

        public static unsafe bool git_repository_is_shallow(RepositoryHandle repo)
        {
            return RepositoryStateChecker(repo, NativeMethods.git_repository_is_shallow);
        }

        public static unsafe void git_repository_state_cleanup(RepositoryHandle repo)
        {
            int res = NativeMethods.git_repository_state_cleanup(repo);
            Ensure.ZeroResult(res);
        }

        public static unsafe ICollection<TResult> git_repository_mergehead_foreach<TResult>(
            RepositoryHandle repo,
            Func<GitOid, TResult> resultSelector)
        {
            return git_foreach(resultSelector,
                               c => NativeMethods.git_repository_mergehead_foreach(repo,
                                                                                   (ref GitOid x, IntPtr p) => c(x, p), IntPtr.Zero),
                               GitErrorCode.NotFound);
        }

        public static unsafe string git_repository_message(RepositoryHandle repo)
        {
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

        public static unsafe ObjectDatabaseHandle git_repository_odb(RepositoryHandle repo)
        {
            git_odb* handle;
            int res = NativeMethods.git_repository_odb(out handle, repo);
            Ensure.ZeroResult(res);

            return new ObjectDatabaseHandle(handle, true);
        }

        public static unsafe RepositoryHandle git_repository_open(string path)
        {
            git_repository* repo;
            int res = NativeMethods.git_repository_open(out repo, path);

            if (res == (int)GitErrorCode.NotFound)
            {
                throw new RepositoryNotFoundException("Path '{0}' doesn't point at a valid Git repository or workdir.",
                                                                    path);
            }

            Ensure.ZeroResult(res);

            return new RepositoryHandle(repo, true);
        }

        public static unsafe RepositoryHandle git_repository_new()
        {
            git_repository* repo;
            int res = NativeMethods.git_repository_new(out repo);

            Ensure.ZeroResult(res);

            return new RepositoryHandle(repo, true);
        }

        public static unsafe void git_repository_open_ext(string path, RepositoryOpenFlags flags, string ceilingDirs)
        {
            int res;
            git_repository *repo;

            res = NativeMethods.git_repository_open_ext(out repo, path, flags, ceilingDirs);
            NativeMethods.git_repository_free(repo);

            if (res == (int)GitErrorCode.NotFound)
            {
                throw new RepositoryNotFoundException("Path '{0}' doesn't point at a valid Git repository or workdir.",
                                                                    path);
            }

            Ensure.ZeroResult(res);
        }

        public static unsafe FilePath git_repository_path(RepositoryHandle repo)
        {
            return NativeMethods.git_repository_path(repo);
        }

        public static unsafe void git_repository_set_config(RepositoryHandle repo, ConfigurationHandle config)
        {
            NativeMethods.git_repository_set_config(repo, config);
        }

        public static unsafe void git_repository_set_ident(RepositoryHandle repo, string name, string email)
        {
            int res = NativeMethods.git_repository_set_ident(repo, name, email);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_repository_set_index(RepositoryHandle repo, IndexHandle index)
        {
            NativeMethods.git_repository_set_index(repo, index);
        }

        public static unsafe void git_repository_set_workdir(RepositoryHandle repo, FilePath workdir)
        {
            int res = NativeMethods.git_repository_set_workdir(repo, workdir, false);
            Ensure.ZeroResult(res);
        }

        public static unsafe CurrentOperation git_repository_state(RepositoryHandle repo)
        {
            int res = NativeMethods.git_repository_state(repo);
            Ensure.Int32Result(res);
            return (CurrentOperation)res;
        }

        public static unsafe FilePath git_repository_workdir(RepositoryHandle repo)
        {
            return NativeMethods.git_repository_workdir(repo);
        }

        public static FilePath git_repository_workdir(IntPtr repo)
        {
            return NativeMethods.git_repository_workdir(repo);
        }

        public static unsafe void git_repository_set_head_detached(RepositoryHandle repo, ObjectId commitish)
        {
            GitOid oid = commitish.Oid;
            int res = NativeMethods.git_repository_set_head_detached(repo, ref oid);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_repository_set_head_detached_from_annotated(RepositoryHandle repo, AnnotatedCommitHandle commit)
        {
            int res = NativeMethods.git_repository_set_head_detached_from_annotated(repo, commit);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_repository_set_head(RepositoryHandle repo, string refname)
        {
            int res = NativeMethods.git_repository_set_head(repo, refname);
            Ensure.ZeroResult(res);
        }

#endregion

#region git_reset_

        public static unsafe void git_reset(
            RepositoryHandle repo,
            ObjectId committishId,
            ResetMode resetKind,
            ref GitCheckoutOpts checkoutOptions)
        {
            using (var osw = new ObjectSafeWrapper(committishId, repo))
            {
                int res = NativeMethods.git_reset(repo, osw.ObjectPtr, resetKind, ref checkoutOptions);
                Ensure.ZeroResult(res);
            }
        }

#endregion

#region git_revert_

        public static unsafe void git_revert(
            RepositoryHandle repo,
            ObjectId commit,
            GitRevertOpts opts)
        {
            using (var nativeCommit = git_object_lookup(repo, commit, GitObjectType.Commit))
            {
                int res = NativeMethods.git_revert(repo, nativeCommit, opts);
                Ensure.ZeroResult(res);
            }
        }

        internal static unsafe IndexHandle git_revert_commit(RepositoryHandle repo, ObjectHandle revertCommit, ObjectHandle ourCommit, uint mainline, GitMergeOpts opts, out bool earlyStop)
        {
            git_index* index;
            int res = NativeMethods.git_revert_commit(out index, repo, revertCommit, ourCommit, mainline, ref opts);
            if (res == (int)GitErrorCode.MergeConflict)
            {
                earlyStop = true;
            }
            else
            {
                earlyStop = false;
                Ensure.ZeroResult(res);
            }
            return new IndexHandle(index, true);
        }
        #endregion

        #region git_revparse_

        public static unsafe Tuple<ObjectHandle, ReferenceHandle> git_revparse_ext(RepositoryHandle repo, string objectish)
        {
            git_object* obj;
            git_reference* reference;
            int res = NativeMethods.git_revparse_ext(out obj, out reference, repo, objectish);

            switch (res)
            {
                case (int)GitErrorCode.NotFound:
                    return null;

                case (int)GitErrorCode.Ambiguous:
                    throw new AmbiguousSpecificationException("Provided abbreviated ObjectId '{0}' is too short.",
                                                              objectish);

                default:
                    Ensure.ZeroResult(res);
                    break;
            }

            return new Tuple<ObjectHandle, ReferenceHandle>(new ObjectHandle(obj, true), new ReferenceHandle(reference, true));
        }

        public static ObjectHandle git_revparse_single(RepositoryHandle repo, string objectish)
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

        public static unsafe void git_revwalk_hide(RevWalkerHandle walker, ObjectId commit_id)
        {
            GitOid oid = commit_id.Oid;
            int res = NativeMethods.git_revwalk_hide(walker, ref oid);
            Ensure.ZeroResult(res);
        }

        public static unsafe RevWalkerHandle git_revwalk_new(RepositoryHandle repo)
        {
            git_revwalk* handle;
            int res = NativeMethods.git_revwalk_new(out handle, repo);
            Ensure.ZeroResult(res);

            return new RevWalkerHandle(handle, true);
        }

        public static unsafe ObjectId git_revwalk_next(RevWalkerHandle walker)
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

        public static unsafe void git_revwalk_push(RevWalkerHandle walker, ObjectId id)
        {
            GitOid oid = id.Oid;
            int res = NativeMethods.git_revwalk_push(walker, ref oid);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_revwalk_reset(RevWalkerHandle walker)
        {
            NativeMethods.git_revwalk_reset(walker);
        }

        public static unsafe void git_revwalk_sorting(RevWalkerHandle walker, CommitSortStrategies options)
        {
            NativeMethods.git_revwalk_sorting(walker, options);
        }

        public static unsafe void git_revwalk_simplify_first_parent(RevWalkerHandle walker)
        {
            NativeMethods.git_revwalk_simplify_first_parent(walker);
        }

#endregion

#region git_signature_

        public static unsafe SignatureHandle git_signature_new(string name, string email, DateTimeOffset when)
        {
            git_signature* ptr;

            int res = NativeMethods.git_signature_new(out ptr, name, email, when.ToUnixTimeSeconds(),
                                                      (int)when.Offset.TotalMinutes);
            Ensure.ZeroResult(res);

            return new SignatureHandle(ptr, true);
        }

        public static unsafe SignatureHandle git_signature_now(string name, string email)
        {
            git_signature* ptr;
            int res = NativeMethods.git_signature_now(out ptr, name, email);
            Ensure.ZeroResult(res);

            return new SignatureHandle(ptr, true);
        }

        public static unsafe git_signature* git_signature_dup(git_signature* sig)
        {
            git_signature* handle;
            int res = NativeMethods.git_signature_dup(out handle, sig);
            Ensure.ZeroResult(res);
            return handle;
        }

#endregion

#region git_stash_

        public static unsafe ObjectId git_stash_save(
            RepositoryHandle repo,
            Signature stasher,
            string prettifiedMessage,
            StashModifiers options)
        {
            using (SignatureHandle sigHandle = stasher.BuildHandle())
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

        public static unsafe ICollection<TResult> git_stash_foreach<TResult>(
            RepositoryHandle repo,
            Func<int, IntPtr, GitOid, TResult> resultSelector)
        {
            return git_foreach(resultSelector,
                               c => NativeMethods.git_stash_foreach(repo,
                                                                    (UIntPtr i, IntPtr m, ref GitOid x, IntPtr p)
                                                                        => c((int)i, m, x, p),
                                                                    IntPtr.Zero),
                               GitErrorCode.NotFound);
        }

        public static unsafe void git_stash_drop(RepositoryHandle repo, int index)
        {
            int res = NativeMethods.git_stash_drop(repo, (UIntPtr)index);
            Ensure.BooleanResult(res);
        }

        private static StashApplyStatus get_stash_status(int res)
        {
            if (res == (int)GitErrorCode.Conflict)
            {
                return StashApplyStatus.Conflicts;
            }

            if (res == (int)GitErrorCode.Uncommitted)
            {
                return StashApplyStatus.UncommittedChanges;
            }

            if (res == (int)GitErrorCode.NotFound)
            {
                return StashApplyStatus.NotFound;
            }

            Ensure.ZeroResult(res);
            return StashApplyStatus.Applied;
        }

        public static unsafe StashApplyStatus git_stash_apply(
            RepositoryHandle repo,
            int index,
            GitStashApplyOpts opts)
        {
            return get_stash_status(NativeMethods.git_stash_apply(repo, (UIntPtr)index, opts));
        }

        public static unsafe StashApplyStatus git_stash_pop(
            RepositoryHandle repo,
            int index,
            GitStashApplyOpts opts)
        {
            return get_stash_status(NativeMethods.git_stash_pop(repo, (UIntPtr)index, opts));
        }

#endregion

#region git_status_

        public static unsafe FileStatus git_status_file(RepositoryHandle repo, FilePath path)
        {
            FileStatus status;
            int res = NativeMethods.git_status_file(out status, repo, path);

            switch (res)
            {
                case (int)GitErrorCode.NotFound:
                    return FileStatus.Nonexistent;

                case (int)GitErrorCode.Ambiguous:
                    throw new AmbiguousSpecificationException("More than one file matches the pathspec '{0}'. " +
                                                              "You can either force a literal path evaluation " +
                                                              "(GIT_STATUS_OPT_DISABLE_PATHSPEC_MATCH), or use git_status_foreach().",
                                                              path);

                default:
                    Ensure.ZeroResult(res);
                    break;
            }

            return status;
        }

        public static unsafe StatusListHandle git_status_list_new(RepositoryHandle repo, GitStatusOptions options)
        {
            git_status_list* ptr;
            int res = NativeMethods.git_status_list_new(out ptr, repo, options);
            Ensure.ZeroResult(res);
            return new StatusListHandle(ptr, true);
        }

        public static unsafe int git_status_list_entrycount(StatusListHandle list)
        {
            int res = NativeMethods.git_status_list_entrycount(list);
            Ensure.Int32Result(res);
            return res;
        }

        public static unsafe git_status_entry* git_status_byindex(StatusListHandle list, long idx)
        {
            return NativeMethods.git_status_byindex(list, (UIntPtr)idx);
        }

#endregion

#region git_submodule_

        /// <summary>
        /// Returns a handle to the corresponding submodule,
        /// or an invalid handle if a submodule is not found.
        /// </summary>
        public static unsafe SubmoduleHandle git_submodule_lookup(RepositoryHandle repo, string name)
        {
            git_submodule* submodule;
            var res = NativeMethods.git_submodule_lookup(out submodule, repo, name);

            switch (res)
            {
                case (int)GitErrorCode.NotFound:
                case (int)GitErrorCode.Exists:
                case (int)GitErrorCode.OrphanedHead:
                    return null;

                default:
                    Ensure.ZeroResult(res);
                    return new SubmoduleHandle(submodule, true);
            }
        }

        public static unsafe string git_submodule_resolve_url(RepositoryHandle repo, string url)
        {
            using (var buf = new GitBuf())
            {
                int res = NativeMethods.git_submodule_resolve_url(buf, repo, url);

                Ensure.ZeroResult(res);
                return LaxUtf8Marshaler.FromNative(buf.ptr);
            }
        }

        public static unsafe ICollection<TResult> git_submodule_foreach<TResult>(RepositoryHandle repo, Func<IntPtr, IntPtr, TResult> resultSelector)
        {
            return git_foreach(resultSelector, c => NativeMethods.git_submodule_foreach(repo, (x, y, p) => c(x, y, p), IntPtr.Zero));
        }

        public static unsafe void git_submodule_add_to_index(SubmoduleHandle submodule, bool write_index)
        {
            var res = NativeMethods.git_submodule_add_to_index(submodule, write_index);
            Ensure.ZeroResult(res);
        }

        public static unsafe void git_submodule_update(SubmoduleHandle submodule, bool init, ref GitSubmoduleUpdateOptions options)
        {
            var res = NativeMethods.git_submodule_update(submodule, init, ref options);
            Ensure.ZeroResult(res);
        }

        public static unsafe string git_submodule_path(SubmoduleHandle submodule)
        {
            return NativeMethods.git_submodule_path(submodule);
        }

        public static unsafe string git_submodule_url(SubmoduleHandle submodule)
        {
            return NativeMethods.git_submodule_url(submodule);
        }

        public static unsafe ObjectId git_submodule_index_id(SubmoduleHandle submodule)
        {
            return ObjectId.BuildFromPtr(NativeMethods.git_submodule_index_id(submodule));
        }

        public static unsafe ObjectId git_submodule_head_id(SubmoduleHandle submodule)
        {
            return ObjectId.BuildFromPtr(NativeMethods.git_submodule_head_id(submodule));
        }

        public static unsafe ObjectId git_submodule_wd_id(SubmoduleHandle submodule)
        {
            return ObjectId.BuildFromPtr(NativeMethods.git_submodule_wd_id(submodule));
        }

        public static unsafe SubmoduleIgnore git_submodule_ignore(SubmoduleHandle submodule)
        {
            return NativeMethods.git_submodule_ignore(submodule);
        }

        public static unsafe SubmoduleUpdate git_submodule_update_strategy(SubmoduleHandle submodule)
        {
            return NativeMethods.git_submodule_update_strategy(submodule);
        }

        public static unsafe SubmoduleRecurse git_submodule_fetch_recurse_submodules(SubmoduleHandle submodule)
        {
            return NativeMethods.git_submodule_fetch_recurse_submodules(submodule);
        }

        public static unsafe void git_submodule_reload(SubmoduleHandle submodule)
        {
            var res = NativeMethods.git_submodule_reload(submodule, false);
            Ensure.ZeroResult(res);
        }

        public static unsafe SubmoduleStatus git_submodule_status(RepositoryHandle repo, string name)
        {
            SubmoduleStatus status;
            var res = NativeMethods.git_submodule_status(out status, repo, name, GitSubmoduleIgnore.Unspecified);
            Ensure.ZeroResult(res);
            return status;
        }

        public static unsafe void git_submodule_init(SubmoduleHandle submodule, bool overwrite)
        {
            var res = NativeMethods.git_submodule_init(submodule, overwrite);
            Ensure.ZeroResult(res);
        }

#endregion

#region git_tag_

        public static unsafe ObjectId git_tag_annotation_create(
            RepositoryHandle repo,
            string name,
            GitObject target,
            Signature tagger,
            string message)
        {
            using (var objectPtr = new ObjectSafeWrapper(target.Id, repo))
            using (SignatureHandle sigHandle = tagger.BuildHandle())
            {
                GitOid oid;
                int res = NativeMethods.git_tag_annotation_create(out oid, repo, name, objectPtr.ObjectPtr, sigHandle, message);
                Ensure.ZeroResult(res);

                return oid;
            }
        }

        public static unsafe ObjectId git_tag_create(
            RepositoryHandle repo,
            string name,
            GitObject target,
            Signature tagger,
            string message,
            bool allowOverwrite)
        {
            using (var objectPtr = new ObjectSafeWrapper(target.Id, repo))
            using (SignatureHandle sigHandle = tagger.BuildHandle())
            {
                GitOid oid;
                int res = NativeMethods.git_tag_create(out oid, repo, name, objectPtr.ObjectPtr, sigHandle, message, allowOverwrite);
                Ensure.ZeroResult(res);

                return oid;
            }
        }

        public static unsafe ObjectId git_tag_create_lightweight(RepositoryHandle repo, string name, GitObject target, bool allowOverwrite)
        {
            using (var objectPtr = new ObjectSafeWrapper(target.Id, repo))
            {
                GitOid oid;
                int res = NativeMethods.git_tag_create_lightweight(out oid, repo, name, objectPtr.ObjectPtr, allowOverwrite);
                Ensure.ZeroResult(res);

                return oid;
            }
        }

        public static unsafe void git_tag_delete(RepositoryHandle repo, string name)
        {
            int res = NativeMethods.git_tag_delete(repo, name);
            Ensure.ZeroResult(res);
        }

        public static unsafe IList<string> git_tag_list(RepositoryHandle repo)
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

        public static unsafe string git_tag_message(ObjectHandle tag)
        {
            return NativeMethods.git_tag_message(tag);
        }

        public static unsafe string git_tag_name(ObjectHandle tag)
        {
            return NativeMethods.git_tag_name(tag);
        }

        public static unsafe Signature git_tag_tagger(ObjectHandle tag)
        {
            git_signature* taggerHandle = NativeMethods.git_tag_tagger(tag);

            // Not all tags have a tagger signature - we need to handle
            // this case.
            Signature tagger = null;
            if (taggerHandle != null)
            {
                tagger = new Signature(taggerHandle);
            }

            return tagger;
        }

        public static unsafe ObjectId git_tag_target_id(ObjectHandle tag)
        {
            return ObjectId.BuildFromPtr(NativeMethods.git_tag_target_id(tag));
        }

        public static unsafe GitObjectType git_tag_target_type(ObjectHandle tag)
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
            int res = NativeMethods.git_trace_set(level, callback);
            Ensure.ZeroResult(res);
        }

#endregion

#region git_transport_

        public static void git_transport_register(String prefix, IntPtr transport_cb, IntPtr param)
        {
            int res = NativeMethods.git_transport_register(prefix, transport_cb, param);

            if (res == (int)GitErrorCode.Exists)
            {
                throw new EntryExistsException("A custom transport for '{0}' is already registered",
                    prefix);
            }

            Ensure.ZeroResult(res);
        }

        public static void git_transport_unregister(String prefix)
        {
            int res = NativeMethods.git_transport_unregister(prefix);

            if (res == (int)GitErrorCode.NotFound)
            {
                throw new NotFoundException("The given transport was not found");
            }

            Ensure.ZeroResult(res);
        }

#endregion

#region git_transport_smart_

        public static int git_transport_smart_credentials(out IntPtr cred, IntPtr transport, string user, int methods)
        {
            return NativeMethods.git_transport_smart_credentials(out cred, transport, user, methods);
        }

#endregion

#region git_tree_

        public static unsafe Mode git_tree_entry_attributes(git_tree_entry* entry)
        {
            return (Mode)NativeMethods.git_tree_entry_filemode(entry);
        }

        public static unsafe TreeEntryHandle git_tree_entry_byindex(ObjectHandle tree, long idx)
        {
            var handle = NativeMethods.git_tree_entry_byindex(tree, (UIntPtr)idx);
            if (handle == null)
            {
                return null;
            }

            return new TreeEntryHandle(handle, false);
        }

        public static unsafe TreeEntryHandle git_tree_entry_bypath(RepositoryHandle repo, ObjectId id, string treeentry_path)
        {
            using (var obj = new ObjectSafeWrapper(id, repo))
            {
                git_tree_entry* treeEntryPtr;
                int res = NativeMethods.git_tree_entry_bypath(out treeEntryPtr, obj.ObjectPtr, treeentry_path);

                if (res == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.ZeroResult(res);

                return new TreeEntryHandle(treeEntryPtr, true);
            }
        }

        public static unsafe ObjectId git_tree_entry_id(git_tree_entry* entry)
        {
            return ObjectId.BuildFromPtr(NativeMethods.git_tree_entry_id(entry));
        }

        public static unsafe string git_tree_entry_name(git_tree_entry* entry)
        {
            return NativeMethods.git_tree_entry_name(entry);
        }

        public static unsafe GitObjectType git_tree_entry_type(git_tree_entry* entry)
        {
            return NativeMethods.git_tree_entry_type(entry);
        }

        public static unsafe int git_tree_entrycount(ObjectHandle tree)
        {
            return (int)NativeMethods.git_tree_entrycount(tree);
        }

#endregion

#region git_treebuilder_

        public static unsafe TreeBuilderHandle git_treebuilder_new(RepositoryHandle repo)
        {
            git_treebuilder* builder;
            int res = NativeMethods.git_treebuilder_new(out builder, repo, IntPtr.Zero);
            Ensure.ZeroResult(res);

            return new TreeBuilderHandle(builder, true);
        }

        public static unsafe void git_treebuilder_insert(TreeBuilderHandle builder, string treeentry_name, TreeEntryDefinition treeEntryDefinition)
        {
            GitOid oid = treeEntryDefinition.TargetId.Oid;
            int res = NativeMethods.git_treebuilder_insert(IntPtr.Zero, builder, treeentry_name, ref oid,
                (uint)treeEntryDefinition.Mode);
            Ensure.ZeroResult(res);
        }

        public static unsafe ObjectId git_treebuilder_write(TreeBuilderHandle bld)
        {
            GitOid oid;
            int res = NativeMethods.git_treebuilder_write(out oid, bld);
            Ensure.ZeroResult(res);

            return oid;
        }

#endregion

#region git_transaction_

        public static void git_transaction_commit(IntPtr txn)
        {
            NativeMethods.git_transaction_commit(txn);
        }

        public static void git_transaction_free(IntPtr txn)
        {
            NativeMethods.git_transaction_free(txn);
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

        // C# equivalent of libgit2's git_libgit2_opt_t
        private enum LibGit2Option
        {
            GetMWindowSize,                  // GIT_OPT_GET_MWINDOW_SIZE
            SetMWindowSize,                  // GIT_OPT_SET_MWINDOW_SIZE
            GetMWindowMappedLimit,           // GIT_OPT_GET_MWINDOW_MAPPED_LIMIT
            SetMWindowMappedLimit,           // GIT_OPT_SET_MWINDOW_MAPPED_LIMIT
            GetSearchPath,                   // GIT_OPT_GET_SEARCH_PATH
            SetSearchPath,                   // GIT_OPT_SET_SEARCH_PATH
            SetCacheObjectLimit,             // GIT_OPT_SET_CACHE_OBJECT_LIMIT
            SetCacheMaxSize,                 // GIT_OPT_SET_CACHE_MAX_SIZE
            EnableCaching,                   // GIT_OPT_ENABLE_CACHING
            GetCachedMemory,                 // GIT_OPT_GET_CACHED_MEMORY
            GetTemplatePath,                 // GIT_OPT_GET_TEMPLATE_PATH
            SetTemplatePath,                 // GIT_OPT_SET_TEMPLATE_PATH
            SetSslCertLocations,             // GIT_OPT_SET_SSL_CERT_LOCATIONS
            SetUserAgent,                    // GIT_OPT_SET_USER_AGENT
            EnableStrictObjectCreation,      // GIT_OPT_ENABLE_STRICT_OBJECT_CREATION
            EnableStrictSymbolicRefCreation, // GIT_OPT_ENABLE_STRICT_SYMBOLIC_REF_CREATION
            SetSslCiphers,                   // GIT_OPT_SET_SSL_CIPHERS
            GetUserAgent,                    // GIT_OPT_GET_USER_AGENT
            EnableOfsDelta,                  // GIT_OPT_ENABLE_OFS_DELTA
            EnableFsyncGitdir,               // GIT_OPT_ENABLE_FSYNC_GITDIR
            GetWindowsSharemode,             // GIT_OPT_GET_WINDOWS_SHAREMODE
            SetWindowsSharemode,             // GIT_OPT_SET_WINDOWS_SHAREMODE
            EnableStrictHashVerification,    // GIT_OPT_ENABLE_STRICT_HASH_VERIFICATION
        }

        /// <summary>
        /// Get the paths under which libgit2 searches for the configuration file of a given level.
        /// </summary>
        /// <param name="level">The level (global/system/XDG) of the config.</param>
        /// <returns>
        ///     The paths delimited by 'GIT_PATH_LIST_SEPARATOR'.
        /// </returns>
        public static string git_libgit2_opts_get_search_path(ConfigurationLevel level)
        {
            string path;

            using (var buf = new GitBuf())
            {
                var res = NativeMethods.git_libgit2_opts((int)LibGit2Option.GetSearchPath, (uint)level, buf);
                Ensure.ZeroResult(res);

                path = LaxUtf8Marshaler.FromNative(buf.ptr) ?? string.Empty;
            }

            return path;
        }

        public static void git_libgit2_opts_enable_strict_hash_verification(bool enabled)
        {
            NativeMethods.git_libgit2_opts((int)LibGit2Option.EnableStrictHashVerification, enabled ? 1 : 0);
        }

        /// <summary>
        /// Set the path(s) under which libgit2 searches for the configuration file of a given level.
        /// </summary>
        /// <param name="level">The level (global/system/XDG) of the config.</param>
        /// <param name="path">
        ///     A string of paths delimited by 'GIT_PATH_LIST_SEPARATOR'.
        ///     Pass null to reset the search path to the default.
        /// </param>
        public static void git_libgit2_opts_set_search_path(ConfigurationLevel level, string path)
        {
            var res = NativeMethods.git_libgit2_opts((int)LibGit2Option.SetSearchPath, (uint)level, path);
            Ensure.ZeroResult(res);
        }

        /// <summary>
        /// Enable or disable the libgit2 cache
        /// </summary>
        /// <param name="enabled">true to enable the cache, false otherwise</param>
        public static void git_libgit2_opts_set_enable_caching(bool enabled)
        {
            // libgit2 expects non-zero value for true
            var res = NativeMethods.git_libgit2_opts((int)LibGit2Option.EnableCaching, enabled ? 1 : 0);
            Ensure.ZeroResult(res);
        }

        /// <summary>
        /// Enable or disable the ofs_delta capabilty
        /// </summary>
        /// <param name="enabled">true to enable the ofs_delta capabilty, false otherwise</param>
        public static void git_libgit2_opts_set_enable_ofsdelta(bool enabled)
        {
            // libgit2 expects non-zero value for true
            var res = NativeMethods.git_libgit2_opts((int)LibGit2Option.EnableOfsDelta, enabled ? 1 : 0);
            Ensure.ZeroResult(res);
        }

        /// <summary>
        /// Enable or disable the strict_object_creation capabilty
        /// </summary>
        /// <param name="enabled">true to enable the strict_object_creation capabilty, false otherwise</param>
        public static void git_libgit2_opts_set_enable_strictobjectcreation(bool enabled)
        {
            // libgit2 expects non-zero value for true
            var res = NativeMethods.git_libgit2_opts((int)LibGit2Option.EnableStrictObjectCreation, enabled ? 1 : 0);
            Ensure.ZeroResult(res);
        }

        #endregion

        #region git_worktree_

        /// <summary>
        /// Returns a handle to the corresponding worktree,
        /// or an invalid handle if a worktree is not found.
        /// </summary>
        public static unsafe WorktreeHandle git_worktree_lookup(RepositoryHandle repo, string name)
        {
            git_worktree* worktree;
            var res = NativeMethods.git_worktree_lookup(out worktree, repo, name);

            switch (res)
            {
                case (int)GitErrorCode.Error:
                case (int)GitErrorCode.NotFound:
                case (int)GitErrorCode.Exists:
                case (int)GitErrorCode.OrphanedHead:
                    return null;

                default:
                    Ensure.ZeroResult(res);
                    return new WorktreeHandle(worktree, true);
            }
        }

        public static unsafe IList<string> git_worktree_list(RepositoryHandle repo)
        {
            var array = new GitStrArrayNative();

            try
            {
                int res = NativeMethods.git_worktree_list(out array.Array, repo);
                Ensure.ZeroResult(res);

                return array.ReadStrings();
            }
            finally
            {
                array.Dispose();
            }
        }

        public static unsafe RepositoryHandle git_repository_open_from_worktree(WorktreeHandle handle)
        {
            git_repository* repo;
            int res = NativeMethods.git_repository_open_from_worktree(out repo, handle);

            if (res == (int)GitErrorCode.NotFound)
            {
                throw new RepositoryNotFoundException("Handle doesn't point at a valid Git repository or workdir.");
            }

            Ensure.ZeroResult(res);

            return new RepositoryHandle(repo, true);
        }

        public static unsafe WorktreeLock git_worktree_is_locked(WorktreeHandle worktree)
        {
            using (var buf = new GitBuf())
            {
                int res = NativeMethods.git_worktree_is_locked(buf, worktree);

                if(res < 0)
                {
                    // error
                    return null;
                }

                if (res == (int)GitErrorCode.Ok)
                {
                    return new WorktreeLock();
                }

                return new WorktreeLock(true, LaxUtf8Marshaler.FromNative(buf.ptr));
            }
        }

        public static unsafe bool git_worktree_validate(WorktreeHandle worktree)
        {
            int res = NativeMethods.git_worktree_validate(worktree);

            return res == (int)GitErrorCode.Ok;
        }

        public static unsafe bool git_worktree_unlock(WorktreeHandle worktree)
        {
            int res = NativeMethods.git_worktree_unlock(worktree);

            return res == (int)GitErrorCode.Ok;
        }

        public static unsafe bool git_worktree_lock(WorktreeHandle worktree, string reason)
        {
            int res = NativeMethods.git_worktree_lock(worktree, reason);

            return res == (int)GitErrorCode.Ok;
        }

        public static unsafe WorktreeHandle git_worktree_add(
            RepositoryHandle repo,
            string name,
            string path,
            git_worktree_add_options options)
        {
            git_worktree* worktree;
            int res = NativeMethods.git_worktree_add(out worktree, repo, name, path, options);
            Ensure.ZeroResult(res);
            return new WorktreeHandle(worktree, true);
        }

        public static unsafe bool git_worktree_prune(WorktreeHandle worktree,
            git_worktree_prune_options options)
        {
            int res = NativeMethods.git_worktree_prune(worktree, options);
            Ensure.ZeroResult(res);
            return true;
        }

        #endregion

        private static ICollection<TResult> git_foreach<T, TResult>(
            Func<T, TResult> resultSelector,
            Func<Func<T, IntPtr, int>, int> iterator,
            params GitErrorCode[] ignoredErrorCodes)
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

        private static ICollection<TResult> git_foreach<T1, T2, TResult>(
            Func<T1, T2, TResult> resultSelector,
            Func<Func<T1, T2, IntPtr, int>, int> iterator,
            params GitErrorCode[] ignoredErrorCodes)
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

        private static ICollection<TResult> git_foreach<T1, T2, T3, TResult>(
            Func<T1, T2, T3, TResult> resultSelector,
            Func<Func<T1, T2, T3, IntPtr, int>, int> iterator,
            params GitErrorCode[] ignoredErrorCodes)
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

        public delegate TResult Func<T1, T2, T3, T4, T5, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

        private static ICollection<TResult> git_foreach<T1, T2, T3, T4, TResult>(
            Func<T1, T2, T3, T4, TResult> resultSelector,
            Func<Func<T1, T2, T3, T4, IntPtr, int>, int> iterator,
            params GitErrorCode[] ignoredErrorCodes)
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

        private static unsafe bool RepositoryStateChecker(RepositoryHandle repo, Func<IntPtr, int> checker)
        {
            int res = checker(repo.AsIntPtr());
            Ensure.BooleanResult(res);

            return (res == 1);
        }

        private static FilePath ConvertPath(Func<GitBuf, int> pathRetriever)
        {
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

    /// <summary>
    /// Class to hold extension methods used by the proxy class.
    /// </summary>
    static class ProxyExtensions
    {
        /// <summary>
        /// Convert a UIntPtr to a int value. Will throw
        /// exception if there is an overflow.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static int ConvertToInt(this UIntPtr input)
        {
            ulong ulongValue = (ulong)input;
            if (ulongValue > int.MaxValue)
            {
                throw new LibGit2SharpException("value exceeds size of an int");
            }

            return (int)input;
        }


        /// <summary>
        /// Convert a UIntPtr to a long value. Will throw
        /// exception if there is an overflow.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static long ConvertToLong(this UIntPtr input)
        {
            ulong ulongValue = (ulong)input;
            if (ulongValue > long.MaxValue)
            {
                throw new LibGit2SharpException("value exceeds size of long");
            }

            return (long)input;
        }
    }
}
// ReSharper restore InconsistentNaming
