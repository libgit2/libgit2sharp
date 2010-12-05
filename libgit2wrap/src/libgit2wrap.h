#ifndef INCLUDE_libgit2_wrap_h__
#define INCLUDE_libgit2_wrap_h__

#include <git/common.h>
#include <git/repository.h>

typedef struct wrapped_git_repository {
	size_t db;
	size_t index;
	size_t objects;
	
	char *path_repository;
	char *path_index;
	char *path_odb;
	char *path_workdir;

	unsigned is_bare:1;
} wrapped_git_repository ;

GIT_BEGIN_DECL

GIT_EXTERN(int) wrapped_git_repository_open(git_repository** repo_out, const char* path);
GIT_EXTERN(int) wrapped_git_repository_open2(git_repository** repo_out, const char *git_dir, const char *git_object_directory, const char *git_index_file, const char *git_work_tree);
GIT_EXTERN(void) wrapped_git_repository_free(git_repository* repo);
GIT_EXTERN(int) wrapped_git_odb_read_header(git_rawobj* obj_out, git_repository* repo, const char* raw_id);
GIT_EXTERN(int) wrapped_git_odb_read(git_rawobj* obj_out, git_repository* repo, const char* raw_id);

GIT_END_DECL

#endif