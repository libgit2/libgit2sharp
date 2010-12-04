#include "libgit2wrap.h"

void build_wrapped_git_repository_details(wrapped_git_repository_details* wrapped_repo_out, git_repository* repo);

void build_wrapped_git_repository_details(wrapped_git_repository_details* wrapped_repo_out, git_repository* repo)
{
	wrapped_git_repository* wrapped = ((wrapped_git_repository*)(repo));
	wrapped_repo_out->path_repository = wrapped->path_repository;
	wrapped_repo_out->path_index = wrapped->path_index;
	wrapped_repo_out->path_odb = wrapped->path_odb;
	wrapped_repo_out->path_workdir = wrapped->path_workdir;
	wrapped_repo_out->is_bare = wrapped->is_bare;
	wrapped_repo_out->repo = repo;
}

int wrapped_git_repository_open(wrapped_git_repository_details* wrapped_repo_out, git_repository** repo_out, const char* path)
{
	git_repository *repo;
	int error = git_repository_open(&repo, path);

	build_wrapped_git_repository_details(wrapped_repo_out, repo);

	*repo_out = repo;
	return error;
}

int wrapped_git_repository_open2(wrapped_git_repository_details* wrapped_repo_out, git_repository** repo_out, const char *git_dir, const char *git_object_directory, const char *git_index_file, const char *git_work_tree)
{
	git_repository *repo;
	int error = git_repository_open2(&repo, git_dir, git_object_directory, git_index_file, git_work_tree);

	build_wrapped_git_repository_details(wrapped_repo_out, repo);

	*repo_out = repo;
	return error;
}



void wrapped_git_repository_free(git_repository* repo)
{
	git_repository_free(repo);
}

int wrapped_git_odb_read_header(git_rawobj* obj_out, git_repository* repo, const char *raw_id)
{
	git_odb *odb;
	git_oid id;
	int error;

	odb = git_repository_database(repo);

	error = git_oid_mkstr(&id, raw_id);
	if (error != GIT_SUCCESS)
		return error;

	error = git_odb_read_header(obj_out, odb, &id);
	if (error != GIT_SUCCESS)
		return error;

	return error;
}