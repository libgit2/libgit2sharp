#ifndef INCLUDE_libgit2_wrap_h__
#define INCLUDE_libgit2_wrap_h__

#include <git2.h>

GIT_BEGIN_DECL

GIT_EXTERN(int) wrapped_git_repository_open(git_repository** repo_out, const char* path);
GIT_EXTERN(int) wrapped_git_repository_open2(git_repository** repo_out, const char *git_dir, const char *git_object_directory, const char *git_index_file, const char *git_work_tree);
GIT_EXTERN(void) wrapped_git_repository_free(git_repository* repo);
GIT_EXTERN(int) wrapped_git_repository_lookup(git_object** obj_out, git_repository* repo, const char* raw_id, git_otype type);
GIT_EXTERN(int) wrapped_git_odb_exists(git_repository* repo, const char* raw_id);
GIT_EXTERN(int) wrapped_git_odb_read_header(git_rawobj* obj_out, git_repository* repo, const char* raw_id);
GIT_EXTERN(int) wrapped_git_odb_read(git_rawobj* obj_out, git_repository* repo, const char* raw_id);
GIT_EXTERN(int) wrapped_git_apply_tag(git_tag** tag_out, git_repository* repo, const char *raw_target_id, const char *tag_name, const char *tag_message, const char *tagger_name, const char *tagger_email, time_t tagger_time);

GIT_END_DECL

#endif