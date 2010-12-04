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

typedef struct wrapped_git_repository_details {
	char *path_repository;
	char *path_index;
	char *path_odb;
	char *path_workdir;
	git_repository* repo;

	unsigned is_bare:1;
} wrapped_git_repository_details ;

GIT_BEGIN_DECL

GIT_EXTERN(int) wrapped_git_repository_open(wrapped_git_repository_details* repo_out, git_repository** repo2, const char* path);
GIT_EXTERN(int) wrapped_git_repository_open2(wrapped_git_repository_details* repo_out, git_repository** repo2, const char *git_dir, const char *git_object_directory, const char *git_index_file, const char *git_work_tree);
GIT_EXTERN(void) wrapped_git_repository_free(git_repository* repo);
GIT_EXTERN(int) wrapped_git_odb_read_header(git_rawobj* obj_out, git_repository* repo, const char* raw_id);

GIT_END_DECL

#endif