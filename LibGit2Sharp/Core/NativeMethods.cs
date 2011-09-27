﻿using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal static class NativeMethods
    {
        private const string libgit2 = "git2";

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
        public static extern int git_index_remove(IndexSafeHandle index, int n);

        [DllImport(libgit2)]
        public static extern int git_index_write(IndexSafeHandle index);

        [DllImport(libgit2)]
        public static extern IntPtr git_lasterror();

        [DllImport(libgit2)]
        public static extern void git_object_close(IntPtr obj);

        [DllImport(libgit2)]
        public static extern IntPtr git_object_id(IntPtr obj);

        [DllImport(libgit2)]
        public static extern int git_object_lookup(out IntPtr obj, RepositorySafeHandle repo, ref GitOid id, GitObjectType type);

        [DllImport(libgit2)]
        public static extern int git_object_lookup_prefix(out IntPtr obj, RepositorySafeHandle repo, ref GitOid id, uint len, GitObjectType type);

        [DllImport(libgit2)]
        public static extern GitObjectType git_object_type(IntPtr obj);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool git_odb_exists(IntPtr db, ref GitOid id);

        [DllImport(libgit2)]
        public static extern void git_odb_object_close(IntPtr obj);

        [DllImport(libgit2)]
        public static extern int git_oid_cmp(ref GitOid a, ref GitOid b);

        [DllImport(libgit2)]
        public static extern int git_reference_create_oid(
            out IntPtr reference,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            ref GitOid oid,
            bool force);

        [DllImport(libgit2)]
        public static extern int git_reference_create_symbolic(
            out IntPtr reference,
            RepositorySafeHandle repo,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string name,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(Utf8Marshaler))] string target,
            bool force);

        [DllImport(libgit2)]
        public static extern int git_reference_delete(IntPtr reference);

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
            bool force);

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
        public static extern IntPtr git_repository_database(RepositorySafeHandle repository);

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
        public static extern IntPtr git_repository_path(RepositorySafeHandle repository, GitRepositoryPathId pathIdentifier);

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
    }
}
