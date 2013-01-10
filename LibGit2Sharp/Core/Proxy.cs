﻿using System;
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

        #region git_blob_

        public static ObjectId git_blob_create_fromchunks(RepositorySafeHandle repo, FilePath hintpath, NativeMethods.source_callback fileCallback)
        {
            using (ThreadAffinity())
            {
                var oid = new GitOid();
                int res = NativeMethods.git_blob_create_fromchunks(ref oid, repo, hintpath, fileCallback, IntPtr.Zero);
                Ensure.Success(res);

                return new ObjectId(oid);
            }
        }

        public static ObjectId git_blob_create_fromdisk(RepositorySafeHandle repo, FilePath path)
        {
            using (ThreadAffinity())
            {
                var oid = new GitOid();
                int res = NativeMethods.git_blob_create_fromdisk(ref oid, repo, path);
                Ensure.Success(res);

                return new ObjectId(oid);
            }
        }

        public static ObjectId git_blob_create_fromfile(RepositorySafeHandle repo, FilePath path)
        {
            using (ThreadAffinity())
            {
                var oid = new GitOid();
                int res = NativeMethods.git_blob_create_fromworkdir(ref oid, repo, path);
                Ensure.Success(res);

                return new ObjectId(oid);
            }
        }

        public static byte[] git_blob_rawcontent(RepositorySafeHandle repo, ObjectId id, int size)
        {
            using (var obj = new ObjectSafeWrapper(id, repo))
            {
                var arr = new byte[size];
                Marshal.Copy(NativeMethods.git_blob_rawcontent(obj.ObjectPtr), arr, 0, size);
                return arr;
            }
        }

        public static UnmanagedMemoryStream git_blob_rawcontent_stream(RepositorySafeHandle repo, ObjectId id, Int64 size)
        {
            using (var obj = new ObjectSafeWrapper(id, repo))
            {
                IntPtr ptr = NativeMethods.git_blob_rawcontent(obj.ObjectPtr);
                unsafe
                {
                    return new UnmanagedMemoryStream((byte*)ptr.ToPointer(), size);
                }
            }
        }

        public static Int64 git_blob_rawsize(GitObjectSafeHandle obj)
        {
            return NativeMethods.git_blob_rawsize(obj);
        }

        #endregion

        #region git_branch_

        public static ReferenceSafeHandle git_branch_create(RepositorySafeHandle repo, string branch_name, ObjectId targetId, bool force)
        {
            using (ThreadAffinity())
            using (var osw = new ObjectSafeWrapper(targetId, repo))
            {
                ReferenceSafeHandle reference;
                int res = NativeMethods.git_branch_create(out reference, repo, branch_name, osw.ObjectPtr, force);
                Ensure.Success(res);
                return reference;
            }
        }

        public static void git_branch_delete(ReferenceSafeHandle reference)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_branch_delete(reference);
                reference.SetHandleAsInvalid();
                Ensure.Success(res);
            }
        }

        public static ICollection<TResult> git_branch_foreach<TResult>(
            RepositorySafeHandle repo,
            GitBranchType branch_type,
            Func<IntPtr, GitBranchType, TResult> resultSelector)
        {
            return git_foreach(resultSelector, c => NativeMethods.git_branch_foreach(repo, branch_type, (x, y, p) => c(x, y, p), IntPtr.Zero));
        }

        public static void git_branch_move(ReferenceSafeHandle reference, string new_branch_name, bool force)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_branch_move(reference, new_branch_name, force);
                Ensure.Success(res);
            }
        }

        public static ReferenceSafeHandle git_branch_tracking(ReferenceSafeHandle branch)
        {
            using (ThreadAffinity())
            {
                ReferenceSafeHandle reference;
                int res = NativeMethods.git_branch_tracking(out reference, branch);

                if (res == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.Success(res);

                return reference;
            }
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
                Ensure.Success(res);
            }
        }

        public static void git_checkout_head(RepositorySafeHandle repo, ref GitCheckoutOpts opts)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_checkout_head(repo, ref opts);
                Ensure.Success(res);
            }
        }

        public static void git_checkout_index(RepositorySafeHandle repo, GitObjectSafeHandle treeish, ref GitCheckoutOpts opts)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_checkout_index(repo, treeish, ref opts);
                Ensure.Success(res);
            }
        }

        #endregion

        #region git_clone_

        public static RepositorySafeHandle git_clone(
            string url,
            string workdir,
            GitCloneOptions opts)
        {
            using (ThreadAffinity())
            {
                RepositorySafeHandle repo;
                int res = NativeMethods.git_clone(out repo, url, workdir, opts);
                Ensure.Success(res);
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
            string prettifiedMessage,
            Tree tree,
            IEnumerable<ObjectId> parentIds)
        {
            using (ThreadAffinity())
            using (var treePtr = new ObjectSafeWrapper(tree.Id, repo))
            using (var parentObjectPtrs = new DisposableEnumerable<ObjectSafeWrapper>(parentIds.Select(id => new ObjectSafeWrapper(id, repo))))
            using (SignatureSafeHandle authorHandle = author.BuildHandle())
            using (SignatureSafeHandle committerHandle = committer.BuildHandle())
            {
                GitOid commitOid;
                string encoding = null; //TODO: Handle the encoding of the commit to be created

                IntPtr[] parentsPtrs = parentObjectPtrs.Select(o => o.ObjectPtr.DangerousGetHandle()).ToArray();
                int res = NativeMethods.git_commit_create(out commitOid, repo, referenceName, authorHandle,
                                                      committerHandle, encoding, prettifiedMessage, treePtr.ObjectPtr, parentObjectPtrs.Count(), parentsPtrs);
                Ensure.Success(res);

                return new ObjectId(commitOid);
            }
        }

        public static string git_commit_message(GitObjectSafeHandle obj)
        {
            return NativeMethods.git_commit_message(obj);
        }

        public static string git_commit_message_encoding(GitObjectSafeHandle obj)
        {
            return NativeMethods.git_commit_message_encoding(obj);
        }

        public static GitObjectSafeHandle git_commit_parent(ObjectSafeWrapper obj, uint i)
        {
            using (ThreadAffinity())
            {
                GitObjectSafeHandle parentCommit;
                int res = NativeMethods.git_commit_parent(out parentCommit, obj.ObjectPtr, i);
                Ensure.Success(res);

                return parentCommit;
            }
        }

        public static ObjectId git_commit_parent_oid(GitObjectSafeHandle obj, uint i)
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

        public static ObjectId git_commit_tree_oid(GitObjectSafeHandle obj)
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
                Ensure.Success(res);
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

                Ensure.Success(res);
                return true;
            }
        }

        public static string git_config_find_global()
        {
            return ConvertPath(NativeMethods.git_config_find_global);
        }

        public static string git_config_find_system()
        {
            return ConvertPath(NativeMethods.git_config_find_system);
        }

        public static string git_config_find_xdg()
        {
            return ConvertPath(NativeMethods.git_config_find_xdg);
        }

        public static void git_config_free(IntPtr config)
        {
            NativeMethods.git_config_free(config);
        }

        public static ConfigurationEntry<T> git_config_get_entry<T>(ConfigurationSafeHandle config, string key)
        {
            GitConfigEntryHandle handle;

            if (!configurationParser.ContainsKey(typeof(T)))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Generic Argument of type '{0}' is not supported.", typeof(T).FullName));
            }

            using (ThreadAffinity())
            {
                var res = NativeMethods.git_config_get_entry(out handle, config, key);
                if (res == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.Success(res);
            }

            GitConfigEntry entry = handle.MarshalAsGitConfigEntry();

            return new ConfigurationEntry<T>(Utf8Marshaler.FromNative(entry.namePtr),
                (T)configurationParser[typeof(T)](Utf8Marshaler.FromNative(entry.valuePtr)),
                (ConfigurationLevel)entry.level);
        }

        public static ConfigurationSafeHandle git_config_new()
        {
            using (ThreadAffinity())
            {
                ConfigurationSafeHandle handle;
                int res = NativeMethods.git_config_new(out handle);
                Ensure.Success(res);

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

                Ensure.Success(res);

                return handle;
            }
        }

        public static ConfigurationSafeHandle git_config_open_ondisk(FilePath path)
        {
            using (ThreadAffinity())
            {
                ConfigurationSafeHandle handle;
                int res = NativeMethods.git_config_open_ondisk(out handle, path);
                Ensure.Success(res);

                return handle;
            }
        }

        public static bool git_config_parse_bool(string value)
        {
            using (ThreadAffinity())
            {
                bool outVal;
                var res = NativeMethods.git_config_parse_bool(out outVal, value);

                Ensure.Success(res);
                return outVal;
            }
        }

        public static int git_config_parse_int32(string value)
        {
            using (ThreadAffinity())
            {
                int outVal;
                var res = NativeMethods.git_config_parse_int32(out outVal, value);

                Ensure.Success(res);
                return outVal;
            }
        }

        public static long git_config_parse_int64(string value)
        {
            using (ThreadAffinity())
            {
                long outVal;
                var res = NativeMethods.git_config_parse_int64(out outVal, value);

                Ensure.Success(res);
                return outVal;
            }
        }

        public static void git_config_set_bool(ConfigurationSafeHandle config, string name, bool value)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_config_set_bool(config, name, value);
                Ensure.Success(res);
            }
        }

        public static void git_config_set_int32(ConfigurationSafeHandle config, string name, int value)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_config_set_int32(config, name, value);
                Ensure.Success(res);
            }
        }

        public static void git_config_set_int64(ConfigurationSafeHandle config, string name, long value)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_config_set_int64(config, name, value);
                Ensure.Success(res);
            }
        }

        public static void git_config_set_string(ConfigurationSafeHandle config, string name, string value)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_config_set_string(config, name, value);
                Ensure.Success(res);
            }
        }

        public static ICollection<TResult> git_config_foreach<TResult>(
            ConfigurationSafeHandle config,
            Func<IntPtr, TResult> resultSelector)
        {
            return git_foreach(resultSelector, c => NativeMethods.git_config_foreach(config, (e, p) => c(e, p), IntPtr.Zero));
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
            NativeMethods.git_diff_data_cb lineCallback)
        {
            using (ThreadAffinity())
            using (var osw1 = new ObjectSafeWrapper(oldBlob, repo, true))
            using (var osw2 = new ObjectSafeWrapper(newBlob, repo, true))
            {
                int res = NativeMethods.git_diff_blobs(osw1.ObjectPtr, osw2.ObjectPtr, options, fileCallback, hunkCallback, lineCallback, IntPtr.Zero);
                Ensure.Success(res);
            }
        }

        public static DiffListSafeHandle git_diff_tree_to_index(
            RepositorySafeHandle repo,
            IndexSafeHandle index,
            ObjectId oldTree,
            GitDiffOptions options)
        {
            using (ThreadAffinity())
            using (var osw = new ObjectSafeWrapper(oldTree, repo, true))
            {
                DiffListSafeHandle diff;
                int res = NativeMethods.git_diff_tree_to_index(out diff, repo, osw.ObjectPtr, index, options);
                Ensure.Success(res);

                return diff;
            }
        }

        public static void git_diff_list_free(IntPtr diff)
        {
            NativeMethods.git_diff_list_free(diff);
        }

        public static void git_diff_merge(DiffListSafeHandle onto, DiffListSafeHandle from)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_diff_merge(onto, from);
                Ensure.Success(res);
            }
        }

        public static void git_diff_print_patch(DiffListSafeHandle diff, NativeMethods.git_diff_data_cb printCallback)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_diff_print_patch(diff, printCallback, IntPtr.Zero);
                Ensure.Success(res);
            }
        }

        public static DiffListSafeHandle git_diff_tree_to_tree(
            RepositorySafeHandle repo,
            ObjectId oldTree,
            ObjectId newTree,
            GitDiffOptions options)
        {
            using (ThreadAffinity())
            using (var osw1 = new ObjectSafeWrapper(oldTree, repo, true))
            using (var osw2 = new ObjectSafeWrapper(newTree, repo, true))
            {
                DiffListSafeHandle diff;
                int res = NativeMethods.git_diff_tree_to_tree(out diff, repo, osw1.ObjectPtr, osw2.ObjectPtr, options);
                Ensure.Success(res);

                return diff;
            }
        }

        public static DiffListSafeHandle git_diff_index_to_workdir(
            RepositorySafeHandle repo,
            IndexSafeHandle index,
            GitDiffOptions options)
        {
            using (ThreadAffinity())
            {
                DiffListSafeHandle diff;
                int res = NativeMethods.git_diff_index_to_workdir(out diff, repo, index, options);
                Ensure.Success(res);

                return diff;
            }
        }

        public static DiffListSafeHandle git_diff_tree_to_workdir(
           RepositorySafeHandle repo,
           ObjectId oldTree,
           GitDiffOptions options)
        {
            using (ThreadAffinity())
            using (var osw = new ObjectSafeWrapper(oldTree, repo, true))
            {
                DiffListSafeHandle diff;
                int res = NativeMethods.git_diff_tree_to_workdir(out diff, repo, osw.ObjectPtr, options);
                Ensure.Success(res);

                return diff;
            }
        }

        #endregion

        #region git_index_

        public static void git_index_add(IndexSafeHandle index, GitIndexEntry entry)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_index_add(index, entry);
                Ensure.Success(res);
            }
        }

        public static void git_index_add_from_workdir(IndexSafeHandle index, FilePath path)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_index_add_from_workdir(index, path);
                Ensure.Success(res);
            }
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

        public static int? git_index_find(IndexSafeHandle index, FilePath path)
        {
            int res = NativeMethods.git_index_find(index, path);

            if (res == (int)GitErrorCode.NotFound)
            {
                return null;
            }

            Ensure.Success(res, true);

            return res;
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
            return NativeMethods.git_index_has_conflicts(index) != 0;
        }

        public static IndexSafeHandle git_index_open(FilePath indexpath)
        {
            using (ThreadAffinity())
            {
                IndexSafeHandle handle;
                int res = NativeMethods.git_index_open(out handle, indexpath);
                Ensure.Success(res);

                return handle;
            }
        }

        public static void git_index_read_tree(RepositorySafeHandle repo, IndexSafeHandle index, Tree tree)
        {
            using (ThreadAffinity())
            using (var osw = new ObjectSafeWrapper(tree.Id, repo))
            {
                int res = NativeMethods.git_index_read_tree(index, osw.ObjectPtr, IntPtr.Zero);
                Ensure.Success(res);
            }
        }

        public static void git_index_remove(IndexSafeHandle index, FilePath path, int stage)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_index_remove(index, path, stage);
                Ensure.Success(res);
            }
        }

        public static void git_index_write(IndexSafeHandle index)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_index_write(index);
                Ensure.Success(res);
            }
        }

        public static GitOid git_tree_create_fromindex(Index index)
        {
            using (ThreadAffinity())
            {
                GitOid treeOid;
                int res = NativeMethods.git_index_write_tree(out treeOid, index.Handle);
                Ensure.Success(res);

                return treeOid;
            }
        }

        #endregion

        #region git_merge_

        public static ObjectId git_merge_base(RepositorySafeHandle repo, Commit first, Commit second)
        {
            using (ThreadAffinity())
            using (var osw1 = new ObjectSafeWrapper(first.Id, repo))
            using (var osw2 = new ObjectSafeWrapper(second.Id, repo))
            {
                GitOid ret;
                int res = NativeMethods.git_merge_base(out ret, repo, osw1.ObjectPtr, osw2.ObjectPtr);

                if (res == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.Success(res);

                return new ObjectId(ret);
            }
        }

        #endregion

        #region git_message_

        public static string git_message_prettify(string message)
        {
            using (ThreadAffinity())
            {
                int bufSize = NativeMethods.git_message_prettify(null, UIntPtr.Zero, message, false);
                Ensure.Success(bufSize, true);

                var buffer = new byte[bufSize];

                int res = NativeMethods.git_message_prettify(buffer, (UIntPtr)buffer.Length, message, false);
                Ensure.Success(res, true);

                return Utf8Marshaler.Utf8FromBuffer(buffer) ?? string.Empty;
            }
        }

        #endregion

        #region git_note_

        public static ObjectId git_note_create(
            RepositorySafeHandle repo,
            Signature author,
            Signature committer,
            string notes_ref,
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

                int res = NativeMethods.git_note_create(out noteOid, repo, authorHandle, committerHandle, notes_ref, ref oid, note, force ? 1 : 0);
                Ensure.Success(res);

                return new ObjectId(noteOid);
            }
        }

        public static string git_note_default_ref(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                string notes_ref;
                int res = NativeMethods.git_note_default_ref(out notes_ref, repo);
                Ensure.Success(res);

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

        public static ObjectId git_note_oid(NoteSafeHandle note)
        {
            return NativeMethods.git_note_oid(note).MarshalAsObjectId();
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

                Ensure.Success(res);

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

                Ensure.Success(res);
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
                        Ensure.Success(res);
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
                    (res == (int)GitErrorCode.NotFound || res == (int)GitErrorCode.Ambiguous))
                {
                    return null;
                }

                Ensure.Success(res);
                return peeled;
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
            Ensure.Success(NativeMethods.git_odb_add_backend(odb, backend, priority));
        }

        public static IntPtr git_odb_backend_malloc(IntPtr backend, UIntPtr len)
        {
            IntPtr toReturn = NativeMethods.git_odb_backend_malloc(backend, len);

            if (IntPtr.Zero == toReturn)
            {
                throw new LibGit2SharpException(String.Format(CultureInfo.InvariantCulture,
                                                              "Unable to allocate {0} bytes; out of memory",
                                                              len.ToString()),
                                                GitErrorCode.Error, GitErrorCategory.NoMemory);
            }

            return toReturn;
        }

        public static bool git_odb_exists(ObjectDatabaseSafeHandle odb, ObjectId id)
        {
            GitOid oid = id.Oid;
            return NativeMethods.git_odb_exists(odb, ref oid) != (int)GitErrorCode.Ok;
        }

        public static void git_odb_free(IntPtr odb)
        {
            NativeMethods.git_odb_free(odb);
        }

        #endregion

        #region git_reference_

        public static ReferenceSafeHandle git_reference_create_oid(RepositorySafeHandle repo, string name, ObjectId targetId, bool allowOverwrite)
        {
            using (ThreadAffinity())
            {
                GitOid oid = targetId.Oid;
                ReferenceSafeHandle handle;

                int res = NativeMethods.git_reference_create(out handle, repo, name, ref oid, allowOverwrite);
                Ensure.Success(res);

                return handle;
            }
        }

        public static ReferenceSafeHandle git_reference_create_symbolic(RepositorySafeHandle repo, string name, string target, bool allowOverwrite)
        {
            using (ThreadAffinity())
            {
                ReferenceSafeHandle handle;
                int res = NativeMethods.git_reference_symbolic_create(out handle, repo, name, target, allowOverwrite);
                Ensure.Success(res);

                return handle;
            }
        }

        public static void git_reference_delete(ReferenceSafeHandle reference)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_reference_delete(reference);
                reference.SetHandleAsInvalid();

                Ensure.Success(res);
            }
        }

        public static ICollection<TResult> git_reference_foreach_glob<TResult>(
            RepositorySafeHandle repo,
            string glob,
            GitReferenceType flags,
            Func<IntPtr, TResult> resultSelector)
        {
            return git_foreach(resultSelector, c => NativeMethods.git_reference_foreach_glob(repo, glob, flags, (x, p) => c(x, p), IntPtr.Zero));
        }

        public static void git_reference_free(IntPtr reference)
        {
            NativeMethods.git_reference_free(reference);
        }

        public static bool git_reference_is_valid_name(string refname)
        {
            return NativeMethods.git_reference_is_valid_name(refname) != 0;
        }

        public static IList<string> git_reference_list(RepositorySafeHandle repo, GitReferenceType flags)
        {
            using (ThreadAffinity())
            {
                UnSafeNativeMethods.git_strarray arr;
                int res = UnSafeNativeMethods.git_reference_list(out arr, repo, flags);
                Ensure.Success(res);

                return Libgit2UnsafeHelper.BuildListOf(arr);
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

                Ensure.Success(res);

                return handle;
            }
        }

        public static string git_reference_name(ReferenceSafeHandle reference)
        {
            return NativeMethods.git_reference_name(reference);
        }

        public static ObjectId git_reference_oid(ReferenceSafeHandle reference)
        {
            return NativeMethods.git_reference_target(reference).MarshalAsObjectId();
        }

        public static void git_reference_rename(ReferenceSafeHandle reference, string newName, bool allowOverwrite)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_reference_rename(reference, newName, allowOverwrite);
                Ensure.Success(res);
            }
        }

        public static ReferenceSafeHandle git_reference_resolve(ReferenceSafeHandle reference)
        {
            using (ThreadAffinity())
            {
                ReferenceSafeHandle resolvedHandle;
                int res = NativeMethods.git_reference_resolve(out resolvedHandle, reference);

                if (res == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.Success(res);

                return resolvedHandle;
            }
        }

        public static void git_reference_set_oid(ReferenceSafeHandle reference, ObjectId id)
        {
            using (ThreadAffinity())
            {
                GitOid oid = id.Oid;
                int res = NativeMethods.git_reference_set_target(reference, ref oid);
                Ensure.Success(res);
            }
        }

        public static void git_reference_set_target(ReferenceSafeHandle reference, string target)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_reference_symbolic_set_target(reference, target);
                Ensure.Success(res);
            }
        }

        public static string git_reference_target(ReferenceSafeHandle reference)
        {
            return NativeMethods.git_reference_symbolic_target(reference);
        }

        public static GitReferenceType git_reference_type(ReferenceSafeHandle reference)
        {
            return NativeMethods.git_reference_type(reference);
        }

        #endregion

        #region git_remote_

        public static RemoteSafeHandle git_remote_create(RepositorySafeHandle repo, string name, string url)
        {
            using (ThreadAffinity())
            {
                RemoteSafeHandle handle;
                int res = NativeMethods.git_remote_create(out handle, repo, name, url);
                Ensure.Success(res);

                return handle;
            }
        }

        public static void git_remote_connect(RemoteSafeHandle remote, GitDirection direction)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_remote_connect(remote, direction);
                Ensure.Success(res);
            }
        }

        public static void git_remote_disconnect(RemoteSafeHandle remote)
        {
            using (ThreadAffinity())
            {
                NativeMethods.git_remote_disconnect(remote);
            }
        }

        public static void git_remote_download(RemoteSafeHandle remote, TransferProgressHandler onTransferProgress)
        {
            using (ThreadAffinity())
            {
                NativeMethods.git_transfer_progress_callback cb = TransferCallbacks.GenerateCallback(onTransferProgress);

                int res = NativeMethods.git_remote_download(remote, cb, IntPtr.Zero);
                Ensure.Success(res);
            }
        }

        public static void git_remote_free(IntPtr remote)
        {
            NativeMethods.git_remote_free(remote);
        }

        public static IList<string> git_remote_list(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                UnSafeNativeMethods.git_strarray arr;
                int res = UnSafeNativeMethods.git_remote_list(out arr, repo);
                Ensure.Success(res);

                return Libgit2UnsafeHelper.BuildListOf(arr);
            }
        }

        public static RemoteSafeHandle git_remote_load(RepositorySafeHandle repo, string name, bool throwsIfNotFound)
        {
            using (ThreadAffinity())
            {
                RemoteSafeHandle handle;
                int res = NativeMethods.git_remote_load(out handle, repo, name);

                if (res == (int)GitErrorCode.NotFound && !throwsIfNotFound)
                {
                    return null;
                }

                Ensure.Success(res);
                return handle;
            }
        }

        public static string git_remote_name(RemoteSafeHandle remote)
        {
            return NativeMethods.git_remote_name(remote);
        }

        public static void git_remote_save(RemoteSafeHandle remote)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_remote_save(remote);
                Ensure.Success(res);
            }
        }

        public static void git_remote_set_autotag(RemoteSafeHandle remote, TagFetchMode value)
        {
            using (ThreadAffinity())
            {
                NativeMethods.git_remote_set_autotag(remote, value);
            }
        }

        public static void git_remote_set_fetchspec(RemoteSafeHandle remote, string fetchspec)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_remote_set_fetchspec(remote, fetchspec);
                Ensure.Success(res);
            }
        }

        public static void git_remote_set_callbacks(RemoteSafeHandle remote, ref GitRemoteCallbacks callbacks)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_remote_set_callbacks(remote, ref callbacks);
                Ensure.Success(res);
            }
        }

        public static void git_remote_update_tips(RemoteSafeHandle remote)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_remote_update_tips(remote);
                Ensure.Success(res);
            }
        }

        public static string git_remote_url(RemoteSafeHandle remote)
        {
            return NativeMethods.git_remote_url(remote);
        }

        #endregion

        #region git_repository_

        public static FilePath git_repository_discover(FilePath start_path)
        {
            return ConvertPath((buffer, bufSize) => NativeMethods.git_repository_discover(buffer, bufSize, start_path, false, null));
        }

        public static bool git_repository_head_detached(RepositorySafeHandle repo)
        {
            return RepositoryStateChecker(repo, NativeMethods.git_repository_head_detached);
        }

        public static void git_repository_free(IntPtr repo)
        {
            NativeMethods.git_repository_free(repo);
        }

        public static bool git_repository_head_orphan(RepositorySafeHandle repo)
        {
            return RepositoryStateChecker(repo, NativeMethods.git_repository_head_orphan);
        }

        public static IndexSafeHandle git_repository_index(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                IndexSafeHandle handle;
                int res = NativeMethods.git_repository_index(out handle, repo);
                Ensure.Success(res);

                return handle;
            }
        }

        public static RepositorySafeHandle git_repository_init(FilePath path, bool isBare)
        {
            using (ThreadAffinity())
            {
                RepositorySafeHandle repo;
                int res = NativeMethods.git_repository_init(out repo, path, isBare);
                Ensure.Success(res);

                return repo;
            }
        }

        public static bool git_repository_is_bare(RepositorySafeHandle repo)
        {
            return RepositoryStateChecker(repo, NativeMethods.git_repository_is_bare);
        }

        public static bool git_repository_is_empty(RepositorySafeHandle repo)
        {
            return RepositoryStateChecker(repo, NativeMethods.git_repository_is_empty);
        }

        public static void git_repository_merge_cleanup(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_repository_merge_cleanup(repo);
                Ensure.Success(res);
            }
        }

        public static ICollection<TResult> git_repository_mergehead_foreach<TResult>(
            RepositorySafeHandle repo,
            Func<GitOid, TResult> resultSelector)
        {
            using (ThreadAffinity())
            {
                return git_foreach(resultSelector, c => NativeMethods.git_repository_mergehead_foreach(repo, (ref GitOid x, IntPtr p) => c(x, p), IntPtr.Zero));
            }
        }

        public static ObjectDatabaseSafeHandle git_repository_odb(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                ObjectDatabaseSafeHandle handle;
                int res = NativeMethods.git_repository_odb(out handle, repo);
                Ensure.Success(res);

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

                Ensure.Success(res);

                return repo;
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

        public static void git_repository_set_index(RepositorySafeHandle repo, IndexSafeHandle index)
        {
            NativeMethods.git_repository_set_index(repo, index);
        }

        public static void git_repository_set_workdir(RepositorySafeHandle repo, FilePath workdir)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_repository_set_workdir(repo, workdir, false);
                Ensure.Success(res);
            }
        }

        public static CurrentOperation git_repository_state(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_repository_state(repo);
                Ensure.Success(res, true);
                return (CurrentOperation)res;
            }
        }

        public static FilePath git_repository_workdir(RepositorySafeHandle repo)
        {
            return NativeMethods.git_repository_workdir(repo);
        }

        #endregion

        #region git_reset_

        public static void git_reset(
            RepositorySafeHandle repo,
            ObjectId committishId,
            ResetOptions resetKind)
        {
            using (ThreadAffinity())
            using (var osw = new ObjectSafeWrapper(committishId, repo))
            {
                int res = NativeMethods.git_reset(repo, osw.ObjectPtr, resetKind);
                Ensure.Success(res);
            }
        }

        #endregion

        #region git_revparse_

        public static GitObjectSafeHandle git_revparse_single(RepositorySafeHandle repo, string objectish)
        {
            using (ThreadAffinity())
            {
                GitObjectSafeHandle obj;
                int res = NativeMethods.git_revparse_single(out obj, repo, objectish);

                switch (res)
                {
                    case (int)GitErrorCode.NotFound:
                        return null;

                    case (int)GitErrorCode.Ambiguous:
                        throw new AmbiguousException(string.Format(CultureInfo.InvariantCulture, "Provided abbreviated ObjectId '{0}' is too short.", objectish));

                    default:
                        Ensure.Success(res);
                        break;
                }

                return obj;
            }
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
                Ensure.Success(res);
            }
        }

        public static RevWalkerSafeHandle git_revwalk_new(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                RevWalkerSafeHandle handle;
                int res = NativeMethods.git_revwalk_new(out handle, repo);
                Ensure.Success(res);

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

                Ensure.Success(res);

                return new ObjectId(ret);
            }
        }

        public static void git_revwalk_push(RevWalkerSafeHandle walker, ObjectId id)
        {
            using (ThreadAffinity())
            {
                GitOid oid = id.Oid;
                int res = NativeMethods.git_revwalk_push(walker, ref oid);
                Ensure.Success(res);
            }
        }

        public static void git_revwalk_reset(RevWalkerSafeHandle walker)
        {
            NativeMethods.git_revwalk_reset(walker);
        }

        public static void git_revwalk_sorting(RevWalkerSafeHandle walker, GitSortOptions options)
        {
            NativeMethods.git_revwalk_sorting(walker, options);
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
                Ensure.Success(res);

                return handle;
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
                        throw new AmbiguousException(string.Format(CultureInfo.InvariantCulture, "More than one file matches the pathspec '{0}'. You can either force a literal path evaluation (GIT_STATUS_OPT_DISABLE_PATHSPEC_MATCH), or use git_status_foreach().", path));

                    default:
                        Ensure.Success(res);
                        break;
                }

                return status;
            }
        }

        public static ICollection<TResult> git_status_foreach<TResult>(RepositorySafeHandle repo, Func<IntPtr, uint, TResult> resultSelector)
        {
            return git_foreach(resultSelector, c => NativeMethods.git_status_foreach(repo, (x, y, p) => c(x, y, p), IntPtr.Zero));
        }

        #endregion

        #region git_tag_

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
            using (SignatureSafeHandle taggerHandle = tagger.BuildHandle())
            {
                GitOid oid;
                int res = NativeMethods.git_tag_create(out oid, repo, name, objectPtr.ObjectPtr, taggerHandle, message, allowOverwrite);
                Ensure.Success(res);

                return new ObjectId(oid);
            }
        }

        public static ObjectId git_tag_create_lightweight(RepositorySafeHandle repo, string name, GitObject target, bool allowOverwrite)
        {
            using (ThreadAffinity())
            using (var objectPtr = new ObjectSafeWrapper(target.Id, repo))
            {
                GitOid oid;
                int res = NativeMethods.git_tag_create_lightweight(out oid, repo, name, objectPtr.ObjectPtr, allowOverwrite);
                Ensure.Success(res);

                return new ObjectId(oid);
            }
        }

        public static void git_tag_delete(RepositorySafeHandle repo, string name)
        {
            using (ThreadAffinity())
            {
                int res = NativeMethods.git_tag_delete(repo, name);
                Ensure.Success(res);
            }
        }

        public static IList<string> git_tag_list(RepositorySafeHandle repo)
        {
            using (ThreadAffinity())
            {
                UnSafeNativeMethods.git_strarray arr;
                int res = UnSafeNativeMethods.git_tag_list(out arr, repo);
                Ensure.Success(res);

                return Libgit2UnsafeHelper.BuildListOf(arr);
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
            return new Signature(NativeMethods.git_tag_tagger(tag));
        }

        public static ObjectId git_tag_target_oid(GitObjectSafeHandle tag)
        {
            return NativeMethods.git_tag_target_id(tag).MarshalAsObjectId();
        }

        public static GitObjectType git_tag_target_type(GitObjectSafeHandle tag)
        {
            return NativeMethods.git_tag_target_type(tag);
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

                Ensure.Success(res);

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

        public static TreeBuilderSafeHandle git_treebuilder_create()
        {
            using (ThreadAffinity())
            {
                TreeBuilderSafeHandle builder;
                int res = NativeMethods.git_treebuilder_create(out builder, IntPtr.Zero);
                Ensure.Success(res);

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
                Ensure.Success(res);
            }
        }

        public static ObjectId git_treebuilder_write(RepositorySafeHandle repo, TreeBuilderSafeHandle bld)
        {
            using (ThreadAffinity())
            {
                GitOid oid;
                int res = NativeMethods.git_treebuilder_write(out oid, repo, bld);
                Ensure.Success(res);

                return new ObjectId(oid);
            }
        }

        #endregion

        private static ICollection<TResult> git_foreach<T, TResult>(Func<T, TResult> resultSelector, Func<Func<T, IntPtr, int>, int> iterator)
        {
            using (ThreadAffinity())
            {
                var result = new List<TResult>();
                var res = iterator((x, payload) =>
                                       {
                                           result.Add(resultSelector(x));
                                           return 0;
                                       });
                Ensure.Success(res);
                return result;
            }
        }

        private static ICollection<TResult> git_foreach<T1, T2, TResult>(Func<T1, T2, TResult> resultSelector, Func<Func<T1, T2, IntPtr, int>, int> iterator)
        {
            using (ThreadAffinity())
            {
                var result = new List<TResult>();
                var res = iterator((x, y, payload) =>
                                       {
                                           result.Add(resultSelector(x, y));
                                           return 0;
                                       });
                Ensure.Success(res);
                return result;
            }
        }

        private static unsafe class Libgit2UnsafeHelper
        {
            public static IList<string> BuildListOf(UnSafeNativeMethods.git_strarray strArray)
            {
                var list = new List<string>();

                try
                {
                    UnSafeNativeMethods.git_strarray* gitStrArray = &strArray;

                    uint numberOfEntries = (uint)gitStrArray->size;
                    for (uint i = 0; i < numberOfEntries; i++)
                    {
                        var name = Utf8Marshaler.FromNative((IntPtr)gitStrArray->strings[i]);
                        list.Add(name);
                    }

                    list.Sort(StringComparer.Ordinal);
                }
                finally
                {
                    UnSafeNativeMethods.git_strarray_free(ref strArray);
                }

                return list;
            }
        }

        private static bool RepositoryStateChecker(RepositorySafeHandle repo, Func<RepositorySafeHandle, int> checker)
        {
            using (ThreadAffinity())
            {
                int res = checker(repo);
                Ensure.Success(res, true);

                return (res == 1);
            }
        }

        private static string ConvertPath(Func<byte[], UIntPtr, int> pathRetriever)
        {
            using (ThreadAffinity())
            {
                var buffer = new byte[NativeMethods.GIT_PATH_MAX];

                int result = pathRetriever(buffer, (UIntPtr)NativeMethods.GIT_PATH_MAX);

                if (result == (int)GitErrorCode.NotFound)
                {
                    return null;
                }

                Ensure.Success(result);

                return Utf8Marshaler.Utf8FromBuffer(buffer);
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
    }
}
// ReSharper restore InconsistentNaming
