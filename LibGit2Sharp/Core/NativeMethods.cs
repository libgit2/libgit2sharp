using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    internal class NativeMethods
    {
        private const string libgit2 = "git2.dll";

        [DllImport(libgit2, SetLastError = true)]
        public static extern IntPtr git_commit_author(IntPtr commit);

        [DllImport(libgit2, SetLastError = true)]
        public static extern IntPtr git_commit_committer(IntPtr commit);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_commit_create_o(out GitOid oid, RepositorySafeHandle repo, string updateRef, IntPtr author, IntPtr committer, string message, IntPtr tree, int parentCount, IntPtr parents);

        [DllImport(libgit2, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.AnsiBStr)]
        public static extern string git_commit_message(IntPtr commit);

        [DllImport(libgit2, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.AnsiBStr)]
        public static extern string git_commit_message_short(IntPtr commit);

        [DllImport(libgit2)]
        public static extern int git_commit_parent(out IntPtr parentCommit, IntPtr commit, uint n);

        [DllImport(libgit2)]
        public static extern uint git_commit_parentcount(IntPtr commit);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_commit_tree(out IntPtr tree, IntPtr commit);

        [DllImport(libgit2, SetLastError = true)]
        public static extern void git_object_close(IntPtr obj);

        [DllImport(libgit2, SetLastError = true)]
        public static extern IntPtr git_object_id(IntPtr obj);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_object_lookup(out IntPtr obj, RepositorySafeHandle repo, ref GitOid id, GitObjectType type);

        [DllImport(libgit2, SetLastError = true)]
        public static extern GitObjectType git_object_type(IntPtr obj);

        [DllImport(libgit2, SetLastError = true)]
        public static extern bool git_odb_exists(IntPtr db, ref GitOid id);

        [DllImport(libgit2, SetLastError = true)]
        public static extern void git_odb_object_close(IntPtr obj);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_oid_cmp(ref GitOid a, ref GitOid b);

        [DllImport(libgit2, SetLastError = true)]
        public static extern void git_oid_fmt(byte[] str, ref GitOid oid);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_oid_mkstr(out GitOid oid, string str);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_reference_create_oid(out IntPtr reference, RepositorySafeHandle repo, string name, ref GitOid oid);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_reference_create_symbolic(out IntPtr reference, RepositorySafeHandle repo, string name, string target);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_reference_delete(IntPtr reference);

        [DllImport(libgit2)]
        public static extern int git_reference_lookup(out IntPtr reference, RepositorySafeHandle repo, string name);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.AnsiBStr)]
        public static extern string git_reference_name(IntPtr reference);

        [DllImport(libgit2)]
        public static extern IntPtr git_reference_oid(IntPtr reference);

        [DllImport(libgit2)]
        public static extern int git_reference_resolve(out IntPtr resolvedReference, IntPtr reference);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.AnsiBStr)]
        public static extern string git_reference_target(IntPtr reference);

        [DllImport(libgit2)]
        public static extern GitReferenceType git_reference_type(IntPtr reference);

        [DllImport(libgit2, SetLastError = true)]
        public static extern IntPtr git_repository_database(RepositorySafeHandle repository);

        [DllImport(libgit2, SetLastError = true)]
        public static extern void git_repository_free(IntPtr repository);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_repository_init(out RepositorySafeHandle repository, string path, bool isBare);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_repository_open(out RepositorySafeHandle repository, string path);

        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.AnsiBStr)]
        public static extern string git_repository_path(RepositorySafeHandle repository);
        
        [DllImport(libgit2)]
        [return: MarshalAs(UnmanagedType.AnsiBStr)]
        public static extern string git_repository_workdir(RepositorySafeHandle repository);

        [DllImport(libgit2)]
        public static extern void git_revwalk_free(IntPtr walker);

        [DllImport(libgit2)]
        public static extern int git_revwalk_new(out IntPtr walker, RepositorySafeHandle repo);

        [DllImport(libgit2)]
        public static extern int git_revwalk_next(out GitOid oid, IntPtr walker);

        [DllImport(libgit2)]
        public static extern int git_revwalk_push(IntPtr walker, ref GitOid oid);

        [DllImport(libgit2)]
        public static extern void git_revwalk_reset(IntPtr walker);

        [DllImport(libgit2)]
        public static extern void git_revwalk_sorting(IntPtr walk, GitSortOptions sort);

        [DllImport(libgit2, SetLastError = true)]
        public static extern void git_signature_free(IntPtr signature);

        [DllImport(libgit2, SetLastError = true)]
        public static extern IntPtr git_signature_new(string name, string email, long time, int offset);

        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_tag_create(out GitOid oid, RepositorySafeHandle repo, string name, ref GitOid target, GitObjectType type, GitSignature signature, string message);

        [DllImport(libgit2, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.AnsiBStr)]
        public static extern string git_tag_message(IntPtr tag);

        [DllImport(libgit2, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.AnsiBStr)]
        public static extern string git_tag_name(IntPtr tag);

        [DllImport(libgit2, SetLastError = true)]
        public static extern IntPtr git_tag_tagger(IntPtr tag);

        [DllImport(libgit2, SetLastError = true)]
        public static extern IntPtr git_tag_target_oid(IntPtr tag);

        /* Blob */
        [DllImport(libgit2, SetLastError = true)]
        public static extern int git_blob_rawsize(IntPtr blob);

        [DllImport(libgit2, SetLastError = true)]
        public static extern IntPtr git_blob_rawcontent(IntPtr blob);
    }
}